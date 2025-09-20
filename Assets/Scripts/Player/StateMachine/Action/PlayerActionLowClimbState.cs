using Animancer;
using Character.Ability;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Action
{
    public class PlayerActionLowClimbState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset lowClimbTransition;
        [SerializeField, Range(0, 1f)] private float leftHandStayNormalizedTime = 0.2f;
        [SerializeField, Range(0, 1f)] private float leftHandExitNormalizedTime = 0.4f;
        [SerializeField] private float leftHandSupportForwardOffset = 0.1f;
        [SerializeField] private float leftHandSupportLeftOffset = 0.1f;
        [SerializeField] private float leftHandSupportTopOffset = 0f;
        [SerializeField] private float bodyForwardOffset = 0.3f;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        private Vector3 _obstaclePeak;
        private Vector3 _leftHandTargetPosition;
        private float _bodyTopOffset;
        private Quaternion _obstacleDirection;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.PlayerParameters.obstacleActionIdea == PlayerObstacleActionIdea.LowClimb;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            var parameters = PlayerCharacter.PlayerParameters;

            // 动画播放结束就切换为默认状态
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(lowClimbTransition, true);
            _animancerState.SharedEvents.OnEnd += OnClimbEnd;

            // 开启动作层IK
            PlayerCharacter.AnimationAbility.ApplyActionIK(true);
            // 获取障碍物数据
            _obstaclePeak = parameters.obstacleData.peak;
            _leftHandTargetPosition = parameters.obstacleData.peak +
                                      -parameters.obstacleData.collideNormal.normalized * leftHandSupportForwardOffset +
                                      Vector3.Cross(-parameters.obstacleData.collideNormal, Vector3.up).normalized *
                                      leftHandSupportLeftOffset + leftHandSupportTopOffset * Vector3.up;
            _bodyTopOffset = Mathf.Max((parameters.obstacleData.peak - PlayerCharacter.transform.position).y, 0f);
            _obstacleDirection = Quaternion.LookRotation(-parameters.obstacleData.collideNormal);
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            // 在左手支撑前旋转角色面向
            PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(PlayerCharacter.transform.rotation,
                _obstacleDirection,
                Mathf.Clamp01(_animancerState.NormalizedTime / leftHandStayNormalizedTime)));
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
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "翻上动作", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            var deltaPosition = animator.deltaPosition;

            // 在左手离开障碍物顶面前加工位移数据
            if (_animancerState.NormalizedTime <= leftHandExitNormalizedTime)
            {
                // 加工低处攀爬位移数据，剔除左右两侧以及y轴的动画位移，并设置障碍物对应的y轴位移
                var totalTime = _animancerState.Duration * leftHandExitNormalizedTime;
                deltaPosition = Vector3.ProjectOnPlane(animator.deltaPosition, PlayerCharacter.transform.right);
                deltaPosition = new Vector3(deltaPosition.x, _bodyTopOffset * (DeltaTime / totalTime), deltaPosition.z);
            }

            return (deltaPosition, animator.deltaRotation,
                _animancerState.NormalizedTime <= leftHandStayNormalizedTime);
        }

        public override void HandleAnimatorIK(Animator animator)
        {
            // 设置动画匹配目标，让动画左手匹配到障碍物顶面上
            if (_animancerState.NormalizedTime <= leftHandStayNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01(_animancerState.NormalizedTime / leftHandStayNormalizedTime));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
            }
            else if (_animancerState.NormalizedTime >= leftHandExitNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01((1 - _animancerState.NormalizedTime) / (1 - leftHandExitNormalizedTime)));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
            }
        }

        private void OnClimbEnd()
        {
            Parent.SwitchToDefault();
        }
    }
}