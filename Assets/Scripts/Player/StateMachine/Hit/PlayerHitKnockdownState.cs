using Animancer;
using Character.Data;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Hit
{
    public class PlayerHitKnockdownState : PlayerHitState
    {
        [Title("动画")] [SerializeField] private StringAsset knockdownTransition;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;
        private float _duration;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 播放动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(knockdownTransition, true);
            // 记录原始动画速度并开始同步
            _duration = _animancerState.Duration;
            SynchronizeAnimationWithStun();
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);
            // 每个逻辑帧都同步动画和硬直
            SynchronizeAnimationWithStun();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
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
                GUI.Label(new Rect(0, 0, 100, 100), "击倒动作", guiStyle);
            }
        }

        /// <summary>
        /// 同步动画和硬直
        /// </summary>
        private void SynchronizeAnimationWithStun()
        {
            var animationProgress = 1 - PlayerCharacter.Parameters.resource.stun /
                PlayerCharacter.Parameters.property.stunMeter;
            var animationSpeed = _duration / (1f * PlayerCharacter.Parameters.property.stunMeter /
                                              PlayerCharacter.Parameters.property.stunReduceSpeed);
            _animancerState.Time = animationProgress;
            _animancerState.Speed = animationSpeed;
        }
    }
}