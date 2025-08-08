using UnityEngine;
using System.Collections;

public enum GameState
{
    Startup,     // アプリ起動直後の軽量初期化
    Title,       // タイトル画面
    Preloading,  // ゲームプレイ用アセット読み込み
    Playing,     // プレイ中（サブ状態機が動く）
    Paused,      // ポーズ（HUD 非表示 + 操作停止）
    Result       // リザルト画面
}

/// <summary>
/// ゲーム状態遷移を発火させるトリガー
/// </summary>
public enum GameTrigger
{
    ToTitle,
    StartGame,
    FinishLoading,
    Pause,
    Resume,
    GameOver
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>メイン状態機</summary>
    private StateMachine<GameState, GameTrigger> _stateMachine;

    private Coroutine _loadingRoutine;
    
    private void Awake()
    {
        // シングルトン保証
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeStateMachine();
    }
    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<GameState, GameTrigger>(this,GameState.Startup);

/*
        _stateMachine.RegisterState(GameState.Playing, new GamePlayingState(this);
        _stateMachine.RegisterState(GameState.Paused, new GamePausedState(this));
        _stateMachine.RegisterState(GameState.Result, new GameResultState(this));
        _stateMachine.RegisterState(GameState.Title, new GameTitleState(this));
        _stateMachine.RegisterState(GameState.Preloading, new GamePreloadingState(this));
        _stateMachine.RegisterState(GameState.Startup, new GameStartupState(this));*/
        
        
        // Startup
        _stateMachine.AddTransition(GameState.Startup,GameState.Title,GameTrigger.FinishLoading);

        // Title
        _stateMachine.AddTransition(GameState.Title, GameState.Preloading, GameTrigger.StartGame);

        // Preloading
        _stateMachine.AddTransition(GameState.Preloading, GameState.Playing, GameTrigger.FinishLoading);

        // Playing
        _stateMachine.AddTransition(GameState.Playing, GameState.Paused, GameTrigger.Pause);
        _stateMachine.AddTransition(GameState.Playing, GameState.Result, GameTrigger.GameOver);

        // Paused
        _stateMachine.AddTransition(GameState.Paused, GameState.Playing, GameTrigger.Resume);
        _stateMachine.AddTransition(GameState.Paused, GameState.Result, GameTrigger.GameOver);
        _stateMachine.AddTransition(GameState.Paused, GameState.Title, GameTrigger.ToTitle);

        // Result
        _stateMachine.AddTransition(GameState.Result, GameState.Title, GameTrigger.ToTitle);
    }

    /// <summary>
    /// アドレスアブル経由でゲームプレイ用アセットを読み込む。
    /// </summary>
    /*private IEnumerator LoadGameplayAssets()
    {
        // アセットバンドル / Addressables の読み込み（詳細は実装者へ委任）
        yield return AddressableLoader.LoadGameplayGroup();
        _stateMachine.ExecuteTrigger(GameTrigger.FinishLoading);
    }*/

    /// <summary>
    /// タイトル画面のスタートボタンから呼ばれる。
    /// </summary>
    public void StartGame() => _stateMachine.ExecuteTrigger(GameTrigger.StartGame);
    public void PauseGame() => _stateMachine.ExecuteTrigger(GameTrigger.Pause);
    public void ResumeGame() => _stateMachine.ExecuteTrigger(GameTrigger.Resume);
    public void GameOver() => _stateMachine.ExecuteTrigger(GameTrigger.GameOver);
    public void ToTitle() => _stateMachine.ExecuteTrigger(GameTrigger.ToTitle);
}
