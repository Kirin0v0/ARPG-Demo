using Animancer;
using Character.Ability;
using Framework.Common.Audio;
using Framework.Common.StateMachine;
using Player.StateMachine.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Action
{
    public class PlayerActionVaultState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset vaultTransition;
        [SerializeField] private StringAsset vaultSprintTransition;
        [SerializeField] private StringAsset forwardSpeedParameter;
        [SerializeField] private float sprintSpeedThreshold = 3.4f;
        [SerializeField, Range(0, 1f)] private float leftHandStayNormalizedTime = 0.2f;
        [SerializeField, Range(0, 1f)] private float leftHandExitNormalizedTime = 0.5f;
        [SerializeField] private float leftHandSupportForwardOffset = 0.1f;
        [SerializeField] private float leftHandSupportLeftOffset = 0.1f;
        [SerializeField] private float leftHandSupportTopOffset = 0f;

        [Title("状态切换")] [SerializeField] private string sprintStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        private Vector3 _obstaclePeak;
        private Quaternion _obstacleDirection;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.PlayerParameters.obstacleActionIdea == PlayerObstacleActionIdea.Vault;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            var parameters = PlayerCharacter.PlayerParameters;

            // 根据向前速度切换动画
            if (PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter) >=
                sprintSpeedThreshold)
            {
                _animancerState = PlayerCharacter.AnimationAbility.SwitchBase(vaultSprintTransition);
            }
            else
            {
                _animancerState = PlayerCharacter.AnimationAbility.SwitchBase(vaultTransition);
            }

            // 动画播放结束就切换为默认状态
            _animancerState.SharedEvents.OnEnd += OnVaultEnd;

            // 开启动作层IK
            PlayerCharacter.AnimationAbility.ApplyBaseIK(true);
            // 获取障碍物数据
            _obstaclePeak = parameters.obstacleData.peak +
                            -parameters.obstacleData.collideNormal.normalized * leftHandSupportForwardOffset +
                            Vector3.Cross(-parameters.obstacleData.collideNormal, Vector3.up).normalized *
                            leftHandSupportLeftOffset + leftHandSupportTopOffset * Vector3.up;
            _obstacleDirection = Quaternion.LookRotation(-parameters.obstacleData.collideNormal);
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            // 在左手支撑前旋转角色面向
            PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(PlayerCharacter.transform.rotation,
                _obstacleDirection, Mathf.Clamp01(_animancerState.NormalizedTime / leftHandStayNormalizedTime)));
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 移除结束回调
            _animancerState.SharedEvents.OnEnd -= OnVaultEnd;
            _animancerState = null;

            // 关闭动作层IK
            PlayerCharacter.AnimationAbility.ApplyBaseIK(false);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.DrawSphere(_obstaclePeak, 0.1f);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "翻越动作", guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(
                Animator animator)
        {
            return (animator.deltaPosition, animator.deltaRotation,
                _animancerState.NormalizedTime <= leftHandStayNormalizedTime);
        }

        public override void HandleAnimatorIK(Animator animator)
        {
            // 设置动画匹配目标，让动画左手匹配到障碍物顶面上
            if (_animancerState.NormalizedTime <= leftHandStayNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01(_animancerState.NormalizedTime / leftHandStayNormalizedTime));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _obstaclePeak);
            }
            else if (_animancerState.NormalizedTime >= leftHandExitNormalizedTime)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                    Mathf.Clamp01((1 - _animancerState.NormalizedTime) / (1 - leftHandExitNormalizedTime)));
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _obstaclePeak);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, _obstaclePeak);
            }
        }

        private void OnVaultEnd()
        {
            if (PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter) >=
                sprintSpeedThreshold)
            {
                if (Parent.SwitchState(sprintStateName, true))
                {
                    return;
                }

                Parent.SwitchToDefault();
            }
            else
            {
                Parent.SwitchToDefault();
            }
        }
    }
}