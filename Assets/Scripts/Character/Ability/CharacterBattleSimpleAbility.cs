using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Buff;
using Buff.Data;
using Character.Collider;
using Common;
using Damage;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterBattleSimpleAbility : CharacterBattleAbility
    {
        private const float DetectInterval = 0.1f;

        [Title("侦察配置")] [SerializeField, MinValue(0.5f)]
        private float detectInitialCooldown = 2f;

        [SerializeField, MinValue(0.5f)] private float detectResetCooldown = 5f;

        [SerializeField, MinValue(0f)] private float detectRadius = 20f;

        [SerializeField, MinValue(0f), MaxValue(360f)] [InfoBox("侦察角度，特指为正面扇形角度")]
        private float detectAngle = 60f;

        [SerializeField] [InfoBox("侦察是否忽略障碍物，即是否透视")]
        private bool detectIgnoreObstacle = false;

        [Title("被偷袭配置")] [SerializeField] private bool beSneakAttackedDebuffEnable = true;

        [ShowIf("beSneakAttackedDebuffEnable")] [SerializeField]
        private List<string> beSneakAttackedBuffs = new();

        [ShowIf("beSneakAttackedDebuffEnable")] [SerializeField, MinValue(0f)]
        private float beSneakAttackedBuffDuration = 10f;

        [Inject] private GameManager _gameManager;
        [Inject] private BuffManager _buffManager;
        [Inject] private DamageManager _damageManager;
        [Inject] private BattleManager _battleManager;

        private readonly List<CharacterObject> _detectedEnemy = new();

        private float _detectCooldown;

        protected override void OnInit()
        {
            base.OnInit();
            _detectCooldown = detectInitialCooldown;
        }

        public override void Tick(float deltaTime)
        {
            // 处于战斗时直接使用战斗敌人作为检测敌人
            if (Owner.Parameters.battleState == CharacterBattleState.Battle)
            {
                _detectedEnemy.Clear();
                if (_battleManager.TryGetBattleInfo(Owner.Parameters.battleId, out var battleInfo))
                {
                    var sideCharacters = battleInfo.GetSideCharacters(Owner);
                    _detectedEnemy.AddRange(sideCharacters.enemies);
                }
            }
            else
            {
                // 如果处于侦察cd，就不会主动去侦察敌人，保证玩家脱战后能够有一段时间不进战
                if (_detectCooldown <= 0f)
                {
                    DetectEnemy();
                    // 侦察完敌人后间隔一小段时间再次侦察
                    _detectCooldown = DetectInterval;
                }

                _detectCooldown = Mathf.Clamp(_detectCooldown - deltaTime, 0f, _detectCooldown);
            }

            // 最后才执行父类帧函数
            base.Tick(deltaTime);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _detectedEnemy.Clear();
            UpdateDetectedEnemiesImmediately();
        }

        public override void BeSneakAttacked()
        {
            base.BeSneakAttacked();
            if (!beSneakAttackedDebuffEnable)
            {
                return;
            }

            // 依次添加Buff
            beSneakAttackedBuffs.ForEach(buffId =>
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
                        Duration = beSneakAttackedBuffDuration,
                        RuntimeParams = new()
                        {
                            { BuffRuntimeParameters.ExistOnlyBattle, true }
                        },
                    });
                }
            });
        }

        protected override List<CharacterObject> GetDetectedEnemies() => _detectedEnemy;

        /// <summary>
        /// 不允许主动逃离战斗
        /// </summary>
        /// <param name="battleInfo"></param>
        /// <returns></returns>
        public override bool AllowEscapeBattle(BattleInfo battleInfo) => false;

        protected override void OnJoinBattle(BattleInfo battleInfo)
        {
        }

        protected override void OnStayBattle(BattleInfo battleInfo)
        {
        }

        protected override void OnEscapeBattle(BattleInfo battleInfo)
        {
            ExitBattle();
            // 如果角色逃离了战斗就恢复所有资源
            Owner.ResourceAbility.FillResource(false);
        }

        protected override void OnDeadInBattle(BattleInfo battleInfo)
        {
            ExitBattle();
        }

        protected override void OnFinishBattle(BattleInfo battleInfo)
        {
            ExitBattle();
            // 如果角色结束了战斗就恢复所有资源
            Owner?.ResourceAbility.FillResource(false);
        }

        private void DetectEnemy()
        {
            // 遍历场景的全部角色，获取侦察到的敌人
            var detectedEnemies = _gameManager.Characters.Where(target =>
            {
                if (target.Parameters.side == CharacterSide.Neutral ||
                    target.Parameters.side == Owner.Parameters.side)
                {
                    return false;
                }

                if (target.Parameters.dead)
                {
                    return false;
                }

                if (MathUtil.IsMoreThanDistance(Owner.Parameters.position, target.Parameters.position, detectRadius,
                        MathUtil.TwoDimensionAxisType.XZ))
                {
                    return false;
                }

                var angle = Vector3.Angle(
                    transform.forward,
                    new Vector3(target.transform.position.x - transform.position.x, 0,
                        target.transform.position.z - transform.position.z)
                );
                if (angle > detectAngle / 2f)
                {
                    return false;
                }

                // 如果忽视障碍物，这里就可以认为是检测到敌人了，直接返回true
                if (detectIgnoreObstacle)
                {
                    return true;
                }

                // 后续是射线检测是否被障碍物遮挡
                var raycastHits = Physics.RaycastAll(
                    Owner.Visual.Eye.transform.position,
                    target.Visual.Eye.transform.position - Owner.Visual.Eye.transform.position,
                    detectRadius,
                    GlobalRuleSingletonConfigSO.Instance.characterModelLayer |
                    GlobalRuleSingletonConfigSO.Instance.maskLayer
                );
                if (raycastHits.Length == 0)
                {
                    return false;
                }

                Array.Sort(raycastHits, (a, b) => a.distance.CompareTo(b.distance));
                // 这里规定如果在碰撞到目标角色前碰撞到障碍物就认为目标角色不可见
                foreach (var raycastHit in raycastHits)
                {
                    // 成功碰撞到目标角色，认为角色可见
                    if (raycastHit.transform.gameObject == target.gameObject ||
                        (raycastHit.transform.TryGetComponent<CharacterCollider>(out var OwnerCollider) &&
                         OwnerCollider.Owner == target))
                    {
                        return true;
                    }

                    // 碰撞到障碍物，认为角色不可见
                    if (((1 << raycastHit.transform.gameObject.layer) &
                         GlobalRuleSingletonConfigSO.Instance.maskLayer) != 0)
                    {
                        return false;
                    }
                }

                return false;
            }).ToList();

            // 每次检测后重置列表
            _detectedEnemy.Clear();
            _detectedEnemy.AddRange(detectedEnemies);
        }

        private void ExitBattle()
        {
            // 清空侦察敌人
            _detectedEnemy.Clear();
            UpdateDetectedEnemiesImmediately();
            // 进入侦察cd
            _detectCooldown = detectResetCooldown;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Owner)
            {
                return;
            }
            
            DebugUtil.LogGrey($"角色({Owner.Parameters.DebugName})位置: {Owner.Parameters.position}");
            Gizmos.color = Color.magenta;
            var sectorMesh = MeshUtil.Generate2DSectorMesh(
                0,
                detectRadius,
                0f,
                detectAngle,
                Owner.transform.forward,
                2f
            );
            var matrix = new Matrix4x4();
            matrix.SetTRS(
                Owner.Parameters.position,
                Quaternion.identity,
                Vector3.one
            );
            Gizmos.matrix = matrix;
            Gizmos.DrawMesh(sectorMesh);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawSphere(Owner.Parameters.position, 1f);
        }
    }
}