using UnityEngine;

/// <summary>
/// 死亡状態：一度入ったら固定。入力無効・リスポーンやリトライは別途UI側等で制御。
/// </summary>
public class PlayerDeadState : IState
{
    private PlayerMovement _player;

    public PlayerDeadState(PlayerMovement p) { _player = p; }

    private float _timer = 0f;
    private float _deadDuration = 3f; // 死亡モーションの長さに合わせる

    public void OnEnter()
    {
        // 死亡レイヤーへブレンド
        _player.BlendToState(PlayerState.Dead);

        // 必ず 0 秒から再生して、長い死亡モーションの冒頭から見せる
        _player.ResetDeadClipPlayable();

        // 必要なら当たり判定や操作を無効化（例）
        // _player.enabled = false; など
        _timer = 0f;
    }
    public void OnExit() { }

    public void OnUpdate(float dt)
    {
        _timer += dt;
        if (_timer >= _deadDuration)
        {
            // 死亡モーションが終わったら何かする
            GameManager.Instance?.GameOver();
            _timer = -10000f;
        }
    }
}