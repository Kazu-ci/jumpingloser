using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// 被弾状態：即時に再生を開始し、クリップ終盤でHPを判定して Dead/Idle へ遷移する。
/// ・ダッシュ無敵などは TakeDamage 側で弾く設計
/// ・地上/空中を問わず発生させる（落下遷移は維持するが死亡はここでのみ確定）
/// </summary>
public class PlayerHitState : IState
{
    private PlayerMovement _player;
    private float timer;
    private float length;

    // 日本語：終盤チェックの猶予（クリップ終了のこの秒数手前でHP判定）
    private const float ExitCheckWindow = 0.08f;

    public PlayerHitState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 日本語：被弾レイヤーへクロスフェード
        _player.BlendToState(PlayerState.Hit);

        // 日本語：必ず 0 秒から再生（ClampForever 対策）
        _player.ResetHitClipPlayable();

        // 日本語：クリップ長（未設定時のフェイルセーフ）
        length = (_player.hitClip != null) ? _player.hitClip.length : 0.2f;
        timer = 0f;
    }
    public void OnExit()
    {
        // 特になし
    }

    public void OnUpdate(float deltaTime)
    {
        // 日本語：空中に出たら Falling へ（死亡確定はこのステート内のみで行う）
        if (!_player.IsGrounded)
        {
            _player.HandleFalling();
            return;
        }

        timer += deltaTime;

        // 日本語：終盤で HP 判定 → Dead / Idle or Move
        if (timer >= Mathf.Max(0.01f, length - 0.08f))
        {
            if (_player.CurrentHealth <= 0f)
            {
                // 日本語：死亡は被弾からのみ
                _player.ExecuteTriggerExternal(PlayerTrigger.Die);
                return;
            }

            // 日本語：入力の有無で戻り先を決定
            if (_player.HasMoveInput())
                _player.ExecuteTriggerExternal(PlayerTrigger.MoveStart); // Move へ
            else
                _player.ExecuteTriggerExternal(PlayerTrigger.MoveStop);  // Idle へ
        }
    }
}