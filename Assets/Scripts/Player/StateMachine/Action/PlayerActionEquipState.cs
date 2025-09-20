using System;
using System.Collections.Generic;
using Animancer;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Humanoid;
using Humanoid.SO;
using Humanoid.Weapon.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Player.StateMachine.Action
{
    public class PlayerActionEquipState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset equipTransition;
        [SerializeField] private StringAsset idleTransition;

        [Title("事件")] [SerializeField, EventNames]
        private StringAsset holdEvent;

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

            // 过滤配置武器后的姿势是默认姿势
            var finalPose = PlayerCharacter.WeaponAbility.CalculatePoseIfWeaponsAreEquipped();
            if (finalPose == HumanoidCharacterPose.NoWeapon)
            {
                return false;
            }

            // 过滤未配置武器姿势动画过渡库
            if (!HumanoidCharacterSingletonConfigSO.Instance.poseTransitionLibraryConfigurations.TryGetValue(finalPose,
                    out var transitionLibrary))
            {
                DebugUtil.LogWarning($"The transition library asset of the pose({finalPose}) is not existed");
                return false;
            }

            return true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 根据计算后的姿势设置动画过渡库
            var finalPose = PlayerCharacter.WeaponAbility.CalculatePoseIfWeaponsAreEquipped();
            if (!HumanoidCharacterSingletonConfigSO.Instance.poseTransitionLibraryConfigurations.TryGetValue(finalPose,
                    out var transitionLibrary))
            {
                throw new Exception($"The transition library asset of the pose({finalPose}) is not existed");
            }

            PlayerCharacter.AnimationAbility!.SwitchTransitionLibrary(transitionLibrary);

            // 基础层播放闲置动画
            PlayerCharacter.AnimationAbility.SwitchBase(idleTransition);

            // 手臂层播放装备动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayArm(equipTransition, true);
            _callbackRegistry = _animancerState.SharedEvents.AddCallbacksWithResult<int>(holdEvent, OnWeaponHold);
            _animancerState.SharedEvents.OnEnd += OnEquipEnd;

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

            // 空中装备则需要与动画同步竖直速度
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
                _animancerState.SharedEvents.OnEnd -= OnEquipEnd;
                _animancerState.SharedEvents.RemoveCallbacksWithResult(_callbackRegistry);
                // 隐藏手臂层
                PlayerCharacter.AnimationAbility.StopArm(_animancerState, false);
            }

            _animancerState = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "装备武器动作", guiStyle);
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

        private void OnWeaponHold(int handType)
        {
            // 当动画握住武器时，装备对应类型的武器
            if (handType == leftHandEventValue)
            {
                // 装备左手武器
                PlayerCharacter.WeaponAbility.EquipByEquippedPosition(HumanoidWeaponEquippedPosition.LeftHand);
            }
            else if (handType == rightHandEventValue)
            {
                // 装备右手武器
                PlayerCharacter.WeaponAbility.EquipByEquippedPosition(HumanoidWeaponEquippedPosition.RightHand);
            }
        }

        private void OnEquipEnd()
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