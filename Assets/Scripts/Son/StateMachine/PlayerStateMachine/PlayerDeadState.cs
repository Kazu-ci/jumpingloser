using UnityEngine;

/// <summary>
/// 死亡状態：終端。全入力無視・遷移不可。
/// </summary>
public class PlayerDeadState : IState
{
    private PlayerMovement _player;

    public PlayerDeadState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 日本語：死亡レイヤーへクロスフェード（遷移時間は設定で解決）
        _player.BlendToState(PlayerState.Dead);
    }

    public void OnExit() { }

    public void OnUpdate(float deltaTime)
    {
        // 日本語：何もしない（静的）
    }
}