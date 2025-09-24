using UnityEngine;
using static EventBus;
using System.Collections;

public enum GameState
{
    Startup,     // アプリ起動直後の軽量初期化
    Title,       // タイトル画面
    Preloading,  // ゲームプレイ用アセット読み込み
    Preloading2, // ゲームプレイ用アセット読み込み2
    Preloading3, // ゲームプレイ用アセット読み込み3
    Playing,     // プレイ中（サブ状態機が動く）
    Paused,      // ポーズ（HUD 非表示 + 操作停止）
    Result,      // リザルト画面
    GameOver,     // ゲームオーバー画面
    Intro,       // オープニング
    PlayDataResult // プレイデータリザルト
}

/// <summary>
/// ゲーム状態遷移を発火させるトリガー
/// </summary>
public enum GameTrigger
{
    ToTitle,
    StartGame,
    EnterStage2,
    FinishLoading,
    Pause,
    Resume,
    GameOver,
    GameClear,
    ToIntro,
    ToResultData
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    /*//--------------------------------倉岡追加部分-----------------------------
    //現在のシーン名などの保持
    public string CurrentStageSceneName { get; private set; }
    public bool IsBossStage { get; private set; }
    //呼び出しメソッド
    public void SetStageInfo(string sceneName, bool isBossStage)
    {
        CurrentStageSceneName = sceneName;
        IsBossStage = isBossStage;
    }
    //--------------------------------------------------------------------------*/
    /// <summary>メイン状態機</summary>
    private StateMachine<GameState, GameTrigger> _stateMachine;

    private bool _isTimePaused = false;
    private bool _sceneLoadedFlag = false;

    /// <summary>現在の状態を公開</summary>
    public GameState CurrentState = GameState.Startup;
    private GameState getCurrentState => _stateMachine != null ? _stateMachine.CurrentState : GameState.Startup;
    public GameState preGameState = GameState.Startup;

    private GameTrigger PreSceneTrigger = GameTrigger.ToTitle;
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
                SystemEvents.OnGameStateChange?.Invoke(GameState.Title); 
                SetScaleTimeTo1();
            },
            onExit: () => { preGameState = GameState.Title; PreSceneTrigger = GameTrigger.ToTitle; }
        );
        _stateMachine.SetupState(
            GameState.Preloading,
            onEnter: null,
            onExit: () => { preGameState = GameState.Preloading; PreSceneTrigger = GameTrigger.StartGame; },
            enterRoutine: EnterPreloadingRoutine
            );
        _stateMachine.SetupState(
            GameState.Preloading2,
            onEnter: null,
            onExit: () => { preGameState = GameState.Preloading2; PreSceneTrigger = GameTrigger.EnterStage2; },
            enterRoutine: EnterPreloadingRoutine
            );

        _stateMachine.SetupState(
            GameState.Preloading3,
            onEnter: null,
            onExit: () => { preGameState = GameState.Preloading3; PreSceneTrigger = GameTrigger.EnterStage2; },
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

        _stateMachine.SetupState(
            GameState.GameOver,
            onEnter: () =>
            {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.GameOver);
            },
            onExit: () =>
            {
                preGameState = GameState.GameOver;
            }
        );
        _stateMachine.SetupState(
            GameState.Intro,
            onEnter: () =>
            {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.Intro);
            },
            onExit: () => { preGameState = GameState.Intro; }
        );
        _stateMachine.SetupState(
            GameState.PlayDataResult,
            onEnter: () =>
            {
                CurrentState = getCurrentState;
                SystemEvents.OnGameStateChange?.Invoke(GameState.PlayDataResult);
            },
            onExit: () => { preGameState = GameState.PlayDataResult;}
        );



        // Startup
        _stateMachine.AddTransition(GameState.Startup, GameState.Title, GameTrigger.FinishLoading);

        // Title
        _stateMachine.AddTransition(GameState.Title, GameState.Intro, GameTrigger.ToIntro);

        // Intro
        _stateMachine.AddTransition(GameState.Intro, GameState.Preloading, GameTrigger.StartGame);

        // Preloading
        _stateMachine.AddTransition(GameState.Preloading, GameState.Playing, GameTrigger.FinishLoading);

        // Preloading2
        _stateMachine.AddTransition(GameState.Preloading2, GameState.Playing, GameTrigger.FinishLoading);

        // Preloading3
        _stateMachine.AddTransition(GameState.Preloading3, GameState.PlayDataResult, GameTrigger.FinishLoading);

        // Playing
        _stateMachine.AddTransition(GameState.Playing, GameState.Paused, GameTrigger.Pause);
        _stateMachine.AddTransition(GameState.Playing, GameState.Preloading3, GameTrigger.ToResultData);
        _stateMachine.AddTransition(GameState.Playing, GameState.Preloading2, GameTrigger.EnterStage2);
        _stateMachine.AddTransition(GameState.Playing, GameState.GameOver, GameTrigger.GameOver);
        _stateMachine.AddTransition(GameState.Playing, GameState.Title, GameTrigger.ToTitle);

        // Paused
        _stateMachine.AddTransition(GameState.Paused, GameState.Playing, GameTrigger.Resume);
        _stateMachine.AddTransition(GameState.Paused, GameState.Title, GameTrigger.ToTitle);

        // Result
        _stateMachine.AddTransition(GameState.Result, GameState.Title, GameTrigger.ToTitle);

        // PlayDataResult
        _stateMachine.AddTransition(GameState.PlayDataResult, GameState.Result, GameTrigger.GameClear);

        // GameOver
        _stateMachine.AddTransition(GameState.GameOver, GameState.Title, GameTrigger.ToTitle);
        _stateMachine.AddTransition(GameState.GameOver, GameState.Preloading, GameTrigger.StartGame);
        _stateMachine.AddTransition(GameState.GameOver, GameState.Preloading2, GameTrigger.EnterStage2);


        _stateMachine.ExecuteTrigger(GameTrigger.FinishLoading);
    }

    /// <summary>
    /// アドレスアブル経由でゲームプレイ用アセットを読み込む。
    /// </summary>
    private IEnumerator EnterPreloadingRoutine()
    {
        CurrentState = getCurrentState;
        _sceneLoadedFlag = false;
        SystemEvents.OnGameStateChange?.Invoke(CurrentState);

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
    /// 
    public void ToIntro() => _stateMachine.ExecuteTrigger(GameTrigger.ToIntro);
    public void StartGame() => _stateMachine.ExecuteTrigger(GameTrigger.StartGame);
    public void EnterStage2() => _stateMachine.ExecuteTrigger(GameTrigger.EnterStage2);
    public void PauseGame() => _stateMachine.ExecuteTrigger(GameTrigger.Pause);
    public void ResumeGame() => _stateMachine.ExecuteTrigger(GameTrigger.Resume);
    public void GameOver() => _stateMachine.ExecuteTrigger(GameTrigger.GameOver);
    public void ReTry()=> _stateMachine.ExecuteTrigger(PreSceneTrigger);
    public void GameClear() => _stateMachine.ExecuteTrigger(GameTrigger.ToResultData);
    public void ToTitle() => _stateMachine.ExecuteTrigger(GameTrigger.ToTitle);
    public void ToEndRoll() => _stateMachine.ExecuteTrigger(GameTrigger.GameClear);
}
