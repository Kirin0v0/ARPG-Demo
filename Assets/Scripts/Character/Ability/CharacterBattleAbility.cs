using System;
using System.Collections.Generic;
using Character.Data;
using Common;
using Framework.Common.Audio;
using Framework.Core.Extension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Ability
{
    public abstract class CharacterBattleAbility : BaseCharacterOptionalAbility
    {
        private BattleInfo _battleInfo;

        public Dictionary<CharacterObject, float> CharacterToOthersDamage => _battleInfo == null
            ? new Dictionary<CharacterObject, float>()
            : _battleInfo.GetTargetToOthersDamageRecords(Owner);

        public Dictionary<CharacterObject, float> OthersToCharacterDamage => _battleInfo == null
            ? new Dictionary<CharacterObject, float>()
            : _battleInfo.GetOthersToTargetDamageRecords(Owner);

        [Title("运行时数据")]
        [ShowInInspector, ReadOnly]
        public CharacterObject[] DetectedEnemies { get; private set; } = Array.Empty<CharacterObject>();

        [ShowInInspector, ReadOnly]
        public CharacterObject[] BattleEnemies { get; private set; } = Array.Empty<CharacterObject>();

        [ShowInInspector, ReadOnly]
        public CharacterObject[] BattleAllies { get; private set; } = Array.Empty<CharacterObject>();

        /// <summary>
        /// 帧函数
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
            UpdateDetectedEnemiesImmediately();
            if (Owner.Parameters.battleState != CharacterBattleState.Battle)
            {
                SetBattleState(
                    DetectedEnemies.Length > 0
                        ? CharacterBattleState.Warning
                        : CharacterBattleState.Idle
                );
            }
        }

        protected void UpdateDetectedEnemiesImmediately()
        {
            DetectedEnemies = GetDetectedEnemies().ToArray();
        }

        protected abstract List<CharacterObject> GetDetectedEnemies();

        /// <summary>
        /// 偷袭攻击函数，该函数在角色偷袭对方或偷袭阵营的角色加入偷袭方的战斗时调用
        /// </summary>
        public virtual void SneakAttack()
        {
        }

        /// <summary>
        /// 被偷袭攻击函数，该函数在角色受到伤害或被偷袭阵营的角色加入被偷袭方的战斗时调用
        /// </summary>
        /// <param name=""></param>
        public virtual void BeSneakAttacked()
        {
        }

        /// <summary>
        /// 是否允许加入战斗，无论是主动还是被动加入战斗之前都会调用此函数
        /// </summary>
        /// <param name="battleInfo"></param>
        /// <returns>返回是否允许加入，false则代表不加入该战斗</returns>
        public virtual bool AllowJoinBattle(BattleInfo battleInfo) =>
            Owner.Parameters.battleState != CharacterBattleState.Battle && !Owner.Parameters.dead;

        /// <summary>
        /// 是否允许逃离战斗，逃离战斗之前都会调用此函数
        /// </summary>
        /// <param name="battleInfo"></param>
        /// <returns>返回是否允许逃离，false则代表不逃离该战斗</returns>
        public virtual bool AllowEscapeBattle(BattleInfo battleInfo) =>
            Owner.Parameters.battleState == CharacterBattleState.Battle && !Owner.Parameters.dead;

        /// <summary>
        /// 加入战斗函数
        /// </summary>
        /// <param name="battleInfo"></param>
        public void JoinBattle(BattleInfo battleInfo)
        {
            _battleInfo = battleInfo;
            SetBattleState(CharacterBattleState.Battle);
            Owner.Parameters.battleId = battleInfo.id;
            var sideCharacters = battleInfo.GetSideCharacters(Owner);
            BattleEnemies = sideCharacters.enemies.ToArray();
            BattleAllies = sideCharacters.allies.ToArray();
            OnJoinBattle(battleInfo);
        }

        protected abstract void OnJoinBattle(BattleInfo battleInfo);

        /// <summary>
        /// 处于战斗函数
        /// </summary>
        /// <param name="battleInfo"></param>
        public void StayBattle(BattleInfo battleInfo)
        {
            _battleInfo = battleInfo;
            var sideCharacters = battleInfo.GetSideCharacters(Owner);
            BattleEnemies = sideCharacters.enemies.ToArray();
            BattleAllies = sideCharacters.allies.ToArray();
            OnStayBattle(battleInfo);
        }

        protected abstract void OnStayBattle(BattleInfo battleInfo);

        /// <summary>
        /// 逃离战斗函数
        /// </summary>
        /// <param name="battleInfo"></param>
        public void EscapeBattle(BattleInfo battleInfo)
        {
            _battleInfo = null;
            SetBattleState(CharacterBattleState.Idle);
                Owner.Parameters.battleId = "";
            BattleEnemies = Array.Empty<CharacterObject>();
            BattleAllies = Array.Empty<CharacterObject>();
            OnEscapeBattle(battleInfo);
        }

        protected abstract void OnEscapeBattle(BattleInfo battleInfo);

        public void DeadInBattle(BattleInfo battleInfo)
        {
            _battleInfo = battleInfo;
            SetBattleState(CharacterBattleState.Idle);
            if (Owner)
            {
                Owner.Parameters.battleId = "";
            }
            BattleEnemies = Array.Empty<CharacterObject>();
            BattleAllies = Array.Empty<CharacterObject>();
            OnDeadInBattle(battleInfo);
        }

        protected abstract void OnDeadInBattle(BattleInfo battleInfo);

        /// <summary>
        /// 结束战斗函数
        /// </summary>
        /// <param name="battleInfo"></param>
        public void FinishBattle(BattleInfo battleInfo)
        {
            SetBattleState(CharacterBattleState.Idle);
            if (Owner)
            {
                Owner.Parameters.battleId = "";
            }
            BattleEnemies = Array.Empty<CharacterObject>();
            BattleAllies = Array.Empty<CharacterObject>();
            OnFinishBattle(battleInfo);
        }

        protected abstract void OnFinishBattle(BattleInfo battleInfo);
        
        

        private void SetBattleState(CharacterBattleState battleState)
        {
            if (Owner.IsGameObjectDestroyed())
            {
                return;
            }
            
            if (Owner.Parameters.battleState == battleState)
            {
                return;
            }

            var oldBattleState = Owner.Parameters.battleState;
            Owner.Parameters.battleState = battleState;
            Owner.StateChangeAbility?.OnBattleStateChanged(oldBattleState, battleState);
        }
    }
}