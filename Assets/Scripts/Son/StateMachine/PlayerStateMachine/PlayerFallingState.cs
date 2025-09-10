using UnityEngine;

/// <summary>
/// 落下状態：離地時に遷移。入力は無視。着地でIdle/Moveへ。
/// </summary>
public class PlayerFallingState : IState
{
    private PlayerMovement _player;

    public PlayerFallingState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 落下レイヤーへクロスフェード（遷移時間は設定で解決）
        _player.BlendToState(PlayerState.Falling);
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