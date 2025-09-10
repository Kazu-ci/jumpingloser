using UnityEngine;

/// <summary>
/// ���S��ԁF�I�[�B�S���͖����E�J�ڕs�B
/// </summary>
public class PlayerDeadState : IState
{
    private PlayerMovement _player;

    public PlayerDeadState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // ���{��F���S���C���[�փN���X�t�F�[�h�i�J�ڎ��Ԃ͐ݒ�ŉ����j
        _player.BlendToState(PlayerState.Dead);
    }

    public void OnExit() { }

    public void OnUpdate(float deltaTime)
    {
        // ���{��F�������Ȃ��i�ÓI�j
    }
}