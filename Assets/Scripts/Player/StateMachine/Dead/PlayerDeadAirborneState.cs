using Animancer;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Player.StateMachine.Dead;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Dead
{
    public class PlayerDeadAirborneState : PlayerDeadState, IPlayerStateLocomotion, IPlayerStateLocomotionParameter
    {
        [Title("状态属性")] [SerializeField] private bool forwardLocomotion;
        [SerializeField] protected StringAsset forwardSpeedParameter;
        [SerializeField] private bool lateralLocomotion;
        [SerializeField] protected StringAsset lateralSpeedParameter;

        [Title("动画")] [SerializeField] private StringAsset airborneDeadStartTransition;
        [SerializeField] private StringAsset airborneDeadLoopTransition;
        [SerializeField] private PlayerDeadPose animationDeadPose;

        [Title("状态切换")] [SerializeField] private string landDeadStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private Vector3 _horizontalPlaneSpeedBeforeFall;

        private AnimancerState _airborneDeadStartState;
        private AnimancerState _airborneDeadLoopState;

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

            // 播放开始死亡动画
            _airborneDeadStartState = PlayerCharacter.AnimationAbility.PlayAction(airborneDeadStartTransition);
            _airborneDeadStartState.SharedEvents.OnEnd = HandleDeadStartAnimationEnd;
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);
            
            if (HandleRespawn())
            {
                return;
            }

            HandleLand();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            if (_airborneDeadStartState != null)
            {
                PlayerCharacter.AnimationAbility.StopAction(_airborneDeadStartState);
                _airborneDeadStartState.SharedEvents.OnEnd = null;
                _airborneDeadStartState = null;
            }

            if (_airborneDeadLoopState != null)
            {
                PlayerCharacter.AnimationAbility.StopAction(_airborneDeadLoopState);
                _airborneDeadLoopState = null;
            }
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "空中死亡状态", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime, animator.deltaRotation,
                true);
        }

        public PlayerDeadPose DeadPose => animationDeadPose;

        private void HandleDeadStartAnimationEnd()
        {
            // 播放空中循环死亡动画
            _airborneDeadLoopState = PlayerCharacter.AnimationAbility.PlayAction(airborneDeadLoopTransition);
        }

        private bool HandleRespawn()
        {
            if (!PlayerCharacter.Parameters.dead)
            {
                Parent.SwitchToDefault();
                return true;
            }

            return false;
        }

        private void HandleLand()
        {
            // 判断当前角色是否处于空中，不是则切换落地死亡状态
            if (!PlayerCharacter.Parameters.Airborne)
            {
                Parent.SwitchState(landDeadStateName, true);
            }
        }
    }
}