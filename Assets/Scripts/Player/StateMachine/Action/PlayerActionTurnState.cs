using Animancer;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Action
{
    public class PlayerActionTurnState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset turnTransition;
        [SerializeField] private StringAsset turnTypeParameter;
        [SerializeField] private int turn90LeftValue = 0;
        [SerializeField] private int turn90RightValue = 1;
        [SerializeField] private int turn180Value = 2;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            var brainParameters = PlayerCharacter.PlayerParameters;

            // 判断当前玩家是否输入移动
            if (brainParameters.playerInputMovementInFrame.magnitude != 0)
            {
                // 计算偏差角度，规定顺时针为正值，逆时针为负值
                var inputCross = Vector3.Cross(PlayerCharacter.transform.forward,
                    brainParameters.playerInputMovementInFrame);
                var playerMovementInputOffsetAngle = inputCross.y > 0
                    ? Vector3.Angle(PlayerCharacter.transform.forward,
                        brainParameters.playerInputMovementInFrame)
                    : -Vector3.Angle(PlayerCharacter.transform.forward,
                        brainParameters.playerInputMovementInFrame);

                // 转身逻辑：优先判断能否满足180转身，再判断能否满足90转身
                // 设计效果：角色除了前方扇形小范围外都能转身，允许存在转身误差
                if (Mathf.Abs(playerMovementInputOffsetAngle) >=
                    PlayerCharacter.PlayerCommonConfigSO.turn180StartAngle)
                {
                    _animancerState = PlayerCharacter.AnimationAbility.PlayAction(turnTransition, true);
                    _animancerState.SharedEvents.OnEnd ??= HandleTurnEnd;
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(turnTypeParameter, turn180Value);
                }
                else if (Mathf.Abs(playerMovementInputOffsetAngle) >=
                         PlayerCharacter.PlayerCommonConfigSO.turn90StartAngle)
                {
                    _animancerState = PlayerCharacter.AnimationAbility.PlayAction(turnTransition, true);
                    _animancerState.SharedEvents.OnEnd ??= HandleTurnEnd;
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(turnTypeParameter,
                        Mathf.Sign(playerMovementInputOffsetAngle) > 0 ? turn90RightValue : turn90LeftValue);
                }
                else
                {
                    Parent.SwitchToDefault();
                }
            }
            else
            {
                // 没输入就回到默认状态
                Parent.SwitchToDefault();
            }
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "转身动作", guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            if (_animancerState != null)
            {
                PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                _animancerState.SharedEvents.OnEnd = null;
                _animancerState = null;
            }
        }

        private void HandleTurnEnd()
        {
            Parent.SwitchToDefault();
        }
    }
}