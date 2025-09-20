using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Player.StateMachine.Hit
{
    [Serializable]
    public class PlayerHitConfigData
    {
        [MinMaxSlider(-180f, 180f, true)] public Vector2 hitAngle;
        public StringAsset hitTransition;
    }

    public class PlayerHitGroundState : PlayerHitState
    {
        [Title("动画")] [SerializeField] private List<PlayerHitConfigData> hitConfigs;
        [SerializeField] private StringAsset defaultHitTransition;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;
        private float _duration;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 计算导致硬直的伤害方向并结合配置播放动画，如果没有伤害方向，则播放默认的动画
            if (PlayerCharacter.StateAbility.CausedIntoStunnedDamageInfo.HasValue)
            {
                var stunnedDamageInfo = PlayerCharacter.StateAbility.CausedIntoStunnedDamageInfo.Value;
                var sign = Vector3.Cross(PlayerCharacter.transform.forward, stunnedDamageInfo.Direction).y > 0
                    ? 1
                    : -1;
                var angle = sign * Vector3.Angle(PlayerCharacter.transform.forward, stunnedDamageInfo.Direction);
                var hitConfigDataList = hitConfigs.Where(hitConfig =>
                    hitConfig.hitAngle.x <= angle && hitConfig.hitAngle.y >= angle).ToList();
                // 从满足条件的配置中随机选择一个播放
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(
                    hitConfigDataList.Count > 0
                        ? hitConfigDataList[Random.Range(0, hitConfigDataList.Count)].hitTransition
                        : defaultHitTransition,
                    true
                );
            }
            else
            {
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(defaultHitTransition, true);
            }

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

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "受击动作", guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            // 停止动画
            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState = null;
        }

        /// <summary>
        /// 同步动画和硬直
        /// </summary>
        private void SynchronizeAnimationWithStun()
        {
            var animationProgress = PlayerCharacter.Parameters.property.stunMeter == 0
                ? 0f
                : 1 - PlayerCharacter.Parameters.resource.stun / PlayerCharacter.Parameters.property.stunMeter;
            var animationSpeed = PlayerCharacter.Parameters.property.stunReduceSpeed == 0
                ? 0f
                : _duration / (1f * PlayerCharacter.Parameters.property.stunMeter /
                               PlayerCharacter.Parameters.property.stunReduceSpeed);
            _animancerState.NormalizedTime = animationProgress;
            _animancerState.Speed = animationSpeed;
        }
    }
}