using UnityEngine;
using static EventBus;
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

    private bool _isTimePaused = false;
    private bool _sceneLoadedFlag = false;

    /// <summary>現在の状態を公開</summary>
    public GameState CurrentState = GameState.Startup;
    private GameState getCurrentState => _stateMachine != null ? _stateMachine.CurrentState : GameState.Startup;
    public GameState preGameState = GameState.Startup;
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
    private void Start()
    {
        Application.targetFrameRate = 60;
    }
    private void OnEnable()
    {
        SystemEvents.OnGamePause += SetScaleTimeTo0;
        SystemEvents.OnGameResume += SetScaleTimeTo1;
        SystemEvents.OnGameExit += HandleGameExit;
        SystemEvents.OnSceneLoadComplete += HandleSceneLoaded;
    }
    private void OnDisable()
    {
        SystemEvents.OnGamePause -= SetScaleTimeTo0;
        SystemEvents.OnGameResume -= SetScaleTimeTo1;
        SystemEvents.OnGameExit -= HandleGameExit;
        SystemEvents.OnSceneLoadComplete -= HandleSceneLoaded;
    }
    private void Update()
    {
        _stateMachine.Update(Time.deltaTime);
        if(Input.GetKeyDown(KeyCode.Tab)) {HandleGameExit();}
    }
    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<GameState, GameTrigger>(this, GameState.Startup);

        _stateMachine.SetupState(
                    GameState.Startup,
                    onEnter: () =>
                    {
                        CurrentState = getCurrentState;
                        SystemEvents.OnGameStateChange?.Invoke(GameState.Startup);
                    },
                    onExit: () =>
                    {
                        preGameState = GameState.Startup;
                    }
                );

        _stateMachine.SetupState(
            GameState.Title,
            onEnter: () => {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.Title); },
            onExit: () => { preGameState = GameState.Title; }
        );
        _stateMachine.SetupState(
            GameState.Preloading,
            onEnter: null,
            onExit: () => { preGameState = GameState.Preloading; },
            enterRoutine: EnterPreloadingRoutine
            );
        _stateMachine.SetupState(
            GameState.Playing,
            onEnter: () => {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.Playing); },
            onExit: () => { preGameState = GameState.Playing; },
            onUpdate: null
            );
        _stateMachine.SetupState(
            GameState.Result,
            onEnter: () => {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.Result); },
            onExit: () => { preGameState = GameState.Result; }
        );

        _stateMachine.SetupState(
            GameState.Paused,
            onEnter: () =>
            {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.Paused);
                _isTimePaused = true;
                SystemEvents.OnGamePause?.Invoke();
            },
            onExit: () =>
            {
                _isTimePaused = false;
                SystemEvents.OnGameResume?.Invoke();
            }
        );



        // Startup
        _stateMachine.AddTransition(GameState.Startup, GameState.Title, GameTrigger.FinishLoading);

        // Title
        _stateMachine.AddTransition(GameState.Title, GameState.Preloading, GameTrigger.StartGame);

        // Preloading
        _stateMachine.AddTransition(GameState.Preloading, GameState.Playing, GameTrigger.FinishLoading);

        // Playing
        _stateMachine.AddTransition(GameState.Playing, GameState.Paused, GameTrigger.Pause);
        _stateMachine.AddTransition(GameState.Playing, GameState.Result, GameTrigger.GameOver);
        //_stateMachine.AddTransition(GameState.Playing, GameState.Preloading, GameTrigger.StartGame);

        // Paused
        _stateMachine.AddTransition(GameState.Paused, GameState.Playing, GameTrigger.Resume);
        _stateMachine.AddTransition(GameState.Paused, GameState.Result, GameTrigger.GameOver);
        _stateMachine.AddTransition(GameState.Paused, GameState.Title, GameTrigger.ToTitle);

        // Result
        _stateMachine.AddTransition(GameState.Result, GameState.Title, GameTrigger.ToTitle);

        _stateMachine.ExecuteTrigger(GameTrigger.FinishLoading);
    }

    /// <summary>
    /// アドレスアブル経由でゲームプレイ用アセットを読み込む。
    /// </summary>
    private IEnumerator EnterPreloadingRoutine()
    {
        CurrentState = getCurrentState;
        _sceneLoadedFlag = false;
        SystemEvents.OnGameStateChange?.Invoke(GameState.Preloading);

        while (!_sceneLoadedFlag)
            yield return null;

        // ロード完了
        _stateMachine.ExecuteTrigger(GameTrigger.FinishLoading);

        // Exit 通知は状態遷移時に呼ばれるため、このコルーチンでは不要
        yield break;
    }
    private void SetScaleTimeTo0() { Time.timeScale = 0; }
    private void SetScaleTimeTo1() { Time.timeScale = 1; }
    private void HandleGameExit(){ Application.Quit(); }
    private void HandleSceneLoaded()
    {
        _sceneLoadedFlag = true;
    }

    /// <summary>
    /// タイトル画面のスタートボタンから呼ばれる。
    /// </summary>
    public void StartGame() => _stateMachine.ExecuteTrigger(GameTrigger.StartGame);
    public void PauseGame() => _stateMachine.ExecuteTrigger(GameTrigger.Pause);
    public void ResumeGame() => _stateMachine.ExecuteTrigger(GameTrigger.Resume);
    public void GameOver() => _stateMachine.ExecuteTrigger(GameTrigger.GameOver);
    public void ToTitle() => _stateMachine.ExecuteTrigger(GameTrigger.ToTitle);
}
