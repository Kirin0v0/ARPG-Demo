using System.Collections.Generic;
using System.Linq;
using Buff;
using Buff.Data;
using Camera;
using Character;
using Character.Ability;
using Character.Data;
using Common;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.Ability
{
    public class PlayerBattleAbility : CharacterBattleAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;

        public event System.Action<BattleInfo> OnPlayerEnterBattle;
        public event System.Action<BattleInfo, float> OnPlayerTryToEscapeBattle;
        public event System.Action<BattleInfo> OnPlayerEscapeBattle;
        public event System.Action<BattleInfo> OnPlayerDeadInBattle;
        public event System.Action<BattleInfo> OnPlayerFinishBattle;

        [Title("侦察配置")] [SerializeField] private float detectedRadius = 10f;

        [Title("逃跑配置")] [SerializeField] private float escapeBufferTime = 5f;

        [Title("偷袭配置")] [SerializeField] private List<string> sneakAttackBuffs = new();
        [SerializeField] private float sneakAttackBuffDuration = 10f;

        [Inject] private MainCameraController _mainCameraController;
        [Inject] private BuffManager _buffManager;
        [Inject] private BattleManager _battleManager;

        private bool _allowEscape;
        private float _recentInBattleFieldTime;

        public override void SneakAttack()
        {
            base.SneakAttack();
            if (sneakAttackBuffs.Count == 0)
            {
                return;
            }

            // 依次添加Buff
            sneakAttackBuffs.ForEach(buffId =>
            {
                if (_buffManager.TryGetBuffInfo(buffId, out var buffInfo))
                {
                    _buffManager.AddBuff(new BuffAddInfo
                    {
                        Info = buffInfo,
                        Caster = Owner,
                        Target = Owner,
                        Stack = buffInfo.maxStack,
                        Permanent = false,
                        DurationType = BuffAddDurationType.SetDuration,
                        Duration = sneakAttackBuffDuration,
                        RuntimeParams = new()
                        {
                            { BuffRuntimeParameters.ExistOnlyBattle, true }
                        },
                    });
                }
            });
        }

        protected override List<CharacterObject> GetDetectedEnemies()
        {
            if (!Owner)
            {
                return new List<CharacterObject>();
            }

            // 处于战斗时直接使用战斗敌人作为检测敌人
            if (Owner.Parameters.battleState == CharacterBattleState.Battle &&
                _battleManager.TryGetBattleInfo(Owner.Parameters.battleId, out var battleInfo))
            {
                var sideCharacters = battleInfo.GetSideCharacters(Owner);
                return sideCharacters.enemies.ToList();
            }

            // 为了避免一看到敌人就进战，我们可以规定，在一段距离外检测到敌人也不认为是发现敌人
            return _mainCameraController.PlayerVisibleEnemies.Where(enemy =>
                MathUtil.IsLessThanDistance(Owner.transform.position, enemy.transform.position, detectedRadius,
                    MathUtil.TwoDimensionAxisType.XZ)).ToList();
        }

        public override bool AllowEscapeBattle(BattleInfo battleInfo)
        {
            return base.AllowEscapeBattle(battleInfo) && _allowEscape;
        }

        protected override void OnJoinBattle(BattleInfo battleInfo)
        {
            _allowEscape = false;
            _recentInBattleFieldTime = Time.time;
            OnPlayerEnterBattle?.Invoke(battleInfo);
        }

        protected override void OnStayBattle(BattleInfo battleInfo)
        {
            if (battleInfo.InBattleField(Owner.transform.position))
            {
                _recentInBattleFieldTime = Time.time;
            }
            else
            {
                var escapeTime = escapeBufferTime - (Time.time - _recentInBattleFieldTime);
                OnPlayerTryToEscapeBattle?.Invoke(battleInfo, escapeTime);
            }

            if (Time.time - _recentInBattleFieldTime >= escapeBufferTime)
            {
                _allowEscape = true;
            }
        }

        protected override void OnEscapeBattle(BattleInfo battleInfo)
        {
            OnPlayerEscapeBattle?.Invoke(battleInfo);
            UpdateDetectedEnemiesImmediately();
        }

        protected override void OnDeadInBattle(BattleInfo battleInfo)
        {
            OnPlayerDeadInBattle?.Invoke(battleInfo);
            UpdateDetectedEnemiesImmediately();
        }

        protected override void OnFinishBattle(BattleInfo battleInfo)
        {
            OnPlayerFinishBattle?.Invoke(battleInfo);
            UpdateDetectedEnemiesImmediately();
        }
    }
}