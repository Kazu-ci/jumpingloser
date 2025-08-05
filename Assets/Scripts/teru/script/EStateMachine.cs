using System.Collections.Generic;

public class EStateMachine<TOwner>
{
    /// <summary>
    /// ステート基底クラス
    /// 各ステートクラスはこのクラスを継承する
    /// </summary>
    public abstract class StateBase
    {
        public EStateMachine<TOwner> StateMachine;
        protected TOwner Owner => StateMachine.Owner;

        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        public virtual void OnEnd() { }
    }
    private TOwner Owner { get; }
    private StateBase _currentState; // 現在のステート
    private StateBase _prevState;    // 前のステート
    private readonly Dictionary<int, StateBase> _states = new Dictionary<int, StateBase>(); // 全てのステート定義

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="owner">StateMachineを使用するOwner</param>
    public EStateMachine(TOwner owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// ステート定義登録
    /// ステートマシン初期化後にこのメソッドを呼ぶ
    /// </summary>
    /// <param name="stateId">ステートID</param>
    /// <typeparam name="T">ステート型</typeparam>
    public void Add<T>(int stateId) where T : StateBase, new()
    {
        if (_states.ContainsKey(stateId))
        {
            return;
        }
        // ステート定義を登録
        var newState = new T
        {
            StateMachine = this
        };
        _states.Add(stateId, newState);
    }

    /// <summary>
    /// ステート開始処理
    /// </summary>
    /// <param name="stateId">ステートID</param>
    public void OnStart(int stateId)
    {
        if (!_states.TryGetValue(stateId, out var nextState))
        {
            return;
        }
        // 現在のステートに設定して処理を開始
        _currentState = nextState;
        _currentState.OnStart();
    }

    /// <summary>
    /// ステート更新処理
    /// </summary>
    public void OnUpdate()
    {
        _currentState.OnUpdate();
    }

    /// <summary>
    /// 次のステートに切り替える
    /// </summary>
    /// <param name="stateId">切り替えるステートID</param>
    public void ChangeState(int stateId)
    {
        if (!_states.TryGetValue(stateId, out var nextState))
        {
            return;
        }
        // 前のステートを保持
        _prevState = _currentState;
        // ステートを切り替える
        _currentState.OnEnd();
        _currentState = nextState;
        _currentState.OnStart();
    }

    /// <summary>
    /// 前回のステートに切り替える
    /// </summary>
    public void ChangePrevState()
    {
        if (_prevState == null)
        {
            return;
        }
        // 前のステートと現在のステートを入れ替える
        (_prevState, _currentState) = (_currentState, _prevState);
    }
}