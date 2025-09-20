using System;
using System.Linq;
using Common;
using Damage;
using Damage.Data;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Humanoid.Weapon.Data;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Defence
{
    public abstract class PlayerDefenceState : PlayerState, IPlayerStateWeapon, IPlayerStateDefence
    {
        [Title("状态属性")] [SerializeField] private PlayerDefenceStateType stateType = PlayerDefenceStateType.Action;

        [Title("通用状态切换")] [SerializeField] private string airborneStateName;
        [SerializeField] private string equipStateName;
        [SerializeField] private string jumpStateName;
        [SerializeField] private string evadeStateName;

        [Inject] protected DamageManager DamageManager;

        private float _stateChangeCountdown = 0f;
        protected bool AllowStateChanged => _stateChangeCountdown <= 0f;

        public PlayerDefenceStateType StateType => stateType;
        public virtual float AllowChangeStateTime => 0f;
        public abstract bool InitialState { get; }
        public abstract bool DamageResistant { get; }

        public bool OnlyWeapon => !InitialState;
        public bool OnlyNoWeapon => false;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            // 过滤配置无防御武器或无配置防御动画过渡库
            if (PlayerCharacter.WeaponAbility.DefensiveWeaponSlot == null ||
                PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data == null ||
                !PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data.Defence.defenceAbility.transitionLibrary)
            {
                return false;
            }

            return true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            // 如果没装备武器就直接切换到武器装备状态去装备武器
            if (!PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Equipped)
            {
                if (Parent.SwitchState(equipStateName, true))
                {
                    return;
                }

                Parent.SwitchToDefault();
                return;
            }

            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;

            // 检测当前动画过渡库是否是防御武器的动画过渡库，不是就去设置
            PlayerCharacter.AnimationAbility?.SwitchTransitionLibrary(PlayerCharacter.WeaponAbility.DefensiveWeaponSlot
                .Data.Defence.defenceAbility.transitionLibrary);

            // 如果允许减伤才会设置伤害系数
            if (DamageResistant)
            {
                PlayerCharacter.Parameters.inDefence = true;
                PlayerCharacter.Parameters.damageMultiplier = weaponData.DefenceDamageMultiplier;
            }

            // 进入防御后即监听伤害处理事件，用于触发防御音效
            DamageManager.AfterDamageHandled += HandleDamage;

            // 如果上一状态是防御状态，就设置倒计时来避免频繁切换防御/非防御状态
            if (previousState is IPlayerStateDefence defenceState)
            {
                _stateChangeCountdown = defenceState.AllowChangeStateTime;
            }
            else
            {
                _stateChangeCountdown = 0f;
            }

            EnterAfterCheck();
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            // 防御状态内部切换条件：1.必须是移动状态 2.必须允许状态切换
            _stateChangeCountdown -= deltaTime;
            if (stateType == PlayerDefenceStateType.Locomotion && AllowStateChanged)
            {
                if (HandleEvade())
                {
                    return;
                }

                if (HandleJump())
                {
                    return;
                }

                if (HandleCancelDefend())
                {
                    return;
                }

                if (HandleAirborne())
                {
                    return;
                }
            }

            UpdateAfterCheck();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 取消监听伤害处理事件
            DamageManager.AfterDamageHandled -= HandleDamage;

            // 退出防御时取消减免受到的伤害
            PlayerCharacter.Parameters.damageMultiplier = 1f;
            PlayerCharacter.Parameters.inDefence = false;

            // 退出防御相关状态时重新设置姿势，本质上是重新设置动画过渡库
            PlayerCharacter.SetPose(PlayerCharacter.HumanoidParameters.pose);
        }

        protected abstract void EnterAfterCheck();

        protected abstract void UpdateAfterCheck();

        private bool HandleAirborne()
        {
            if (PlayerCharacter.Parameters.Airborne)
            {
                return Parent.SwitchState(airborneStateName, true);
            }

            return false;
        }

        private bool HandleEvade()
        {
            if (PlayerCharacter.PlayerParameters.isEvadeInFrame)
            {
                return Parent.SwitchState(evadeStateName, true);
            }

            return false;
        }

        private bool HandleJump()
        {
            if (PlayerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame)
            {
                return Parent.SwitchState(jumpStateName, true);
            }

            return false;
        }

        private bool HandleCancelDefend()
        {
            if (!PlayerCharacter.PlayerParameters.isDefendingInFrame)
            {
                return Parent.SwitchToDefault();
            }

            return false;
        }

        private void HandleDamage(DamageInfo damageInfo)
        {
            // 如果伤害的承受方是自身才执行后续逻辑
            if (damageInfo.Target != PlayerCharacter) return;

            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;
            // 判断伤害触发标识符
            if ((damageInfo.TriggerFlags & DamageInfo.PerfectDefenceFlag) != 0) // 触发完美防御
            {
                DebugUtil.LogOrange("触发完美防御");
                // 播放完美防御音效
                PlayerCharacter.AudioAbility?.PlaySound(
                    weaponData.Defence.defenceAbility.perfectAudioClipRandomizer.Random(),
                    false,
                    weaponData.Defence.defenceAbility.perfectAudioVolume
                );
            }
            else if ((damageInfo.TriggerFlags & DamageInfo.DefenceFlag) != 0) // 触发普通防御
            {
                DebugUtil.LogOrange("触发普通防御");
                // 播放普通防御音效
                PlayerCharacter.AudioAbility?.PlaySound(
                    weaponData.Defence.defenceAbility.universalAudioClipRandomizer.Random(),
                    false,
                    weaponData.Defence.defenceAbility.universalAudioVolume
                );
            }
        }
    }
}