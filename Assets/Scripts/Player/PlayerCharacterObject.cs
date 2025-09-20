using System;
using System.Collections.Generic;
using Character.Ability;
using Character.Data;
using Framework.Common.Debug;
using Humanoid;
using Player.Ability;
using Player.Brain;
using Player.SO;
using Player.StateMachine;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player
{
    public class PlayerCharacterObject : HumanoidCharacterObject
    {
        private const float SafePositionInvalidateTime = 0.5f;

        [Title("玩家必备组件")] [SerializeField] private PlayerRootStateMachine stateMachine;
        public PlayerRootStateMachine StateMachine => stateMachine;

        [Title("玩家角色配置")] [SerializeField] private PlayerCharacterCommonConfigSO playerCommonConfigSO;
        public PlayerCharacterCommonConfigSO PlayerCommonConfigSO => playerCommonConfigSO;

        [Title("玩家必备能力")] [SerializeField] private PlayerObstacleSensorAbility obstacleSensorAbility;
        public PlayerObstacleSensorAbility ObstacleSensorAbility => obstacleSensorAbility;

        [Title("玩家角色数据"), ShowInInspector, ReadOnly]
        public PlayerCharacterParameters PlayerParameters { get; } = new();

        public new PlayerCharacterBrain Brain => base.Brain as PlayerCharacterBrain;

        private readonly List<PlayerSafePositionRecord> _safePositionRecords = new();

        protected override void InitOptionalAbility()
        {
            base.InitOptionalAbility();
            obstacleSensorAbility?.Init(this);
        }

        public override void Init()
        {
            base.Init();
            // 在AI大脑初始化后再执行状态机初始化
            if (stateMachine)
            {
                stateMachine.blackboard.Owner = this;
            }
            stateMachine?.BuildStateMachine();
            stateMachine?.LaunchStateMachine();
        }

        public override void RenderUpdate(float deltaTime)
        {
            base.RenderUpdate(deltaTime);
            // 状态机组件最后更新渲染帧
            stateMachine?.RenderTick(deltaTime);
        }

        public override void LogicUpdate(float deltaTime)
        {
            // 玩家角色永远不会被破防
            StateAbility.StartUnbreakable(nameof(PlayerCharacterObject), float.MaxValue);
            // 障碍感知能力执行帧更新
            obstacleSensorAbility?.Tick();
            base.LogicUpdate(deltaTime);
            // 状态机组件最后更新逻辑帧
            stateMachine?.LogicTick(deltaTime);
        }

        public override void PostUpdate(float renderDeltaTime, float logicDeltaTime)
        {
            base.PostUpdate(renderDeltaTime, logicDeltaTime);

            // 如果处于安全位置就记录位置并更新时间，如果处于不安全位置就清空时间
            if (!Parameters.dead && !Parameters.Airborne)
            {
                _safePositionRecords.ForEach((record) => record.Time += renderDeltaTime);
                _safePositionRecords.Add(new PlayerSafePositionRecord
                {
                    Time = 0f,
                    Position = Parameters.position
                });
            }
            else
            {
                _safePositionRecords.Clear();
            }

            // 如果安全位置记录一定时间，就代表在到达该位置一定时间后没有问题，直接设置为安全位置
            var index = 0;
            while (index < _safePositionRecords.Count)
            {
                var record = _safePositionRecords[index];
                if (record.Time >= SafePositionInvalidateTime)
                {
                    PlayerParameters.lastSafePosition = record.Position;
                    _safePositionRecords.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        protected override void DestroyOptionalAbility()
        {
            base.DestroyOptionalAbility();
            obstacleSensorAbility?.Dispose();
        }

        public override void Destroy()
        {
            // 和创建顺序相反，先销毁状态机
            stateMachine?.DestroyStateMachine();
            if (stateMachine)
            {
                stateMachine.blackboard.Clear();
            }
            base.Destroy();
        }

        public void SetPlayerCharacterParameters(
            int hp,
            int mp
        )
        {
            // 这里重新设置玩家角色资源，仅在玩家创建时手动设置，其他角色在创建时默认是满资源状态
            ResourceAbility.Modify(new CharacterResource
            {
                hp = hp - Parameters.property.maxHp,
                mp = mp - Parameters.property.maxMp,
            });
            // 这里重新构建玩家角色交易
            if (TradeAbility is PlayerTradeAbility playerTradeAbility)
            {
                playerTradeAbility.RebuildTrade();
            }
        }

        private class PlayerSafePositionRecord
        {
            public float Time;
            public Vector3 Position;
        }
    }
}