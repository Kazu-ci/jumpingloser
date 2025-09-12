using UnityEngine;

public class PlayerDashState : IState
{
    private PlayerMovement _player;

    public PlayerDashState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // �������C���[�փN���X�t�F�[�h�i�J�ڎ��Ԃ͐ݒ�ŉ����j
        _player.BlendToState(PlayerState.Dash);
    }

    public void OnExit()
    {
        // ���ɂȂ�
    }

    public void OnUpdate(float deltaTime)
    {
        _player.HandleMovement(deltaTime);
        if (_player.isHitboxVisible) { /* �f�o�b�O������ */ }
    }
}
