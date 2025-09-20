using Character.Data;
using Common;
using Damage.Data;
using Events;
using Framework.Common.Debug;
using Player;
using UnityEngine;

namespace Character.Ability
{
    public class CharacterStateAbility: BaseCharacterNecessaryAbility
    {
        public DamageInfo? CausedDeadDamageInfo; // 导致死亡的伤害信息
        public DamageInfo? CausedRespawnedDamageInfo; // 导致复活的伤害信息
        public DamageInfo? CausedIntoStunnedDamageInfo; // 导致进入硬直的伤害信息
        public DamageInfo? CausedExitStunnedDamageInfo; // 导致退出硬直的伤害信息
        public DamageInfo? CausedIntoBrokenDamageInfo; // 导致进入破防的伤害信息
        public DamageInfo? CausedExitBrokenDamageInfo; // 导致退出破防的伤害信息

        public event System.Action<DamageInfo?> OnCharacterKilled;
        public event System.Action<DamageInfo?> OnCharacterRespawned;
        public event System.Action<DamageInfo?> OnCharacterIntoStunned;
        public event System.Action<DamageInfo?> OnCharacterExitStunned;
        public event System.Action<DamageInfo?> OnCharacterIntoBroken;
        public event System.Action<DamageInfo?> OnCharacterExitBroken;

        private readonly CharacterState _immune = new();
        private readonly CharacterState _endure = new();
        private readonly CharacterState _unbreakable = new();

        private bool _destroyAfterDead = false;
        private float _destroyDelay = 0f;
        private float _destroyCountdown = 0f;
        private bool _startDestroyCountdown = false;

        public bool ShouldDestroy() => Owner.Parameters.dead && _destroyAfterDead && _startDestroyCountdown &&
                                     _destroyCountdown <= 0f;

        protected override void OnInit()
        {
            base.OnInit();
            _destroyCountdown = 0f;
            _startDestroyCountdown = false;
        }

        public void SetDestroyParameters(bool destroyAfterDead, float destroyDelay)
        {
            _destroyAfterDead = destroyAfterDead;
            _destroyDelay = destroyDelay;
        }

        /// <summary>
        /// 检查角色状态，每帧都要调用
        /// </summary>
        /// <param name="deltaTime"></param>
        public void CheckState(float deltaTime)
        {
            // 角色无敌状态倒计时并赋值
            _immune.Countdown(deltaTime);
            Owner.Parameters.immune = _immune.IsActive();
            // 角色霸体状态倒计时并赋值
            _endure.Countdown(deltaTime);
            Owner.Parameters.endure = _endure.IsActive();
            // 角色不可破防状态倒计时并赋值
            _unbreakable.Countdown(deltaTime);
            Owner.Parameters.unbreakable = _unbreakable.IsActive();
            // 每帧都检查是否死亡并销毁
            CheckWhetherDestroyIfDead(deltaTime);
        }

        public void StartImmune(string command, float time)
        {
            _immune.Active(command, time);
            Owner.Parameters.immune = _immune.IsActive();
        }

        public void StopImmune(string command)
        {
            _immune.Inactive(command);
            Owner.Parameters.immune = _immune.IsActive();
        }

        public void StartEndure(string command, float time)
        {
            _endure.Active(command, time);
            Owner.Parameters.endure = _endure.IsActive();
        }

        public void StopEndure(string command)
        {
            _endure.Inactive(command);
            Owner.Parameters.endure = _endure.IsActive();
        }

        public void StartUnbreakable(string command, float time)
        {
            _unbreakable.Active(command, time);
            Owner.Parameters.unbreakable = _unbreakable.IsActive();
        }

        public void StopUnbreakable(string command)
        {
            _unbreakable.Inactive(command);
            Owner.Parameters.unbreakable = _unbreakable.IsActive();
        }

        public void BeKilled(DamageInfo? damageInfo)
        {
            if (Owner.Parameters.dead || Owner.Parameters.resource.hp > 0)
            {
                return;
            }

            Owner.Parameters.dead = true;
            CausedDeadDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnBeKilled(damageInfo);
            OnCharacterKilled?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.KillCharacter, Owner);
            CheckWhetherDestroyIfDead();
        }

        public void BeRespawned(DamageInfo? damageInfo)
        {
            if (!Owner.Parameters.dead || Owner.Parameters.resource.hp <= 0)
            {
                return;
            }

            Owner.Parameters.dead = false;
            CausedRespawnedDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnBeRespawned(damageInfo);
            OnCharacterRespawned?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.RespawnCharacter, Owner);
        }

        public void IntoStunned(DamageInfo? damageInfo)
        {
            if (Owner.Parameters.stunned ||
                Owner.Parameters.resource.stun < Owner.Parameters.property.stunMeter)
            {
                return;
            }

            Owner.Parameters.stunned = true;
            CausedIntoStunnedDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnIntoStunned(damageInfo);
            OnCharacterIntoStunned?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.CauseCharacterIntoStunned,
                Owner);
        }

        public void ExitStunned(DamageInfo? damageInfo)
        {
            if (!Owner.Parameters.stunned ||
                Owner.Parameters.resource.stun >= Owner.Parameters.property.stunMeter)
            {
                return;
            }

            Owner.Parameters.stunned = false;
            CausedExitStunnedDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnExitStunned(damageInfo);
            OnCharacterExitStunned?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.CauseCharacterExitStunned,
                Owner);
        }

        public void IntoBroken(DamageInfo? damageInfo)
        {
            if (Owner.Parameters.broken ||
                Owner.Parameters.resource.@break < Owner.Parameters.property.breakMeter)
            {
                return;
            }

            Owner.Parameters.broken = true;
            CausedIntoBrokenDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnIntoBroken(damageInfo);
            OnCharacterIntoBroken?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.CauseCharacterIntoBroken,
                Owner);
        }

        public void ExitBroken(DamageInfo? damageInfo)
        {
            if (!Owner.Parameters.broken ||
                Owner.Parameters.resource.@break >= Owner.Parameters.property.breakMeter)
            {
                return;
            }

            Owner.Parameters.broken = false;
            CausedExitBrokenDamageInfo = damageInfo;
            Owner.StateChangeAbility?.OnExitBroken(damageInfo);
            OnCharacterExitBroken?.Invoke(damageInfo);
            GameApplication.Instance.EventCenter.TriggerEvent<CharacterObject>(GameEvents.CauseCharacterExitBroken,
                Owner);
        }

        private void CheckWhetherDestroyIfDead(float deltaTime = 0f)
        {
            if (_destroyAfterDead && Owner.Parameters.dead)
            {
                // 此时未开始倒计时，则赋值开始计时，否则就更新倒计时
                if (!_startDestroyCountdown)
                {
                    _destroyCountdown = _destroyDelay;
                    _startDestroyCountdown = true;
                }
                else
                {
                    _destroyCountdown -= deltaTime;
                }
            }
            else
            {
                _destroyCountdown = 0f;
                _startDestroyCountdown = false;
            }
        }
    }
}