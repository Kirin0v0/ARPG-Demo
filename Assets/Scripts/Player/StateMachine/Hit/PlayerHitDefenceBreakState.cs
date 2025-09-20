using Animancer;
using Common;
using Framework.Common.Audio;
using Framework.Common.StateMachine;
using Humanoid.Weapon.Data;
using Player.StateMachine.Defence;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Hit
{
    public class PlayerHitDefenceBreakState : PlayerHitState
    {
        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;
        private float _duration;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            if (!PlayerCharacter.WeaponAbility || PlayerCharacter.WeaponAbility.DefensiveWeaponSlot == null ||
                PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data == null)
            {
                return false;
            }

            return currentState is PlayerDefenceState;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;
            
            // 检测当前动画过渡库是否是防御武器的动画过渡库，不是就去设置
            PlayerCharacter.AnimationAbility.SwitchTransitionLibrary(
                weaponData.Defence.defenceAbility.transitionLibrary);
            // 播放动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(
                weaponData.Defence.defenceAbility.breakParameter.transition, true);
            // 记录原始动画速度并开始同步
            _duration = _animancerState.Duration;
            SynchronizeAnimationWithStun();
            
            // 设置硬直恢复速度
            PlayerCharacter.ResourceAbility.SetReduceStunRate(weaponData.DefenceBreakResumeSpeed);
            
            // 播放防御打破音效
            if (weaponData.Defence.defenceAbility.breakParameter.playAudio)
            {
                PlayerCharacter.AudioAbility?.PlaySound(
                    weaponData.Defence.defenceAbility.breakParameter.audioClipRandomizer.Random(), false,
                    weaponData.Defence.defenceAbility.breakParameter.audioVolume);
            }
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
            // 退出防御相关状态时重新设置姿势，本质上是重新设置动画过渡库
            PlayerCharacter.SetPose(PlayerCharacter.HumanoidParameters.pose);
            // 停止动画播放
            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState = null;
            
            // 恢复硬直恢复速度
            PlayerCharacter.ResourceAbility.SetReduceStunRate(1f);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "防御打破动作", guiStyle);
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