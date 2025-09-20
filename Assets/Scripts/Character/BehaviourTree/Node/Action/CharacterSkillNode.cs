using System;
using Character.Ability;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Framework.Common.Timeline;
using Skill;
using Skill.Runtime;
using UnityEngine;
using VContainer;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Skill")]
    public class CharacterSkillNode : ActionNode
    {
        [SerializeField] private string skillId;

        private SkillReleaseInfo _skillReleaseInfo;

        public override string Description =>
            "角色技能节点，使用前请检查技能是否需要目标且先执行的节点是否在运行时数据共享目标角色\n此外，存在角色不满足技能释放条件返回失败的场景，成功释放技能则在技能完整结束后返回成功";

        protected override void OnStart(object payload)
        {
            _skillReleaseInfo = null;

            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.SkillManager ||
                !parameters.Character.SkillAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            // 查询角色是否拥有指定技能，如果未拥有则直接返回
            if (!parameters.Character.SkillAbility.TryGetSkill(skillId, out var skill))
            {
                DebugUtil.LogError(
                    $"The character({parameters.Character.Parameters.prototype}-{parameters.Character.Parameters.id}) does not have the skill({skillId})");
                return;
            }

            // 如果技能需要目标且未共享目标角色，则直接返回
            CharacterObject target = null;
            if (skill.flow.NeedTarget)
            {
                if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                        out var target1) || target1 is not CharacterObject)
                {
                    DebugUtil.LogError(
                        $"The target character is not found in the shared dictionary");
                    return;
                }

                target = (CharacterObject)target1;
            }

            parameters.Character.SkillAbility.ReleaseSkill(skillId, target, out _skillReleaseInfo);
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