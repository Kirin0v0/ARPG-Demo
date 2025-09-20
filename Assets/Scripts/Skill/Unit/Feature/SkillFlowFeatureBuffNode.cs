using System;
using System.Collections.Generic;
using Buff;
using Buff.Data;
using Character;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Buff")]
    public class SkillFlowFeatureBuffNode : SkillFlowFeatureNode
    {
        [Title("Buff配置")] public string buffId;
        [MinValue(1)] public int stack = 1;
        public bool permanent = false;
        public BuffAddDurationType durationType = BuffAddDurationType.SetDuration;
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

        public override string Title => "业务——Buff节点\n必须有角色列表的输入";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.CharactersPayloads;
        }

        /// <summary>
        /// Buff节点执行
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

            if (BuffManager.TryGetBuffInfo(buffId, out var buffInfo))
            {
                targets!.ForEach(target =>
                {
                    BuffManager.AddBuff(new BuffAddInfo
                    {
                        Info = buffInfo,
                        Caster = Caster,
                        Target = target,
                        Stack = stack,
                        Permanent = permanent,
                        DurationType = durationType,
                        Duration = duration,
                        RuntimeParams = new Dictionary<string, object>
                        {
                            // { BuffRuntimeParameters.ExistOnlyBattle, true }
                        }
                    });
                });
            }
        }
    }
}