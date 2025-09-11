using UnityEngine;

/// <summary>
/// ���S��ԁF��x��������Œ�B���͖����E���X�|�[���⃊�g���C�͕ʓrUI�����Ő���B
/// </summary>
public class PlayerDeadState : IState
{
    private PlayerMovement _player;

    public PlayerDeadState(PlayerMovement p) { _player = p; }

    private float _timer = 0f;
    private float _deadDuration = 3f; // ���S���[�V�����̒����ɍ��킹��

    public void OnEnter()
    {
        // ���{��F���S���C���[�փu�����h
        _player.BlendToState(PlayerState.Dead);

        // ���{��F�K�� 0 �b����Đ����āA�������S���[�V�����̖`�����猩����
        _player.ResetDeadClipPlayable();

        // ���{��F�K�v�Ȃ瓖���蔻��⑀��𖳌����i��j
        // _player.enabled = false; �Ȃ�
        _timer = 0f;
    }
    public void OnExit() { }

    public void OnUpdate(float dt)
    {
        _timer += dt;
        if (_timer >= _deadDuration)
        {
            // ���{��F���S���[�V�������I������牽������i��F���X�|�[���������ĂԂȂǁj
            GameManager.Instance?.GameOver();
            _timer = -10000f;
        }
    }
}