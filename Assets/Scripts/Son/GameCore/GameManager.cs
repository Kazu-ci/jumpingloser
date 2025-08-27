using UnityEngine;
using System.Collections;

public enum GameState
{
    Startup,     // �A�v���N������̌y�ʏ�����
    Title,       // �^�C�g�����
    Preloading,  // �Q�[���v���C�p�A�Z�b�g�ǂݍ���
    Playing,     // �v���C���i�T�u��ԋ@�������j
    Paused,      // �|�[�Y�iHUD ��\�� + �����~�j
    Result       // ���U���g���
}

/// <summary>
/// �Q�[����ԑJ�ڂ𔭉΂�����g���K�[
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

    /// <summary>���C����ԋ@</summary>
    private StateMachine<GameState, GameTrigger> _stateMachine;

    private Coroutine _loadingRoutine;
    
    private void Awake()
    {
        // �V���O���g���ۏ�
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
    /// �A�h���X�A�u���o�R�ŃQ�[���v���C�p�A�Z�b�g��ǂݍ��ށB
    /// </summary>
    /*private IEnumerator LoadGameplayAssets()
    {
        // �A�Z�b�g�o���h�� / Addressables �̓ǂݍ��݁i�ڍׂ͎����҂ֈϔC�j
        yield return AddressableLoader.LoadGameplayGroup();
        _stateMachine.ExecuteTrigger(GameTrigger.FinishLoading);
    }*/

    /// <summary>
    /// �^�C�g����ʂ̃X�^�[�g�{�^������Ă΂��B
    /// </summary>
    public void StartGame() => _stateMachine.ExecuteTrigger(GameTrigger.StartGame);
    public void PauseGame() => _stateMachine.ExecuteTrigger(GameTrigger.Pause);
    public void ResumeGame() => _stateMachine.ExecuteTrigger(GameTrigger.Resume);
    public void GameOver() => _stateMachine.ExecuteTrigger(GameTrigger.GameOver);
    public void ToTitle() => _stateMachine.ExecuteTrigger(GameTrigger.ToTitle);
}
