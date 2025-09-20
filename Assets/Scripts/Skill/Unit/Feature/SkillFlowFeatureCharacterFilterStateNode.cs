using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Character Filter/State")]
    public class SkillFlowFeatureCharacterFilterStateNode : SkillFlowFeatureNode
    {
        private const string OnExecutePort = "onExecute";

        [Title("死亡状态")] [SerializeField] private bool ignoreDead = true;

        [HideIf("ignoreDead")] [SerializeField]
        private bool dead = true;

        [Title("硬直状态")] [SerializeField] private bool ignoreStunned = true;

        [HideIf("ignoreStunned")] [SerializeField]
        private bool stunned = true;

        [Title("破防状态")] [SerializeField] private bool ignoreBroken = true;

        [HideIf("ignoreBroken")] [SerializeField]
        private bool broken = true;

        [Title("空中状态")] [SerializeField] private bool ignoreAirborne = true;

        [HideIf("ignoreAirborne")] [SerializeField]
        private bool airborne = true;

        public override string Title => "业务——角色过滤状态节点\n提供过滤后的角色列表";

#if UNITY_EDITOR
        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = OnExecutePort,
                    title = "执行时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            if (child is SkillFlowFeatureNode featureNode &&
                (featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire ||
                 featureNode.GetPayloadsRequire() is SkillFlowFeatureCharactersPayloadsRequire))
            {
                AddChildNodeInternal(key, child);
                return true;
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            if (child is SkillFlowFeatureNode featureNode)
            {
                return RemoveChildNodeInternal(key, child);
            }

            return false;
        }
#endif

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.CharactersPayloads;
        }

        /// <summary>
        /// 过滤角色节点执行
        /// </summary>
        /// <param name="timelineInfo"></param>
        /// <param name="payloads">这里规定0：目标角色列表</param>
        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            // 防错误传参
            List<CharacterObject> targets;
            try
            {
                targets = payloads[0] as List<CharacterObject>;
            }
            catch (Exception e)
            {
                DebugUtil.LogWarning($"{GetType().Name} payloads is wrong");
                return;
            }

            // 过滤非指定状态的角色，并执行子节点逻辑
            var filterCharacters = targets!.Where(target =>
            {
                if (!ignoreDead && target.Parameters.dead != dead)
                {
                    return false;
                }

                if (!ignoreStunned && target.Parameters.stunned != stunned)
                {
                    return false;
                }

                if (!ignoreBroken && target.Parameters.broken != broken)
                {
                    return false;
                }

                if (!ignoreAirborne && target.Parameters.Airborne != airborne)
                {
                    return false;
                }

                return true;
            }).ToList();
            GetChildNodes(OnExecutePort).ForEach(childNode =>
            {
                if (childNode is SkillFlowFeatureNode featureNode)
                {
                    switch (featureNode.GetPayloadsRequire())
                    {
                        case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                        {
                            featureNode.Execute(timelineInfo, nonPayloadsRequire.ProvideContext());
                        }
                            break;
                        case SkillFlowFeatureCharactersPayloadsRequire charactersPayloadsRequire:
                        {
                            featureNode.Execute(timelineInfo, charactersPayloadsRequire.ProvideContext(filterCharacters));
                        }
                            break;
                    }
                }
            });
        }
    }
}