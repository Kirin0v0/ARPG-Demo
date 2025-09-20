using Animancer;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Player.StateMachine.Dead;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Hit
{
    public class PlayerHitAirborneState : PlayerHitState, IPlayerStateLocomotion, IPlayerStateLocomotionParameter
    {
        [Title("状态属性")] [SerializeField] private bool forwardLocomotion;
        [SerializeField] protected StringAsset forwardSpeedParameter;
        [SerializeField] private bool lateralLocomotion;
        [SerializeField] protected StringAsset lateralSpeedParameter;
        
        [Title("动画")] [SerializeField] private StringAsset airborneHitTransition;
        [SerializeField] private float maxVerticalSpeedParameter = 2;
        [SerializeField] private float minVerticalSpeedParameter = -2;
        [SerializeField] private StringAsset verticalSpeedParameter;

        [Title("状态切换")] [SerializeField] private string landDeadStateName;
        [SerializeField] private string landKnockdownStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private Vector3 _horizontalPlaneSpeedBeforeFall;

        private AnimancerState _animancerState;

        public bool ForwardLocomotion => forwardLocomotion;
        public bool LateralLocomotion => lateralLocomotion;
        public StringAsset ForwardSpeedParameter => forwardSpeedParameter;
        public StringAsset LateralSpeedParameter => lateralSpeedParameter;
        
        public override bool AllowEnter(IState currentState)
        {
            // 不满足父类条件无法进入
            if (!base.AllowEnter(currentState))
            {
                return false;
            }
            
            // 如果处于空中，则要求竖直速度向下时检测离地高度，如果高度小于步高则禁止进入
            if (PlayerCharacter.Parameters.Airborne && PlayerCharacter.Parameters.verticalSpeed < 0)
            {
                if (Physics.Raycast(PlayerCharacter.Parameters.position, Vector3.down, PlayerCharacter.CharacterController.stepOffset, GlobalRuleSingletonConfigSO.Instance.groundLayer))
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

            // 播放动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(airborneHitTransition);
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                    maxVerticalSpeedParameter));
            
            // 空中受击的特殊规则：不可恢复硬直
            PlayerCharacter.ResourceAbility.SetReduceStunRate(0f);
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);
            HandleLand();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            // 退出时恢复正常速率
            PlayerCharacter.ResourceAbility.SetReduceStunRate(1f);
            // 停止动画
            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "空中受击状态", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime, animator.deltaRotation,
                true);
        }

        private void HandleLand()
        {
            // 判断当前角色是否处于空中，不是则根据是否死亡切换不同状态
            if (!PlayerCharacter.Parameters.Airborne)
            {
                if (PlayerCharacter.Parameters.dead)
                {
                    Parent.SwitchState(landDeadStateName, true);
                }
                else
                {
                    Parent.SwitchState(landKnockdownStateName, true);
                }
            }
        }
    }
}