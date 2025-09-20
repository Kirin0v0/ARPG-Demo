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
    [NodeMenuItem("Feature/Character Filter/Side")]
    public class SkillFlowFeatureCharacterFilterSideNode : SkillFlowFeatureNode
    {
        private const string OnExecutePort = "onExecute";

        [InfoBox("保留的角色阵营，非保留阵营角色将被过滤")] [SerializeField]
        private bool remainingCasterAlly;

        [SerializeField] private bool remainingCasterEnemy;

        public override string Title => "业务——角色过滤阵营节点\n提供过滤后的角色列表";

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

            // 过滤非保留阵营的角色，并执行子节点逻辑
            var filterCharacters = targets!.Where(target =>
            {
                if (remainingCasterAlly && target.Parameters.side == Caster.Parameters.side)
                {
                    return true;
                }

                if (remainingCasterEnemy && target.Parameters.side != Caster.Parameters.side)
                {
                    return true;
                }

                return false;
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
                            featureNode.Execute(timelineInfo,
                                charactersPayloadsRequire.ProvideContext(filterCharacters));
                        }
                            break;
                    }
                }
            });
        }
    }
}