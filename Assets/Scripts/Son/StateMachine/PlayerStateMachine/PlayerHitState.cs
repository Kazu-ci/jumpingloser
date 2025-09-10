using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// ��e��ԁF�n��Ŕ����B�Đ��I����Idle/Move�ɖ߂�B���n������Falling�ցB
/// </summary>
public class PlayerHitState : IState
{
    private PlayerMovement _player;
    private float timer;
    private float length;

    public PlayerHitState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // ���{��F��e���C���[�փN���X�t�F�[�h�i�J�ڎ��Ԃ͐ݒ�ŉ����j
        _player.BlendToState(PlayerState.Hit);

        // ���{��F��e�N���b�v���i0�▢�ݒ�Ȃ瑁�����A�j
        length = (_player.hitClip != null) ? _player.hitClip.length : 0.2f;
        timer = 0f;
    }

    public void OnExit() { }

    public void OnUpdate(float deltaTime)
    {
        // ���n������Falling��
        if (!_player.GetComponent<CharacterController>().isGrounded)
        {
            _player.HandleFalling();
            return;
        }

        timer += deltaTime;
        if (timer >= Mathf.Max(0.05f, length))
        {
            // ���{��F���̗͂L����Idle/Move�ցi�ŏ����W�b�N�j
            var hasMove = _player != null && _player.GetComponent<PlayerMovement>() != null;
            // ���ۂ̓��͔��f��PlayerMovement��Move���͂�����
            // �V���v����Idle�֕Ԃ��i�㑱��MoveStart���o�ł��j
            _player.ToIdle();
        }
    }
}