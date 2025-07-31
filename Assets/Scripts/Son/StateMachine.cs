using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 汎用ステートマシン（状態クラスと委譲両対応）
/// コルーチンによるEnter/Exit演出もサポート
/// </summary>
/// 

public interface IState
{
    void OnEnter();
    void OnExit();
    void OnUpdate(float deltaTime);
}
public class Transition<TState, TTrigger>
{
    public TState To { get; set; }
    public TTrigger Trigger { get; set; }
}


/// <summary>
/// 各ステートに紐づく振る舞い（クラス/委譲/コルーチン）を保持する
/// </summary>
public class StateMapping
{
    public IState stateClass;

    public Action onEnter;
    public Func<IEnumerator> enterRoutine;

    public Action onExit;
    public Func<IEnumerator> exitRoutine;

    public Action<float> onUpdate;

    /// <summary>
    /// ステート開始処理（コルーチン or 通常関数）
    /// </summary>
    public void Enter(MonoBehaviour runner)
    {
        if (stateClass != null) stateClass.OnEnter();
        else if (enterRoutine != null) runner.StartCoroutine(enterRoutine());
        else onEnter?.Invoke();
    }

    /// <summary>
    /// ステート終了処理（コルーチン or 通常関数）
    /// </summary>
    public void Exit(MonoBehaviour runner)
    {
        if (stateClass != null) stateClass.OnExit();
        else if (exitRoutine != null) runner.StartCoroutine(exitRoutine());
        else onExit?.Invoke();
    }

    /// <summary>
    /// 毎フレーム処理
    /// </summary>
    public void Update(float deltaTime)
    {
        if (stateClass != null) stateClass.OnUpdate(deltaTime);
        else onUpdate?.Invoke(deltaTime);
    }
}

public class StateMachine<TState, TTrigger>
    where TState : struct, IConvertible, IComparable
    where TTrigger : struct, IConvertible, IComparable
{
    private TState _current;
    private StateMapping _currentMapping;
    private readonly MonoBehaviour _mono;

    private Dictionary<object, StateMapping> _mappings = new();
    private Dictionary<TState, List<Transition<TState, TTrigger>>> _transitions = new();

    /// <summary>
    /// 初期化（MonoBehaviour注入でコルーチン使用可）
    /// </summary>
    public StateMachine(MonoBehaviour mono, TState initialState)
    {
        _mono = mono;

        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            _mappings[state] = new StateMapping();
        }

        ChangeState(initialState);
    }

    /// <summary>
    /// 状態クラスを登録する
    /// </summary>
    public void RegisterState(TState state, IState stateImpl)
    {
        _mappings[state].stateClass = stateImpl;
    }

    /// <summary>
    /// 委譲で状態処理を登録する（コルーチン対応）
    /// </summary>
    public void SetupState(
        TState state,
        Action onEnter = null,
        Action onExit = null,
        Action<float> onUpdate = null,
        Func<IEnumerator> enterRoutine = null,
        Func<IEnumerator> exitRoutine = null)
    {
        var mapping = _mappings[state];
        mapping.onEnter = onEnter;
        mapping.onExit = onExit;
        mapping.onUpdate = onUpdate;
        mapping.enterRoutine = enterRoutine;
        mapping.exitRoutine = exitRoutine;
    }

    /// <summary>
    /// トリガーを実行して状態遷移する
    /// </summary>
    public void ExecuteTrigger(TTrigger trigger)
    {
        //Debug.Log($"[FSM] ExecuteTrigger: {trigger}, current state = {_current}");
        if (_transitions.TryGetValue(_current, out var list))
        {
            foreach (var transition in list)
            {
                if (transition.Trigger.Equals(trigger))
                {
                    ChangeState(transition.To);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 状態遷移ルールを登録する
    /// </summary>
    public void AddTransition(TState from, TState to, TTrigger trigger)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new List<Transition<TState, TTrigger>>();
        }
        _transitions[from].Add(new Transition<TState, TTrigger> { To = to, Trigger = trigger });
    }

    /// <summary>
    /// 現在の状態を更新する（毎フレーム）
    /// </summary>
    public void Update(float deltaTime)
    {
        _currentMapping?.Update(deltaTime);
    }

    /// <summary>
    /// 状態を直接切り替える（Exit → Enter 順）
    /// </summary>
    private void ChangeState(TState to)
    {
        _currentMapping?.Exit(_mono);
        _current = to;
        _currentMapping = _mappings[to];
        _currentMapping.Enter(_mono);
    }

    /// <summary>
    /// 現在の状態を取得する
    /// </summary>
    public TState CurrentState => _current;
}




/*using System.Collections.Generic;
using System.Linq;
using System;

// 各State毎のdelagateを登録しておくクラス
public interface IState
{
    void OnEnter();
    void OnExit();
    void OnUpdate(float deltaTime);
}

public class StateMapping
{
    public IState stateClass;

    public Action onEnter;
    public Action onExit;
    public Action<float> onUpdate;

    public void Enter()
    {
        if (stateClass != null) stateClass.OnEnter();
        else onEnter?.Invoke();
    }

    public void Exit()
    {
        if (stateClass != null) stateClass.OnExit();
        else onExit?.Invoke();
    }

    public void Update(float deltaTime)
    {
        if (stateClass != null) stateClass.OnUpdate(deltaTime);
        else onUpdate?.Invoke(deltaTime);
    }
}

public class Transition<TState, TTrigger>
{
    public TState To { get; set; }
    public TTrigger Trigger { get; set; }
}

public class StateMachine<TState, TTrigger>
    where TState : struct, IConvertible, IComparable
    where TTrigger : struct, IConvertible, IComparable
{
    private TState _current;
    private StateMapping _currentMapping;

    private Dictionary<object, StateMapping> _mappings = new();
    private Dictionary<TState, List<Transition<TState, TTrigger>>> _transitions = new();

    public StateMachine(TState initialState)
    {
        // StateからStateMappingを作成
        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            _mappings[state] = new StateMapping();
        }
        ChangeState(initialState);
    }
    public void RegisterState(TState state, IState stateImpl)
    {
        _mappings[state].stateClass = stateImpl;
        
    }


    /// <summary>
    /// Stateを初期化する
    /// </summary>
    public void SetupState(TState state, Action onEnter, Action onExit, Action<float> onUpdate)
    {
        var mapping = _mappings[state];
        mapping.onEnter = onEnter;
        mapping.onExit = onExit;
        mapping.onUpdate = onUpdate;
    }

    /// <summary>
    /// トリガーを実行する
    /// </summary>
    public void ExecuteTrigger(TTrigger trigger)
    {
        if (_transitions.TryGetValue(_current, out var list))
        {
            foreach (var transition in list)
            {
                if (transition.Trigger.Equals(trigger))
                {
                    ChangeState(transition.To);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 遷移情報を登録する
    /// </summary>
    public void AddTransition(TState from, TState to, TTrigger trigger)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new List<Transition<TState, TTrigger>>();
        }
        _transitions[from].Add(new Transition<TState, TTrigger> { To = to, Trigger = trigger });
    }




    /// <summary>
    /// 更新する
    /// </summary>
    public void Update(float deltaTime)
    {
        _currentMapping?.Update(deltaTime);
    }


    /// <summary>
    /// Stateを直接変更する
    /// </summary>
    private void ChangeState(TState to)
    {
        _currentMapping?.Exit();
        _current = to;
        _currentMapping = _mappings[to];
        _currentMapping.Enter();
    }
}*/