using UnityEngine;
using UnityEngine.Playables;

public class PlayerMoveState : IState
{
    private PlayerMovement _player;

    public PlayerMoveState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        //Debug.Log("Enter Move");
        _player.BlendToState(PlayerState.Move);
    }

    public void OnExit()
    {
        //Debug.Log("Exit Move");
    }

    public void OnUpdate(float deltaTime)
    {
        _player.HandleMovement(deltaTime);
        _player.CheckMoveStop();
    }
}