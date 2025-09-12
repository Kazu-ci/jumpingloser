using UnityEngine;

public class PlayerDashState : IState
{
    private PlayerMovement _player;

    public PlayerDashState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 落下レイヤーへクロスフェード（遷移時間は設定で解決）
        _player.BlendToState(PlayerState.Dash);
    }

    public void OnExit()
    {
        // 特になし
    }

    public void OnUpdate(float deltaTime)
    {
        _player.HandleMovement(deltaTime);
        if (_player.isHitboxVisible) { /* デバッグ可視化等 */ }
    }
}
