using System;
using System.Collections.Generic;
using Character;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Timeline.Data;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Caster")]
    public class SkillFlowFeatureCasterNode : SkillFlowFeatureNode
    {
        private const string OnExecutePort = "onExecute";

        public override string Title => "业务——施法者节点\n为需要角色列表的业务节点提供仅有技能施法者的角色列表";

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
            if (child is SkillFlowFeatureNode featureNode)
            {
                if (featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire ||
                    featureNode.GetPayloadsRequire() is SkillFlowFeatureCharactersPayloadsRequire)
                {
                    AddChildNodeInternal(key, child);
                    return true;
                }
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
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            GetChildNodes(OnExecutePort).ForEach(childNode =>
            {
                if (childNode is not SkillFlowFeatureNode featureNode)
                {
                    return;
                }

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
                            charactersPayloadsRequire.ProvideContext(new List<CharacterObject> { Caster }));
                    }
                        break;
                }
            });
        }
    }
}