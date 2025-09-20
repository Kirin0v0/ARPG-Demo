using System;
using System.Collections.Generic;
using Character.Ability;
using Character.BehaviourTree;
using Common;
using Damage;
using Framework.Common.Audio;
using Framework.Common.BehaviourTree;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using Map;
using Skill;
using UnityEngine;
using VContainer;

namespace Character.Brain
{
    [RequireComponent(typeof(BehaviourTreeExecutor))]
    public abstract class CharacterBehaviourTreeBrain : CharacterBrain
    {
        [SerializeField] protected BehaviourTreeExecutor executor;

        [Inject] protected DamageManager DamageManager;
        [Inject] protected AlgorithmManager AlgorithmManager;
        [Inject] protected GameManager GameManager;
        [Inject] protected SkillManager SkillManager;
        [Inject] protected MapManager MapManager;

        protected readonly Dictionary<string, object> Shared = new Dictionary<string, object>();

        protected abstract void SetBlackboardBeforeExecuteTick(Blackboard blackboard, float deltaTime);

        protected abstract void GetBlackboardAfterExecuteTick(Blackboard blackboard);

        protected override void OnBrainInit()
        {
            executor.Init();
        }

        protected override void OnRenderThoughtsUpdated(float deltaTime)
        {
        }

        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
            ExecuteBehaviourTreeTick(fixedDeltaTime);
        }

        protected override void OnBrainDestroy()
        {
            executor.Destroy();
        }

        private void OnValidate()
        {
            if (!executor)
            {
                executor = GetComponent<BehaviourTreeExecutor>();
            }
        }

        private void ExecuteBehaviourTreeTick(float deltaTime)
        {
            // 执行前设置行为树黑板数据
            executor.UseBlackboard(blackboard => SetBlackboardBeforeExecuteTick(blackboard, deltaTime));
            // 获取行为树状态
            var treeState = executor.Tick(deltaTime, new CharacterBehaviourTreeParameters
            {
                Shared = Shared,
                Character = Owner,
                DamageManager = DamageManager,
                AlgorithmManager = AlgorithmManager,
                GameManager = GameManager,
                SkillManager = SkillManager,
                MapManager = MapManager,
            });
            // 执行后从行为树黑板获取数据
            executor.UseBlackboard(GetBlackboardAfterExecuteTick);
            // 如果已经结束，就清空共享数据
            if (treeState != TreeState.Running)
            {
                Shared.Clear();
            }
        }
    }
}