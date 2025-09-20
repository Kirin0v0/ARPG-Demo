using Animancer;
using Character.Ability;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Action
{
    public class PlayerActionHighClimbState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset highClimbTransition;
        [SerializeField, Range(0, 1f)] private float handsStayNormalizedTime = 0.2f;
        [SerializeField, Range(0, 1f)] private float handsExitNormalizedTime = 0.4f;
        [SerializeField] private float handsSupportForwardOffset = 0.1f;
        [SerializeField] private float leftHandSupportOffset = 0.1f;
        [SerializeField] private float leftHandSupportTopOffset = 0f;
        [SerializeField] private float rightHandSupportOffset = 0.1f;
        [SerializeField] private float rightHandSupportTopOffset = 0f;
        [SerializeField] private float bodyForwardOffset = 0.3f;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        private Vector3 _obstaclePeak;
        private Vector3 _leftHandTargetPosition;
        private Vector3 _rightHandTargetPosition;
        private float _bodyTopOffset;
        private Quaternion _obstacleDirection;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.PlayerParameters.obstacleActionIdea == PlayerObstacleActionIdea.HighClimb;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            var parameters = PlayerCharacter.PlayerParameters;

            // 动画播放结束就切换为默认状态
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(highClimbTransition, true);
            _animancerState.SharedEvents.OnEnd += OnClimbEnd;

            // 开启动作层IK
            PlayerCharacter.AnimationAbility.ApplyActionIK(true);
            // 获取障碍物数据
            _obstaclePeak = parameters.obstacleData.peak;
            _leftHandTargetPosition =
                parameters.obstacleData.peak +
                -parameters.obstacleData.collideNormal.normalized * handsSupportForwardOffset +
                Vector3.Cross(-parameters.obstacleData.collideNormal, Vector3.up).normalized *
                leftHandSupportOffset + leftHandSupportTopOffset * Vector3.up;
            _rightHandTargetPosition =
                parameters.obstacleData.peak +
                -parameters.obstacleData.collideNormal.normalized * handsSupportForwardOffset +
                Vector3.Cross(parameters.obstacleData.collideNormal, Vector3.up).normalized *
                rightHandSupportOffset + rightHandSupportTopOffset * Vector3.up;
            _bodyTopOffset = Mathf.Max((parameters.obstacleData.peak - PlayerCharacter.transform.position).y, 0f);
            _obstacleDirection = Quaternion.LookRotation(-parameters.obstacleData.collideNormal);
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            // 在双手支撑前旋转角色面向
            PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(PlayerCharacter.transform.rotation,
                _obstacleDirection,
                Mathf.Clamp01(_animancerState.NormalizedTime / handsStayNormalizedTime)));
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            // 移除结束回调
            _animancerState.SharedEvents.OnEnd -= OnClimbEnd;
            _animancerState = null;

            // 关闭动作层IK
            PlayerCharacter.AnimationAbility.ApplyActionIK(false);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.DrawSphere(_leftHandTargetPosition, 0.1f);
            Gizmos.DrawSphere(_rightHandTargetPosition, 0.1f);
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "爬上动作", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            var deltaPosition = animator.deltaPosition;

            // 在双手离开障碍物顶面前加工位移数据
            if (_animancerState.NormalizedTime <= handsExitNormalizedTime)
            {
                // 加工低处攀爬位移数据，剔除左右两侧以及y轴的动画位移，并设置障碍物对应的y轴位移
                var totalTime = _animancerState.Duration * handsExitNormalizedTime;
                deltaPosition = Vector3.ProjectOnPlane(animator.deltaPosition, PlayerCharacter.transform.right);
                deltaPosition = new Vector3(deltaPosition.x, _bodyTopOffset * (DeltaTime / totalTime), deltaPosition.z);
                // 向前位移仅在双手支撑后施加
                if (_animancerState.NormalizedTime > handsStayNormalizedTime)
                {
                    totalTime = _animancerState.Duration * (1 - handsStayNormalizedTime);
                    deltaPosition += transform.forward * bodyForwardOffset * (DeltaTime / totalTime);
                }
            }

            return (deltaPosition, animator.deltaRotation,
                _animancerState.NormalizedTime <= handsExitNormalizedTime);
        }

        public override void HandleAnimatorIK(Animator animator)
        {
            // 设置动画匹配目标，让动画左手匹配到障碍物顶面上
            if (_animancerState.NormalizedTime <= handsStayNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01(_animancerState.NormalizedTime / handsStayNormalizedTime));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,
                    Mathf.Clamp01(_animancerState.NormalizedTime / handsStayNormalizedTime));
                animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
            }
            else if (_animancerState.NormalizedTime >= handsExitNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01((1 - _animancerState.NormalizedTime) / (1 - handsExitNormalizedTime)));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,
                    Mathf.Clamp01((1 - _animancerState.NormalizedTime) / (1 - handsExitNormalizedTime)));
                animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
            }
        }

        private void OnClimbEnd()
        {
            Parent.SwitchToDefault();
        }
    }
}