using System;
using System.Collections.Generic;
using Animancer;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Humanoid.Weapon.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Player.StateMachine.Action
{
    public class PlayerActionUnequipState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset unequipTransition;
        [SerializeField] private StringAsset idleTransition;

        [Title("事件")] [SerializeField, EventNames]
        private StringAsset dropEvent;

        [SerializeField] private int leftHandEventValue = 0;
        [SerializeField] private int rightHandEventValue = 1;
        [SerializeField] private int twoHandsEventValue = 2;
        
        [Title("状态切换")]  [SerializeField] private string airborneStateName;

        [Title("空中参数")] [SerializeField] protected StringAsset forwardSpeedParameter;
        [SerializeField] protected StringAsset lateralSpeedParameter;
        [SerializeField] private float maxVerticalSpeedParameter = 2;
        [SerializeField] private float minVerticalSpeedParameter = -2;
        [SerializeField] private StringAsset verticalSpeedParameter;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;
        private List<(int, System.Action)> _callbackRegistry;

        private Vector3 _horizontalPlaneSpeedBeforeFall;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            if (!PlayerCharacter.WeaponAbility)
            {
                return false;
            }

            // 过滤武器未装备
            if (!PlayerCharacter.HumanoidParameters.WeaponUsed)
            {
                return false;
            }

            return true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 手臂层播放卸下动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayArm(unequipTransition, true);
            _callbackRegistry = _animancerState.Events(this).AddCallbacksWithResult<int>(dropEvent, OnWeaponDrop);
            _animancerState.Events(this).OnEnd += OnUnequipEnd;

            // 如果处于空中就执行以下逻辑
            if (PlayerCharacter.Parameters.Airborne)
            {
                // 计算下落水平位移速度
                var forwardsSpeed =
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter);
                var lateralSpeed =
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(lateralSpeedParameter);
                _horizontalPlaneSpeedBeforeFall =
                    PlayerCharacter.transform.TransformVector(new Vector3(lateralSpeed, 0, forwardsSpeed));
                DebugUtil.LogGreen(
                    $"从状态({previousState?.GetType().Name})转入下落，下落时水平位移速度: {_horizontalPlaneSpeedBeforeFall}");

                // 设置竖直速度的动画参数
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                    Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                        maxVerticalSpeedParameter));
            }
            else
            {
                _horizontalPlaneSpeedBeforeFall = Vector3.zero;
            }
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            // 空中卸下则需要与动画同步竖直速度
            if (PlayerCharacter.Parameters.Airborne)
            {
                // 设置竖直速度的动画参数
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                    Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                        maxVerticalSpeedParameter));
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            if (_animancerState != null)
            {
                // 删除事件回调
                _animancerState.Events(this).OnEnd -= OnUnequipEnd;
                _animancerState.Events(this).RemoveCallbacksWithResult(_callbackRegistry);
                // 隐藏手臂层
                PlayerCharacter.AnimationAbility.StopArm(_animancerState, false);
                _animancerState = null;
            }

            _callbackRegistry = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "卸下武器动作", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            if (PlayerCharacter.Parameters.Airborne)
            {
                return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime, animator.deltaRotation,
                    true);
            }

            return base.CalculateRootMotionDelta(animator);
        }

        private void OnWeaponDrop(int handType)
        {
            // 当动画卸下武器时，卸下对应类型的武器
            if (handType == leftHandEventValue)
            {
                // 卸下左手武器
                PlayerCharacter.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.LeftHand);
            }
            else if (handType == rightHandEventValue)
            {
                // 卸下右手武器
                PlayerCharacter.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.RightHand);
            }
            
            // 基础层播放闲置动画
            PlayerCharacter.AnimationAbility.SwitchBase(idleTransition);
        }

        private void OnUnequipEnd()
        {
            if (PlayerCharacter.Parameters.Airborne)
            {
                Parent.SwitchState(airborneStateName, true);
            }
            else
            {
                Parent.SwitchToDefault();
            }
        }
    }
}