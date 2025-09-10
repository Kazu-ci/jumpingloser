using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 日本語：スキル状態（単発版）。全身は主ミキサーのActionスロットで接管。
//        攻撃と同様に子ミキサーを使うが、段は1つ想定で簡易実装。
public class PlayerSkillState : IState
{
    private PlayerMovement _player;
    private AnimationMixerPlayable skillSubMixer;
    private AnimationClipPlayable skillPlayable;

    private ComboAction action;
    private double duration;
    private double elapsed;
    private WeaponInstance weapon;

    public PlayerSkillState(PlayerMovement p) { _player = p; }

    public void OnEnter()
    {
        // 日本語：武器のスキルクリップを取得（なければフォールバック）
        weapon = _player.GetMainWeapon() ?? _player.fist;
        var list = weapon?.template?.finisherAttack;
        if (list == null || list.Count == 0) list = weapon?.template?.mainWeaponCombo;

        if (list == null || list.Count == 0 || list[0]?.animation == null)
        {
            _player.ToIdle();
            return;
        }

        action = list[0];
        duration = action.animation.length;
        elapsed = 0.0;

        // 日本語：子ミキサー（1入力でも作っておくと拡張しやすい）
        if (!skillSubMixer.IsValid())
        {
            skillSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 1);
            skillSubMixer.SetInputCount(1);
        }

        if (skillPlayable.IsValid())
        {
            skillSubMixer.DisconnectInput(0);
            skillPlayable.Destroy();
        }

        skillPlayable = AnimationClipPlayable.Create(_player.playableGraph, action.animation);
        // 日本語：IKを無効化（骨盤リターン抑制）
        skillPlayable.SetApplyFootIK(false);
        skillPlayable.SetApplyPlayableIK(false);
        skillPlayable.SetTime(0);
        skillPlayable.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed));

        skillSubMixer.ConnectInput(0, skillPlayable, 0, 1f);

        // 日本語：主ミキサーのActionスロットに差し替え
        int actionSlot = (int)PlayerMovement.MainLayerSlot.Action;
        _player.mixer.DisconnectInput(actionSlot);
        _player.mixer.ConnectInput(actionSlot, skillSubMixer, 0, 1f);

        // 日本語：全身接管（極短）
        float enterDur = Mathf.Max(0.0f, _player.ResolveBlendDuration(PlayerState.Move, PlayerState.Skill));
        if (enterDur > 0.06f) enterDur = 0.03f;
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);
    }

    public void OnExit()
    {
        if (skillPlayable.IsValid()) skillPlayable.Destroy();

        // 日本語：素早く撤退
        float exitDur = Mathf.Max(0.0f, _player.ResolveBlendDuration(PlayerState.Skill, PlayerState.Idle));
        if (exitDur > 0.06f) exitDur = 0.03f;
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Idle, exitDur);
    }

    public void OnUpdate(float dt)
    {
        elapsed += dt;
        if (elapsed >= duration)
        {
            _player.ToIdle();
        }
    }
}