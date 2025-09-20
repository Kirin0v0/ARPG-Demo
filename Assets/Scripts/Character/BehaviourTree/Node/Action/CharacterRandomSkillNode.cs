using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Skill;
using Skill.Runtime;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Random Skill")]
    public class CharacterRandomSkillNode : ActionNode
    {
        private enum RandomSkillRange
        {
            All,
            Specified,
        }

        private enum RandomSkillTargetType
        {
            All,
            NoTarget,
            HasTarget,
        }

        [SerializeField] private RandomSkillRange range = RandomSkillRange.All;

        [ShowIf("@range == RandomSkillRange.Specified")] [SerializeField]
        private List<string> specifiedSkillIds = new();

        [ShowIf("@range == RandomSkillRange.All")] [SerializeField]
        private RandomSkillTargetType targetType = RandomSkillTargetType.All;

        private SkillReleaseInfo _skillReleaseInfo;

        public override string Description =>
            "角色随机技能节点，从角色技能列表中筛选当前满足释放场景且能够释放的技能列表并概率平均取一技能进行释放，成功释放技能后在技能完成后返回成功，不存在可释放的技能则立即返回失败";

        protected override void OnStart(object payload)
        {
            _skillReleaseInfo = null;

            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.SkillManager ||
                !parameters.Character.SkillAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            CharacterObject target = null;
            if (parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target1))
            {
                target = target1 as CharacterObject;
            }

            switch (range)
            {
                case RandomSkillRange.All:
                {
                    if (targetType == RandomSkillTargetType.HasTarget && !target)
                    {
                        DebugUtil.LogError(
                            $"The target character is not found in the shared dictionary");
                        return;
                    }

                    if (parameters.Character.SkillAbility.FindAllowReleaseSkills(target, out var skills))
                    {
                        RandomReleaseSkill(skills);
                    }
                }
                    break;
                case RandomSkillRange.Specified:
                {
                    if (parameters.Character.SkillAbility.FindAllowReleaseSkills(specifiedSkillIds, target,
                            out var skills))
                    {
                        RandomReleaseSkill(skills);
                    }
                }
                    break;
            }

            return;

            void RandomReleaseSkill(List<Skill.Runtime.Skill> skills)
            {
                if (skills.Count == 0) return;

                var skill = skills[Random.Range(0, skills.Count)];
                parameters.Character.SkillAbility.ReleaseSkill(skill.id, target, out _skillReleaseInfo);
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (_skillReleaseInfo == null)
            {
                return NodeState.Failure;
            }

            return _skillReleaseInfo.Finished ? NodeState.Success : NodeState.Running;
        }

        protected override void OnStop(object payload)
        {
            if (_skillReleaseInfo == null)
            {
                return;
            }

            _skillReleaseInfo.Stop();
            _skillReleaseInfo = null;
        }
    }
}