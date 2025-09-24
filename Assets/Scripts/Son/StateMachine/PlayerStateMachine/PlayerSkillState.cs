using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;
using static EventBus;
using NUnit.Framework;

public class PlayerSkillState : IState
{
    private PlayerMovement _player;

    // 子ミキサー（PlayerMovement 側の常駐インスタンスを「このステートが占有」する）
    private AnimationMixerPlayable actionMixer;

    // Skill は単発のため 1 枚だけ
    private AnimationClipPlayable skillPlayable;
    // もう一方のスロットは 0 重みのダミーで占位（未接続でも良いが見通しのため保持）
    private AnimationClipPlayable dummyPlayable;

    private ComboAction skillAction;
    private double actionDuration;
    private double elapsedTime;

    private bool hasCheckedHit;
    private bool hasSpawnedAttackVFX;
    private bool hasSpawnedAttackPrefab;
    private bool weaponHiddenForThisAction;
    private bool lungeInvoked;

    // 末尾の先行フェード開始済みか（空洞回避用：Main 層のアウト混合を早めに開始）
    private bool mainExitStarted;

    private WeaponInstance weapon;

    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.1f;
    private DamageData damageData = new DamageData(1);

    private static readonly Collider[] hitBuffer = new Collider[32];
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits
        = new System.Collections.Generic.HashSet<Enemy>();

    private readonly AnimationCurve defaultLungeCurve = null;
    private const float LUNGE_INPUT_MIN = 0.2f;

    private const float AUTO_LUNGE_FIND_RADIUS = 10f; // 索敵半径（必要なら Inspector 化）
    private static readonly Collider[] sphereBuffer = new Collider[64];

    private List<bool> attackListBook = new List<bool>();

    public PlayerSkillState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // === ① 前提チェック：ここでまだメイン層ブレンドは触らない ===
        weapon = _player.GetMainWeapon();
        if (weapon == null || weapon.template == null ||
            weapon.template.finisherAttack == null || weapon.template.finisherAttack.Count == 0 ||
            weapon.template.finisherAttack[0] == null || weapon.template.finisherAttack[0].animation == null)
        {
            // フィニッシャーが無い ⇒ 空洞を作らず即退避
            _player.ToIdle();
            return;
        }

        skillAction = weapon.template.finisherAttack[0];

        // 耐久チェック（未満なら入らない）
        if (weapon.currentDurability < skillAction.durabilityCost)
        {
            _player.ToIdle();
            return;
        }

        // === ② 子ミキサーを「単発スキル用レイアウト」に初期化して占有 ===
        actionMixer = _player.GetActionSubMixer();
        TakeOverSubMixerForSingleShot(skillAction.animation, Mathf.Max(0.0001f, weapon.template.attackSpeed));

        if(skillAction.hitTimeList != null && skillAction.hitTimeList.Count > 0)
        {
            attackListBook.Clear();
            for (int i = 0; i < skillAction.hitTimeList.Count; i++)
            {
                attackListBook.Add(false);
            }
        }

        // 初期姿勢を確定（0重みでの姿勢ジャンプ防止）
        skillPlayable.SetTime(0);
        _player.EvaluateGraphOnce();

        // ここで耐久を消費
        if (!_player.weaponInventory.ConsumeDurability(HandType.Main, skillAction.durabilityCost))
        {
            _player.ToIdle();
            return;
        }

        // === ③ ここで初めてメイン層を Action へ ===
        float enterDur = _player.ResolveBlendDuration(_player.lastBlendState, PlayerState.Skill);
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        // フラグ初期化
        actionDuration = skillAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;
        hasSpawnedAttackPrefab = false;
        weaponHiddenForThisAction = false;
        lungeInvoked = false;
        mainExitStarted = false;

        damageData = new DamageData(weapon.template.attackPower + skillAction.actPowerModifier);

        if (skillAction.swingSFXInfo.clip)
            PlayerEvents.PlayClipByPart(PlayerAudioPart.RHand, skillAction.swingSFXInfo.clip, skillAction.swingSFXInfo.volume, skillAction.swingSFXInfo.pitch, skillAction.swingSFXInfo.delay);
        if (skillAction.voiceSFXInfo.clip)
            PlayerEvents.PlayClipByPart(PlayerAudioPart.Mouth, skillAction.voiceSFXInfo.clip, skillAction.voiceSFXInfo.volume, skillAction.voiceSFXInfo.pitch, skillAction.voiceSFXInfo.delay);
    
        PlayerEvents.OnWeaponSkillUsed?.Invoke(weapon.template.weaponType);
    }

    public void OnExit()
    {
        // ★ 子ミキサーを必ず 0 重み化（他ステート移行での空洞防止）
        if (actionMixer.IsValid())
        {
            actionMixer.SetInputWeight(0, 0f);
            actionMixer.SetInputWeight(1, 0f);
        }

        // 右手武器の表示を戻す
        if (weaponHiddenForThisAction)
        {
            _player.ShowMainHandModel();
            weaponHiddenForThisAction = false;
        }

        // Playable の破棄（次ステートが再構成する前提）
        if (skillPlayable.IsValid()) { actionMixer.DisconnectInput(0); skillPlayable.Destroy(); }
        if (dummyPlayable.IsValid()) { actionMixer.DisconnectInput(1); dummyPlayable.Destroy(); }
    }

    public void OnUpdate(float deltaTime)
    {
        if (skillAction == null) return;

        elapsedTime += deltaTime;

        // --- 末尾事前ブレンド（空洞対策）：clipEnd - blendToNext からメイン層を抜け始める ---
        double clipEndTime = actionDuration;
        float remToClipEnd = Mathf.Max(0f, (float)(clipEndTime - elapsedTime));
        float gate = Mathf.Max(0f, skillAction.blendToNext);
        if (!mainExitStarted && remToClipEnd <= gate + 1e-4f)
        {
            var nextSlot = _player.HasMoveInput() ? PlayerMovement.MainLayerSlot.Move
                                                  : PlayerMovement.MainLayerSlot.Idle;
            float desired = _player.ResolveBlendDuration(PlayerState.Skill,
                          _player.HasMoveInput() ? PlayerState.Move : PlayerState.Idle);
            float exitDur = Mathf.Min(desired, remToClipEnd);
            _player.BlendToMainSlot(nextSlot, exitDur);
            mainExitStarted = true;
        }

        // --- 突進 ---
        if (!lungeInvoked && elapsedTime >= skillAction.lungeTime)
        {
            DoLungeForAction();
            lungeInvoked = true;
        }

        // --- VFX ---
        if (!hasSpawnedAttackVFX && skillAction.attackVFXPrefab != null && elapsedTime >= skillAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // --- ヒット判定/発射 ---
        if (!hasCheckedHit && skillAction.hitCheckTime >= 0f && elapsedTime >= skillAction.hitCheckTime)
        {
            TrySpawnAttackPrefabNow();
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // --- ヒット判定/発射(List) ---
        if (skillAction.hitTimeList != null && skillAction.hitTimeList.Count > 0)
        {
            for (int i = 0; i < skillAction.hitTimeList.Count; i++)
            {
                if (!attackListBook[i] && elapsedTime >= skillAction.hitTimeList[i])
                {
                    TrySpawnAttackPrefabNow();
                    DoAttackHitCheck();
                    attackListBook[i] = true;
                }
            }
        }

        // --- 終了 ---
        if (elapsedTime >= actionDuration)
        {
            _player.ToIdle();
        }
    }

    // ===== 子ミキサー接続（単発スキル用） =====
    /// <summary>
    /// 子ミキサーを「単発用」に構成:
    /// 入力0=スキル, 入力1=ダミー(0重み)。ブレンドは不要、常に0:1=1:0。
    /// </summary>
    private void TakeOverSubMixerForSingleShot(AnimationClip clip, float speed)
    {
        // 既存入力をクリーンアップ
        for (int i = 0; i < actionMixer.GetInputCount(); ++i)
        {
            var p = actionMixer.GetInput(i);
            if (p.IsValid()) { actionMixer.DisconnectInput(i); p.Destroy(); }
            actionMixer.SetInputWeight(i, 0f);
        }

        // 本体
        skillPlayable = AnimationClipPlayable.Create(_player.playableGraph, clip);
        skillPlayable.SetApplyFootIK(false);
        skillPlayable.SetApplyPlayableIK(false);
        skillPlayable.SetSpeed(speed);
        actionMixer.ConnectInput(0, skillPlayable, 0, 1f);

        // ダミー（保守目的。無くても動くが、意図が明確になる）
        dummyPlayable = AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
        dummyPlayable.SetSpeed(0.0001f);
        actionMixer.ConnectInput(1, dummyPlayable, 0, 0f);
    }

    // ===== 判定・演出 =====
    private void DoAttackHitCheck()
    {
        Vector3 worldCenter = _player.transform.TransformPoint(skillAction.hitBoxCenter);
        Vector3 halfExtents = skillAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore);

        if (_player.isHitboxVisible) ShowHitBox(worldCenter, skillAction.hitBoxSize, rot, 0.1f, Color.cyan);

        uniqueEnemyHits.Clear();
        var vfxPositions = new System.Collections.Generic.List<Vector3>(8);

        for (int i = 0; i < count; ++i)
        {
            var col = hitBuffer[i];
            if (col == null) continue;
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            if (!uniqueEnemyHits.Add(enemy)) continue;

            enemy.TakeDamage(damageData);

            Vector3 enemyCenter = col.bounds.center;
            Vector3 toPlayer = _player.transform.position - enemyCenter;
            Vector3 fxPos = (toPlayer.sqrMagnitude > 1e-6f)
                ? enemyCenter + toPlayer.normalized * HIT_VFX_TOWARD_PLAYER
                : enemyCenter + Vector3.up * 0.1f;
            vfxPositions.Add(fxPos);
        }

        if (vfxPositions.Count > 0)
            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));

        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    // 攻撃Prefab生成（ヒットボックス中心）
    private void TrySpawnAttackPrefabNow()
    {
        if (hasSpawnedAttackPrefab) return;
        if (skillAction.attackPrefab == null) return;

        Vector3 spawnPos = _player.transform.TransformPoint(skillAction.hitBoxCenter);
        Quaternion spawnRot = _player.transform.rotation;

        Object.Instantiate(skillAction.attackPrefab, spawnPos, spawnRot);
        hasSpawnedAttackPrefab = true;

        if (!weaponHiddenForThisAction)
        {
            _player.HideMainHandModel();
            weaponHiddenForThisAction = true;
        }
    }

    private void SpawnAttackVFX()
    {
        if (skillAction.attackVFXPrefab == null) return;
        var go = Object.Instantiate(skillAction.attackVFXPrefab, _player.transform.position, _player.transform.rotation);
        Object.Destroy(go, 3f);
    }

    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        for (int i = 0; i < positions.Count; ++i)
        {
            SpawnHitVFXAt(positions[i]);
            PlayerEvents.OnGamepadShakeCurve?.Invoke(0.6f, 1f, 0.1f);
            if (i < positions.Count - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnHitVFXAt(Vector3 pos)
    {
        var prefab = weapon?.template?.hitVFXPrefab;
        if (prefab == null) return;
        Object.Instantiate(prefab, pos, Quaternion.identity);
    }

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("SkillHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }

    // ==== 突進 ====
    private void DoLungeForAction()
    {
        float distance = Mathf.Max(0f, skillAction.lungeDistance);
        float speed = Mathf.Max(0.01f, skillAction.lungeSpeed);

        Vector3 dir;
        bool hasDirection = false;

        // 1) 強い移動入力（最優先）
        if (_player.TryGetMoveDirectionWorld(LUNGE_INPUT_MIN * LUNGE_INPUT_MIN, out dir))
        {
            hasDirection = true;
        }
        // 2) ロックオン対象（次点）
        else if (_player.TryGetLockOnHorizontalDirection(out dir))
        {
            hasDirection = true;
        }
        // 3) 自動索敵：半径内の最近敵（最後のフォールバック）
        else if (TryFindNearestEnemyDirXZ(AUTO_LUNGE_FIND_RADIUS, out dir))
        {
            hasDirection = true;
        }

        // --- 回頭（有効な方向が得られた場合のみ） ---
        if (hasDirection)
        {
            // 即時スナップ回頭：スキル演出開始フレームで向きを合わせる
            _player.RotateYawOverTime(dir, 0f);
        }

        // --- 突進の実行（距離 > 0 のときだけ） ---
        if (distance > 0f)
        {
            EventBus.PlayerEvents.LungeByDistance?.Invoke(
                hasDirection ? LungeManager.LungeAim.CustomDir : LungeManager.LungeAim.Forward,
                Vector3.zero,
                hasDirection ? dir : Vector3.zero,
                speed,
                distance,
                defaultLungeCurve
            );
        }
    }
    private bool TryFindNearestEnemyDirXZ(float radius, out Vector3 dirXZ)
    {
        dirXZ = Vector3.zero;

        // --- 半径内の候補を取得 ---
        int count = Physics.OverlapSphereNonAlloc(
            _player.transform.position,
            radius,
            sphereBuffer,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float bestSqr = float.PositiveInfinity;
        Enemy bestEnemy = null;

        for (int i = 0; i < count; ++i)
        {
            var col = sphereBuffer[i];
            if (!col) continue;

            // 親階層も含めて Enemy を検索（Collider が子にある構成を想定）
            var enemy = col.GetComponentInParent<Enemy>();
            if (!enemy) continue;

            // 水平化した距離（XZ）の二乗を採用
            Vector3 v = enemy.transform.position - _player.transform.position;
            v.y = 0f;
            float d2 = v.sqrMagnitude;
            if (d2 < 1e-6f) continue;

            if (d2 < bestSqr)
            {
                bestSqr = d2;
                bestEnemy = enemy;
            }
        }

        // バッファのクリーンアップ
        for (int i = 0; i < count; ++i) sphereBuffer[i] = null;

        // 最寄りの敵が見つかったら正規化した XZ ベクトルを返す
        if (bestEnemy != null)
        {
            dirXZ = bestEnemy.transform.position - _player.transform.position;
            dirXZ.y = 0f;
            if (dirXZ.sqrMagnitude < 1e-6f) return false;
            dirXZ.Normalize();
            return true;
        }
        return false;
    }
}
