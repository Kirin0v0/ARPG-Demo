using Animancer;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Player.StateMachine.Hit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Dead
{
    public enum PlayerDeadType
    {
        Default,
        AfterFrontAttack,
        AfterBackAttack,
        AirborneLand,
        AirborneKnockdown
    }

    public class PlayerDeadGroundState : PlayerDeadState
    {
        [Title("动画")] [InfoBox("这里是默认死亡动画")] [SerializeField]
        private StringAsset defaultDeadTransition;

        [SerializeField] private PlayerDeadPose defaultDeadPose;

        [InfoBox("这里是正面攻击死亡动画")] [SerializeField]
        private StringAsset deadAfterFrontAttackTransition;

        [SerializeField] private PlayerDeadPose deadAfterFrontAttackPose;

        [InfoBox("这里是背后攻击死亡动画")] [SerializeField]
        private StringAsset deadAfterBackAttackTransition;

        [SerializeField] private PlayerDeadPose deadAfterBackAttackPose;

        [InfoBox("这里是空中落地死亡动画")] [SerializeField]
        private StringAsset deadAirborneLandTransition;

        [SerializeField] private PlayerDeadPose deadAirborneLandPose;

        [InfoBox("这里是空中击倒死亡动画")] [SerializeField]
        private StringAsset deadAirborneKnockdownTransition;

        [SerializeField] private PlayerDeadPose deadAirborneKnockdownPose;

        [Title("调试")] [SerializeField] private bool debug;

        private PlayerDeadType _deadType;
        private AnimancerState _animancerState;
        private PlayerDeadPose _deadPose;

        public override PlayerDeadPose DeadPose => _deadPose;

        public override bool AllowEnter(IState currentState)
        {
            // 只有当前状态不是死亡状态才允许进入
            return base.AllowEnter(currentState) && currentState is not PlayerDeadGroundState;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 如果上一状态是空中硬直状态，就播放空中击倒死亡动画
            if (previousState is PlayerHitAirborneState)
            {
                _deadType = PlayerDeadType.AirborneKnockdown;
                _animancerState =
                    PlayerCharacter.AnimationAbility.PlayAction(deadAirborneKnockdownTransition, true);
                _deadPose = deadAirborneKnockdownPose;
                return;
            }

            // 如果上一状态是空中死亡状态，就播放空中落地动画
            if (previousState is PlayerDeadAirborneState)
            {
                _deadType = PlayerDeadType.AirborneLand;
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(deadAirborneLandTransition, true);
                _deadPose = deadAirborneLandPose;
                return;
            }

            // 地面死亡动画计算角色死亡伤害方向
            if (PlayerCharacter.StateAbility.CausedDeadDamageInfo.HasValue)
            {
                var damageDirectionDot = Vector3.Dot(PlayerCharacter.transform.forward,
                    PlayerCharacter.StateAbility.CausedDeadDamageInfo.Value.Direction);
                if (damageDirectionDot <= 0f) // 如果伤害方向是从前向后，则代表正面攻击
                {
                    _deadType = PlayerDeadType.AfterFrontAttack;
                    _animancerState =
                        PlayerCharacter.AnimationAbility.PlayAction(deadAfterFrontAttackTransition, true);
                    _deadPose = deadAfterFrontAttackPose;
                }
                else // 如果伤害方向是从后向前，则代表背后攻击
                {
                    _deadType = PlayerDeadType.AfterBackAttack;
                    _animancerState =
                        PlayerCharacter.AnimationAbility.PlayAction(deadAfterBackAttackTransition, true);
                    _deadPose = deadAfterBackAttackPose;
                }
            }
            else
            {
                // 播放默认地面死亡动画
                _deadType = PlayerDeadType.Default;
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(defaultDeadTransition, true);
                _deadPose = defaultDeadPose;
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

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
                GUI.Label(
                    new Rect(0, 0, 100, 100),
                    _deadType switch
                    {
                        PlayerDeadType.Default => "默认死亡动作",
                        PlayerDeadType.AfterFrontAttack => "正面攻击死亡动作",
                        PlayerDeadType.AfterBackAttack => "背后攻击死亡动作",
                        PlayerDeadType.AirborneLand => "空中落地死亡动作",
                        PlayerDeadType.AirborneKnockdown => "空中击倒死亡动作",
                    },
                    guiStyle
                );
            }
        }
    }
}