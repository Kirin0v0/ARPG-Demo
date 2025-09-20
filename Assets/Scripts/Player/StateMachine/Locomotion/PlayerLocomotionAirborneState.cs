using Animancer;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.Brain;
using Player.StateMachine.Action;
using Player.StateMachine.Attack;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Locomotion
{
    public class PlayerLocomotionAirborneState : PlayerLocomotionState
    {
        [Title("动画")] [SerializeField] private StringAsset idleTransition;
        [SerializeField] private StringAsset airborneTransition;
        [SerializeField] private float maxVerticalSpeedParameter = 2;
        [SerializeField] private float minVerticalSpeedParameter = -2;
        [SerializeField] private StringAsset verticalSpeedParameter;

        [Title("状态切换")] [SerializeField] private string landStateName;
        [SerializeField] private string hangStateName;
        [SerializeField] private string equipStateName;
        [SerializeField] private string unequipStateName;
        [SerializeField] private string attackStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private Vector3 _horizontalPlaneSpeedBeforeFall;

        public override bool AllowEnter(IState currentState)
        {
            // 跳跃以及装备相关状态允许直接进入，无视是否处于空中
            if (currentState is PlayerActionJumpState || currentState is PlayerActionEquipState || currentState is PlayerActionUnequipState)
            {
                return true;
            }

            // 不满足父类条件无法进入
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            // 如果处于空中，则要求竖直速度向下时检测离地高度，如果高度小于步高则禁止进入，避免在凹凸不平的地面频繁切换到该状态
            if (PlayerCharacter.Parameters.Airborne && PlayerCharacter.Parameters.verticalSpeed < 0)
            {
                if (Physics.Raycast(PlayerCharacter.Parameters.position, Vector3.down,
                        PlayerCharacter.CharacterController.stepOffset,
                        GlobalRuleSingletonConfigSO.Instance.groundLayer))
                {
                    return false;
                }
            }

            return true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 计算下落水平位移速度
            var forwardsSpeed = PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter);
            var lateralSpeed = PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(lateralSpeedParameter);
            _horizontalPlaneSpeedBeforeFall =
                PlayerCharacter.transform.TransformVector(new Vector3(lateralSpeed, 0, forwardsSpeed));
            DebugUtil.LogGreen(
                $"从状态({previousState?.GetType().Name})转入状态({this.GetType().Name})，水平位移速度: {_horizontalPlaneSpeedBeforeFall}");

            // 设置动画参数
            PlayerCharacter.AnimationAbility.SwitchBase(airborneTransition);
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                    maxVerticalSpeedParameter));
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            // 更新竖直速度
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                    maxVerticalSpeedParameter));

            if (HandleEquipOrUnequip())
            {
                return;
            }

            if (HandleAttack())
            {
                return;
            }
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);

            if (HandleHang())
            {
                return;
            }

            HandleLand();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "空中状态", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime, animator.deltaRotation,
                true);
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            
            // 如果下一个状态不允许保持空中基础动画，则切换回空闲动画 
            if (nextState is PlayerState { KeepAirborneBaseAnimation: false })
            {
                PlayerCharacter.AnimationAbility.SwitchBase(idleTransition);
            }
        }

        /// <summary>
        /// 处理装备/卸下行为
        /// </summary>
        /// <returns></returns>
        protected bool HandleEquipOrUnequip()
        {
            if (PlayerCharacter.PlayerParameters.isEquipOrUnequipInFrame)
            {
                if (PlayerCharacter.HumanoidParameters.WeaponUsed)
                {
                    return Parent.SwitchState(unequipStateName, true);
                }
                else
                {
                    return Parent.SwitchState(equipStateName, true);
                }
            }

            return false;
        }

        private bool HandleHang()
        {
            if (PlayerCharacter.PlayerParameters.obstacleActionIdea == PlayerObstacleActionIdea.Hang)
            {
                return Parent.SwitchState(hangStateName, true);
            }

            return false;
        }

        private bool HandleAttack()
        {
            if (PlayerCharacter.PlayerParameters.isAttackInFrame ||
                PlayerCharacter.PlayerParameters.isHeavyAttackInFrame)
            {
                if (Parent.SwitchState(attackStateName, true))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleLand()
        {
            // 过滤处于空中状态
            if (PlayerCharacter.Parameters.Airborne) return;

            // 检测当前是否允许切换成落地状态，不允许就直接切换成默认状态
            if (!PlayerCharacter.GravityAbility)
            {
                Parent.SwitchToDefault();
            }
            else
            {
                if (PlayerCharacter.GravityAbility.AirborneDropHeight <= PlayerCharacter.CharacterController.stepOffset)
                {
                    Parent.SwitchToDefault();
                }
                else
                {
                    Parent.SwitchState(landStateName, true);
                }
            }
        }
    }
}