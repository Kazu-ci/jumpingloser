using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerIdleState : IState
{
    private PlayerMovement _player;
    private float lockOnRotateTime = 0.3f;

    public PlayerIdleState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        //Debug.Log("Enter Idle");
        _player.BlendToState(PlayerState.Idle);

        Vector3 targetDir = Vector3.zero;
        if (_player.TryGetLockOnHorizontalDirection(out targetDir))
        {
            _player.RotateYawOverTime(targetDir, lockOnRotateTime);
        }
    }

    public void OnExit()
    {
        //Debug.Log("Exit Idle");
    }

    public void OnUpdate(float deltaTime)
    {
        _player.CheckMoveInput();
    }
}