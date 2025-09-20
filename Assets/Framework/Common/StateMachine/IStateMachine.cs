namespace Framework.Common.StateMachine
{
    /// <summary>
    /// 分层有限状态机接口
    /// </summary>
    public interface IStateMachine : IState
    {
        // 接口属性
        IState DefaultState { get; }
        IState CurrentState { get; }
        bool Active { get; }
        StateMachineBlackboard Blackboard { get; }

        // 接口方法
        void BuildStateMachine();
        void LaunchStateMachine();
        void DestroyStateMachine();
        bool SwitchState(IState state, bool allowSwitchToDefaultWhenError);
        bool SwitchToDefault();
    }
}