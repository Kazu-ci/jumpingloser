using UnityEngine;
using static EventBus;

/// <summary>
/// ダッシュ状態：
/// ・アニメを 0 秒から再生し、dashFreezeAtSeconds 到達で一時停止（ポーズ保持）
/// ・移動（LungeManager）が完了するまで状態は維持
/// ・完了時にアニメ再開しつつ、他状態へブレンドして離脱
/// </summary>
public class PlayerDashState : IState
{
    private PlayerMovement _player;
    private LungeManager _lm;

    private Vector3 _dashDir;
    private bool _poseFrozen;     // 日本語：ポーズ固定済みか
    private bool _lungeIssued;    // 日本語：ラウンジ開始済みか

    private double _dashLen;      // 日本語：クリップ長
    private double _freezeAt;     // 日本語：停止させる絶対時刻（秒）

    public PlayerDashState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        // 日本語：クールタイム未解消なら即復帰（保険）
        if (!_player.IsDashCooldownReady())
        {
            ExitToLocomotion();
            return;
        }

        // 日本語：アニメ層へブレンド & 0秒から再生（必ず先頭から）
        _player.BlendToState(PlayerState.Dash);
        _player.ResetDashClipPlayable();

        // 日本語：停止時刻（秒）をクリップ長にクランプ
        _dashLen = _player.GetDashClipLength();
        _freezeAt = Mathf.Clamp(_player.dashFreezeAtSeconds, 0f, (float)_dashLen);
        _poseFrozen = false;

        // 日本語：方向決定（カメラ相対／小入力は正面を使用）
        _dashDir = _player.ResolveDashDirectionWorld(0.01f);
        if (_dashDir.sqrMagnitude > 1e-6f)
            _player.transform.rotation = Quaternion.LookRotation(_dashDir, Vector3.up);

        // 日本語：無敵開始（終了時に確実にオフ）
        _player.StartDashInvincibility();

        // 日本語：LungeManager の終了イベントを購読
        _lm = _player.GetComponent<LungeManager>();
        if (_lm != null)
        {
            _lm.OnLungeFinish += OnLungeEnd;
            _lm.OnLungeBlocked += OnLungeEnd;
            _lm.OnLungeTooSteep += OnLungeEnd;
        }

        // 日本語：距離ベースの突進をイベントで起動（ダッシュは移動完了まで継続）
        PlayerEvents.LungeByDistance?.Invoke(
            LungeManager.LungeAim.CustomDir,
            Vector3.zero,
            _dashDir,
            _player.dashSpeed,
            _player.dashDistance,
            null
        );
        _lungeIssued = true;
        UIEvents.OnDashUIChange?.Invoke(false);
    }

    public void OnExit()
    {
        // 日本語：無敵オフ（保険）
        _player.EndDashInvincibility();

        // 日本語：購読解除／進行中なら中断
        if (_lm != null)
        {
            _lm.OnLungeFinish -= OnLungeEnd;
            _lm.OnLungeBlocked -= OnLungeEnd;
            _lm.OnLungeTooSteep -= OnLungeEnd;

            if (_lungeIssued) _lm.ForceCancel(false);
        }

        // 日本語：ダッシュアニメ速度を復帰（外部影響を残さない）
        _player.SetDashPlayableSpeed(1.0);

        // 日本語：クールタイム刻印
        _player.MarkDashCooldown();
    }

    public void OnUpdate(float deltaTime)
    {
        // 日本語：停止時刻に到達したら、その時刻で再生を停止（ポーズ保持）
        if (!_poseFrozen)
        {
            double t = _player.GetDashPlayableTime();
            if (t >= _freezeAt)
            {
                _player.SetDashPlayableTime(_freezeAt);
                _player.SetDashPlayableSpeed(0.0); // ★ 以後はそのポーズで静止
                _poseFrozen = true;
            }
        }
        // 日本語：移動は LungeManager が担当。状態は移動完了まで維持。
    }

    // === LungeManager 側が「移動完了/阻止/急斜面」いずれかで呼ぶ ===
    private void OnLungeEnd()
    {
        // 日本語：まず無敵を終了
        _player.EndDashInvincibility();

        // 日本語：アニメ再開（固定ポーズから後半を軽く再生しつつブレンド開始）
        _player.SetDashPlayableSpeed(1.0);
        // ※ 必要なら微小オフセットを足してサンプル安定性を上げても良い：
        // _player.SetDashPlayableTime(Mathf.Min((float)(_freezeAt + 1e-3), (float)_dashLen - 1e-4));

        // 日本語：他状態への離脱（入力の有無で Idle/Move へ）
        ExitToLocomotion();
    }

    // 日本語：入力の有無で Idle/Move に復帰
    private void ExitToLocomotion()
    {
        if (_player.HasMoveInput())
            _player.ExecuteTriggerExternal(PlayerTrigger.MoveStart);
        else
            _player.ExecuteTriggerExternal(PlayerTrigger.MoveStop);
    }
}
