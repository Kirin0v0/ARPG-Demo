using Animancer;
using Framework.Common.StateMachine;
using Player.StateMachine.Attack;
using Player.StateMachine.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Action
{
    public class PlayerActionJumpState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset jumpTransition;
        [SerializeField] private float maxSpeed = 3.4f;
        [SerializeField] private StringAsset forwardSpeedParameter;
        [SerializeField] private StringAsset lateralSpeedParameter;

        [Title("事件")] [SerializeField, EventNames]
        private StringAsset offGroundEvent;

        [Title("状态切换")] [SerializeField] private string airborneStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            var parameters = PlayerCharacter.PlayerParameters;

            // 如果从攻击状态切换过来，则根据玩家输入方向重新设置动画参数
            if (previousState is PlayerAttackState)
            {
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(forwardSpeedParameter,
                    maxSpeed * parameters.playerInputCharacterMovementInFrame.z);
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(lateralSpeedParameter,
                    maxSpeed * parameters.playerInputCharacterMovementInFrame.x);
            }

            // 播放跳跃动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(jumpTransition, true);

            // 设置动画事件
            _animancerState.SharedEvents.AddCallback(offGroundEvent, HandleOffGroundEvent);
            _animancerState.SharedEvents.OnEnd = HandleJumpEndEvent;
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 删除动画事件
            _animancerState.SharedEvents.RemoveCallback(offGroundEvent, HandleOffGroundEvent);
            _animancerState.SharedEvents.OnEnd = null;

            // 隐藏动作层
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
                GUI.Label(new Rect(0, 0, 100, 100), "跳跃动作", guiStyle);
            }
        }

        private void HandleOffGroundEvent()
        {
            // 设置跳跃起步速度
            PlayerCharacter.Parameters.verticalSpeed = Mathf.Sqrt(
                2 * PlayerCharacter.PlayerCommonConfigSO.jumpHeight * GlobalRuleSingletonConfigSO.Instance.gravity);
            
            // 由于动画中角色会在离地后竖直移动，所以直接跳过后续动画
            if (Parent.SwitchState(airborneStateName, true))
            {
                return;
            }

            Parent.SwitchToDefault();
        }

        private void HandleJumpEndEvent()
        {
        }
    }
}