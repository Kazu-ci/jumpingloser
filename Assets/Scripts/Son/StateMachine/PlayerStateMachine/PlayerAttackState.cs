using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;
    private AnimationClipPlayable attackPlayable;
    private double attackDuration;
    private double elapsedTime;
    private bool hasCheckedHit;
    private DamageData damageData = new DamageData(1); // ���̃_���[�W�f�[�^

    public PlayerAttackState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        Debug.Log("Enter Attack");

        // ���C������擾
        WeaponInstance weapon = _player.GetMainWeapon();
        if (weapon == null || weapon.template.mainWeaponCombo.Count == 0)
        {
            Debug.LogWarning("Weapon or attack clips not found.");
            _player.ToIdle();
            return;
        }

        // �ŏ���ComboAction�擾�i�����ł͒P���ɐ擪�Ƃ���j
        ComboAction action = weapon.template.mainWeaponCombo[0];
        damageData = new DamageData(weapon.template.attackPower);

        // ClipPlayable�쐬
        attackPlayable = AnimationClipPlayable.Create(_player.playableGraph, action.animation);
        attackPlayable.SetDuration(action.animation.length);

        // Mixer��Input2�ɐڑ�����iIndex 2��Attack��p�ɂ���z��j
        _player.EnsureMixerInputCount(3);
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackPlayable, 0, 1f);

        // Weight�ݒ�
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack

        // Graph�X�V
        _player.playableGraph.Evaluate();

        attackDuration = action.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
    }

    public void OnExit()
    {
        Debug.Log("Exit Attack");

        _player.mixer.SetInputWeight(2, 0f);

        if (attackPlayable.IsValid())
        {
            attackPlayable.Destroy();
        }
    }

    public void OnUpdate(float deltaTime)
    {
        elapsedTime += deltaTime;

        // ����0.2�b��ɔ��肷��i���AnimationEvent�ɒu�������j
        if (!hasCheckedHit && elapsedTime >= 0.2f)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        if (elapsedTime >= attackDuration)
        {
            _player.ToIdle();
        }
    }

    private void DoAttackHitCheck()
    {
        float attackRange = 2.0f;
        float boxWidth = 3f;
        float boxHeight = 3f;

        Vector3 center = _player.transform.position
                         + _player.transform.forward * (attackRange * 0.5f)
                         + Vector3.up * 0.5f;

        Vector3 halfExtents = new Vector3(boxWidth * 0.5f, boxHeight * 0.5f, attackRange * 0.5f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

        ShowHitBox(center, halfExtents * 2, _player.transform.rotation, 0.5f, Color.red);

        bool hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"Enemy hit: {hit.gameObject.name}");
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    try
                    {
                        enemy.TakeDamage(damageData);
                        hitEnemy = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Enemy.TakeDamage threw: {e.Message}");
                    }
                }
                
            }
        }

        if (hitEnemy)
        {
            WeaponInstance weapon = _player.GetMainWeapon();
            ComboAction action = weapon.template.mainWeaponCombo[0];
            weapon.Use(action.durabilityCost);
            _player.tempUI?.UpdateWeapon(weapon);
            Debug.Log($"Weapon durability after attack: {weapon.currentDurability}");
        }
        else
        {
            Debug.Log("No enemy hit.");
        }
    }

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("AttackHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }
}