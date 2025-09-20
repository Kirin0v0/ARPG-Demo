using Buff;
using Buff.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Remove/Buff")]
    public class SkillFlowFeatureRemoveBuffNode : SkillFlowFeatureNode
    {
        [Title("Buff配置")] public string buffId;
        [MaxValue(-1)] public int stack = -1;
        public BuffRemoveDurationType durationType = BuffRemoveDurationType.SetDuration;
        [MinValue(0)] public float duration = 0f;

        [Inject] private BuffManager _buffManager;

        private BuffManager BuffManager
        {
            get
            {
                if (!_buffManager)
                {
                    _buffManager = GameEnvironment.FindEnvironmentComponent<BuffManager>();
                }

                return _buffManager;
            }
        }

        public override string Title => "业务——删除Buff节点";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            if (BuffManager.TryGetBuffInfo(buffId, out var buffInfo))
            {
                BuffManager.RemoveBuff(new BuffRemoveInfo
                {
                    Info = buffInfo,
                    Caster = Caster,
                    Target = Target,
                    Stack = stack,
                    DurationType = durationType,
                    Duration = duration,
                    RuntimeParams = new(),
                });
            }
        }
    }
}