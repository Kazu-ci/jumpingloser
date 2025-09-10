using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerIdleState : IState
{
    private PlayerMovement _player;

    public PlayerIdleState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        //Debug.Log("Enter Idle");
        _player.BlendToState(PlayerState.Idle);
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