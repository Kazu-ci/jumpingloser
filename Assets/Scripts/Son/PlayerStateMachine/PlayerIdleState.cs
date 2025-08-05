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

        _player.mixer.SetInputWeight(0, 1f);
        _player.mixer.SetInputWeight(1, 0f);
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