using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// 被弾状態：地上で発生。再生終了でIdle/Moveに戻る。離地したらFallingへ。
/// </summary>
public class PlayerHitState : IState
{
    private PlayerMovement _player;
    private float timer;
    private float length;

    public PlayerHitState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 日本語：被弾レイヤーへクロスフェード（遷移時間は設定で解決）
        _player.BlendToState(PlayerState.Hit);

        // 日本語：被弾クリップ長（0や未設定なら早期復帰）
        length = (_player.hitClip != null) ? _player.hitClip.length : 0.2f;
        timer = 0f;
    }

    public void OnExit() { }

    public void OnUpdate(float deltaTime)
    {
        // 離地したらFallingへ
        if (!_player.GetComponent<CharacterController>().isGrounded)
        {
            _player.HandleFalling();
            return;
        }

        timer += deltaTime;
        if (timer >= Mathf.Max(0.05f, length))
        {
            // 日本語：入力の有無でIdle/Moveへ（最小ロジック）
            var hasMove = _player != null && _player.GetComponent<PlayerMovement>() != null;
            // 実際の入力判断はPlayerMovementのMove入力を見る
            // シンプルにIdleへ返す（後続でMoveStart検出でも可）
            _player.ToIdle();
        }
    }
}