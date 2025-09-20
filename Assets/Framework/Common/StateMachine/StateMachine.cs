using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Core.Attribute;
using UnityEngine;

namespace Framework.Common.StateMachine
{
    [Serializable]
    public abstract class StateMachine<TState> : State, IStateMachine
        where TState : MonoBehaviour, IState
    {
        [SerializeField, DisplayOnly] private TState defaultState;
        public TState DefaultState => defaultState;
        IState IStateMachine.DefaultState => defaultState;

        [SerializeField, DisplayOnly] private TState currentState;
        public TState CurrentState => currentState;
        IState IStateMachine.CurrentState => currentState;

        public bool Active => (!Root && Parent?.CurrentState == this) || Root;
        public bool Root => Parent == null;

        public abstract StateMachineBlackboard Blackboard { get; }

        private readonly Dictionary<string, TState> _states = new();
        private bool _build;

        /// <summary>
        /// 状态机构建函数，根状态机调用，子状态机依次递归执行
        /// </summary>
        public void BuildStateMachine()
        {
            if (_build)
            {
                return;
            }

            // 初始化父状态机以及子状态，必须要求提前设置好父子关系，最终仅有根状态机对象激活
            for (var i = 0; i < transform.childCount; i++)
            {
                var state = transform.GetChild(i).GetComponent<TState>();
                if (!state) continue;

                if (state.SetDefault && !defaultState)
                {
                    defaultState = state;
                }

                // 递归子状态机构建函数
                if (state is IStateMachine stateMachine)
                {
                    stateMachine.Parent = this;
                    stateMachine.BuildStateMachine();
                }
                else
                {
                    state.Parent = this;
                    state.Init();
                }

                // 将子状态对象失活
                state.gameObject.SetActive(false);
                _states.Add(state.Name,  state);
            }
            
            _build = true;
        }

        /// <summary>
        /// 根状态机启动函数，仅根状态机可以执行
        /// </summary>
        public void LaunchStateMachine()
        {
            if (!_build)
            {
                UnityEngine.Debug.LogError($"状态机{Name}未构建，无法启动");
            }

            if (!Root)
            {
                UnityEngine.Debug.LogError($"状态机{Name}不是根状态机，无法启动");
            }

            OnEnter(null);
        }

        /// <summary>
        /// 状态机销毁函数，根状态机调用，子状态机依次递归执行
        /// </summary>
        public void DestroyStateMachine()
        {
            // 根状态机分发失活函数
            if (Root)
            {
                OnExit(null);
            }

            // 分发销毁函数
            foreach (var state in _states.Values)
            {
                if (state is IStateMachine stateMachine)
                {
                    stateMachine.DestroyStateMachine();
                    stateMachine.Parent = null;
                }
                else
                {
                    state.Clear();
                    state.Parent = null;
                }
            }

            _states.Clear();
            defaultState = null;
        }
        
        /// <summary>
        /// 在自身状态机激活或无父状态机（即自身是根状态机）时切换当前层级的状态函数，注意这里不能切换非本层状态机的状态
        /// </summary>
        public bool SwitchState(TState state, bool allowSwitchToDefaultWhenError)
        {
            // 该状态不存在于当前状态机，判断是否切换到默认状态
            if (!IsChildState(state))
            {
                if (allowSwitchToDefaultWhenError && defaultState != null)
                {
                    UnityEngine.Debug.LogError(
                        $"状态机{Name}不存在状态({state.Name})，切换至默认状态{defaultState}");
                    return SwitchToDefault();
                }

                UnityEngine.Debug.LogError($"状态机{Name}不存在状态({state.Name})，无法切换状态");
                return false;
            }
            
            // 如果状态机失活就不能切换到下一状态，防止错误调用API
            if (!Active)
            {
                UnityEngine.Debug.LogWarning($"状态机{Name}失活，切换状态失败");
                return false;
            }

            // 如果被切换的状态不允许进入就不能切换到下一状态
            if (!state.AllowEnter(currentState))
            {
                UnityEngine.Debug.Log($"状态{state.Name}不允许进入，切换状态失败");
                return false;
            }

            var previousState = currentState;
            var nextState = state;

            if (previousState)
            {
                previousState.gameObject.SetActive(false);
                previousState.Exit(nextState);
            }

            currentState = nextState;
            nextState.gameObject.SetActive(true);
            nextState.Enter(previousState);

            return true;
        }

        public bool SwitchState(string stateName, bool allowSwitchToDefaultWhenError)
        {
            if (_states.TryGetValue(stateName, out var state))
            {
                return SwitchState(state, allowSwitchToDefaultWhenError);
            }

            return false;
        }

        bool IStateMachine.SwitchState(IState state, bool allowSwitchToDefaultWhenError) =>
            SwitchState(state as TState, allowSwitchToDefaultWhenError);

        /// <summary>
        /// 切换到默认状态
        /// </summary>
        public bool SwitchToDefault()
        {
            if (defaultState)
            {
                return SwitchState(defaultState, true);
            }

            return false;
        }

        /// <summary>
        /// 获取当前状态机的子状态
        /// </summary>
        public bool TryGetState(string stateName, out TState state)
        {
            return _states.TryGetValue(stateName, out state);
        }

        /// <summary>
        /// 状态机进入（激活）时，由根状态机分发下来
        /// </summary>
        protected virtual void OnEnter(TState previousState)
        {
            base.OnEnter(previousState);

            // 如果上一状态为其他状态机状态，我们就传递空状态，禁止非法接触不同层状态机状态
            TState state = null;
            if (previousState != null && IsChildState(previousState))
            {
                state = previousState;
            }
            
            // 如果状态机存在激活状态就传递函数给当前状态，否则激活默认状态传递函数
            if (currentState)
            {
                currentState.Enter(state);
            }
            else
            {
                SwitchToDefault();
            }
        }

        void IState.Enter(IState previousState) => OnEnter(previousState as TState);

        protected override void OnEnter(IState previousState)
        {
            OnEnter(previousState as TState);
        }

        /// <summary>
        /// 状态机每帧更新执行函数，由根状态机分发下来
        /// </summary>
        /// <param name="deltaTime"></param>
        protected override void OnRenderTick(float deltaTime)
        {
            if (currentState)
            {
                currentState.RenderTick(deltaTime);
            }
        }

        /// <summary>
        /// 状态机固定间隔帧执行函数，由根状态机分发下来
        /// </summary>
        /// <param name="fixedDeltaTime"></param>
        protected override void OnLogicTick(float fixedDeltaTime)
        {
            if (currentState)
            {
                currentState.LogicTick(fixedDeltaTime);
            }
        }

        /// <summary>
        /// 状态机退出（失活）时，由根状态机分发下来
        /// </summary>
        protected virtual void OnExit(TState nextState)
        {
            base.OnExit(nextState);

            // 如果下一状态为其他状态机状态，我们就传递空状态，禁止非法接触不同层状态机状态
            TState state = null;
            if (nextState != null && IsChildState(nextState))
            {
                state = nextState;
            }
            
            // 如果状态机存在激活状态就传递函数给当前状态
            if (currentState)
            {
                currentState.Exit(state);
                // 如果存在激活状态且下一状态为其他状态机状态，则说明当前状态机不是激活状态机，应将当前状态清空，防止下次重新进入状态机恢复之前的旧状态
                if (!state)
                {
                    currentState = null;
                }
            }
        }

        void IState.Exit(IState nextState) => OnExit(nextState as TState);

        protected override void OnExit(IState nextState)
        {
            OnExit(nextState as TState);
        }

        private bool IsChildState(TState state)
        {
            return _states.TryGetValue(state.Name, out var childState);
        }
    }
}