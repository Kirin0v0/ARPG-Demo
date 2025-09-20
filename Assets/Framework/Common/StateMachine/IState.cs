using JetBrains.Annotations;

namespace Framework.Common.StateMachine
{
    /// <summary>
    /// 分层有限状态机状态接口
    /// </summary>
    public interface IState
    {
        IStateMachine Parent { set; get; }
        bool SetDefault { get; }
        string Name { get; }

        void Init();
        bool AllowEnter([CanBeNull] IState currentState);
        void Enter([CanBeNull] IState previousState);
        void RenderTick(float deltaTime);
        void LogicTick(float fixedDeltaTime);
        void Exit([CanBeNull] IState nextState);
        void Clear();
    }
}