using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// ���{��F�X�L����ԁi�P���Łj�B�S�g�͎�~�L�T�[��Action�X���b�g�ŐڊǁB
//        �U���Ɠ��l�Ɏq�~�L�T�[���g�����A�i��1�z��ŊȈՎ����B
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
        // ���{��F����̃X�L���N���b�v���擾�i�Ȃ���΃t�H�[���o�b�N�j
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

        // ���{��F�q�~�L�T�[�i1���͂ł�����Ă����Ɗg�����₷���j
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
        // ���{��FIK�𖳌����i���Ճ��^�[���}���j
        skillPlayable.SetApplyFootIK(false);
        skillPlayable.SetApplyPlayableIK(false);
        skillPlayable.SetTime(0);
        skillPlayable.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed));

        skillSubMixer.ConnectInput(0, skillPlayable, 0, 1f);

        // ���{��F��~�L�T�[��Action�X���b�g�ɍ����ւ�
        int actionSlot = (int)PlayerMovement.MainLayerSlot.Action;
        _player.mixer.DisconnectInput(actionSlot);
        _player.mixer.ConnectInput(actionSlot, skillSubMixer, 0, 1f);

        // ���{��F�S�g�ڊǁi�ɒZ�j
        float enterDur = Mathf.Max(0.0f, _player.ResolveBlendDuration(PlayerState.Move, PlayerState.Skill));
        if (enterDur > 0.06f) enterDur = 0.03f;
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);
    }

    public void OnExit()
    {
        if (skillPlayable.IsValid()) skillPlayable.Destroy();

        // ���{��F�f�����P��
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