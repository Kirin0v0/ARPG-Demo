using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Base
{
    [Flags]
    public enum PlayerStateRule
    {
        Land = 1,
        Airborne = 1 << 1,
        All = Land | Airborne,
    }

    public abstract class PlayerState : State, IPlayerState
    {
        public new PlayerRootStateMachine Parent => base.Parent as PlayerRootStateMachine;

        protected PlayerCharacterObject PlayerCharacter
        {
            get
            {
                if (Parent?.Blackboard is PlayerRootBlackboard blackboard)
                {
                    return blackboard.Owner;
                }

                return null;
            }
        }

        [Title("玩家通用属性")] [SerializeField] protected bool keepAirborneBaseAnimation = false;
        public bool KeepAirborneBaseAnimation => keepAirborneBaseAnimation;

        [SerializeField] protected PlayerStateRule allowEnterRule = PlayerStateRule.Land | PlayerStateRule.Airborne;

        [SerializeField] protected bool simulateGravity = true;

        [SerializeField] protected bool endureUntilExit = false;

        [SerializeField] protected bool immuneUntilExit = false;

        [Title("玩家状态过渡")] [InfoBox("展示状态名称，除攻击状态外一般不配置")] [SerializeField]
        protected string showStateName;

        [InfoBox("设置自状态过渡，用于其他状态展示过渡到该状态的过渡")] [SerializeField]
        protected List<PlayerStateTransition> toShowGotoTransitions;

        [SerializeField] [InfoBox("设置当前状态需要展示的其他过渡状态")]
        private List<string> toShowTransitionStates = new();

        [SerializeField] [InfoBox("设置当前状态需要展示的其他过渡状态")]
        private List<PlayerStateTransition> toShowSelfTransitions = new();

        private readonly HashSet<PlayerStateTransition> _showTransition = new();

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            // 过滤与规则不同的状态切换，防止切换到异常状态
            if ((allowEnterRule & PlayerStateRule.Airborne) != 0 && !PlayerCharacter.Parameters.Airborne
                || ((allowEnterRule & PlayerStateRule.Land) != 0 && PlayerCharacter.Parameters.Airborne))
            {
                if (allowEnterRule != PlayerStateRule.All)
                {
                    return false;
                }
            }

            // 过滤武器接口的无武器能力以及不支持在装备武器下的状态切换
            if (this is IPlayerStateWeapon stateWeapon)
            {
                if (!PlayerCharacter.WeaponAbility)
                {
                    return false;
                }

                if (stateWeapon.OnlyNoWeapon && PlayerCharacter.HumanoidParameters.WeaponUsed)
                {
                    return false;
                }

                if (stateWeapon.OnlyWeapon && !PlayerCharacter.HumanoidParameters.WeaponUsed)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            if (endureUntilExit)
            {
                PlayerCharacter.StateAbility?.StartEndure(GetType().Name, float.MaxValue);
            }

            if (immuneUntilExit)
            {
                PlayerCharacter.StateAbility?.StartImmune(GetType().Name, float.MaxValue);
            }
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            // 如果不模拟重力，就禁止竖直方向移动
            if (!simulateGravity)
            {
                PlayerCharacter.Parameters.verticalSpeed = 0f;
            }
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);

            // 根据可进入状态设置当前过渡提示（包括当前状态内部的过渡和当前状态转其他状态的过渡）
            _showTransition.Clear();
            _showTransition.AddRange(toShowSelfTransitions);
            toShowTransitionStates.ForEach(stateName =>
            {
                if (Parent.TryGetState(stateName, out var state))
                {
                    state.GetAllowEnterTransitions(this).ForEach(otherStateTransition =>
                    {
                        // 删除可替代的过渡并添加过渡
                        _showTransition.RemoveWhere(showTransition =>
                            otherStateTransition.replaceTransitionNames.Contains(showTransition.name));
                        _showTransition.Add(otherStateTransition);
                    });
                }
            });
        }

        protected virtual void OnDrawGizmosSelected()
        {
        }

        protected virtual void OnGUI()
        {
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            if (endureUntilExit)
            {
                PlayerCharacter.StateAbility?.StopEndure(GetType().Name);
            }

            if (immuneUntilExit)
            {
                PlayerCharacter.StateAbility?.StopImmune(GetType().Name);
            }

            // 根据接口来清空位移参数
            if (this is IPlayerStateLocomotionParameter locomotionParameter)
            {
                if (nextState is IPlayerStateLocomotion stateLocomotion)
                {
                    if (locomotionParameter.ForwardSpeedParameter && !stateLocomotion.ForwardLocomotion)
                    {
                        PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(
                            locomotionParameter.ForwardSpeedParameter, 0f);
                    }

                    if (locomotionParameter.LateralSpeedParameter && !stateLocomotion.LateralLocomotion)
                    {
                        PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(
                            locomotionParameter.LateralSpeedParameter, 0f);
                    }
                }
                else
                {
                    if (locomotionParameter.ForwardSpeedParameter)
                    {
                        PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(
                            locomotionParameter.ForwardSpeedParameter, 0f);
                    }

                    if (locomotionParameter.LateralSpeedParameter)
                    {
                        PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(
                            locomotionParameter.LateralSpeedParameter, 0f);
                    }
                }
            }
        }

        public virtual bool ControlRootMotionBySelf() => true;

        public virtual (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            return (animator.deltaPosition, animator.deltaRotation, true);
        }

        public virtual void HandleAnimatorIK(Animator animator)
        {
        }

        public void ShowStateName(string stateName)
        {
            showStateName = stateName;
        }

        public void ShowTransition(PlayerStateTransition transition)
        {
            _showTransition.Add(transition);
        }

        public string GetStateName() => showStateName;

        /// <summary>
        /// 当前状态的所有过渡列表
        /// </summary>
        /// <returns></returns>
        public List<PlayerStateTransition> GetStateTransitions() => _showTransition.ToList();

        /// <summary>
        /// 获取可进入的状态的过渡（注意不是当前状态的过渡，是当前状态转为其他可进入的状态的过渡）
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns></returns>
        protected virtual List<PlayerStateTransition> GetAllowEnterTransitions(IState currentState)
        {
            if (AllowEnter(currentState))
            {
                return toShowGotoTransitions;
            }

            return new List<PlayerStateTransition>();
        }
    }
}