using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill.Unit.Feature
{
    public abstract class SkillFlowFeatureNode : SkillFlowNode
    {
        [Title("调试配置")] public bool debug = false;
        public string debugAlias = "";

        public abstract ISkillFlowFeaturePayloadsRequire GetPayloadsRequire();

        /// <summary>
        /// 框架优化函数，使用上下文储存参数，方便后续拓展使用，但本质上还是调用业务节点的Execute函数
        /// </summary>
        /// <param name="timelineInfo"></param>
        /// <param name="context"></param>
        public void Execute(TimelineInfo timelineInfo, SkillFlowFeaturePayloadContext context)
        {
            var payloadsRequire = GetPayloadsRequire();
            Execute(timelineInfo, payloadsRequire.GetPayloads(context));
        }

        /// <summary>
        /// 业务节点执行函数
        /// </summary>
        /// <param name="timelineInfo"></param>
        /// <param name="payloads">具体传参视业务内部而定</param>
        public void Execute(TimelineInfo timelineInfo, object[] payloads)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: OnExecute");
            }

            OnExecute(timelineInfo, payloads);
        }

        /// <summary>
        /// TimelineNode类的Execute函数的委托函数
        /// </summary>
        /// <param name="timelineInfo"></param>
        protected abstract void OnExecute(TimelineInfo timelineInfo, object[] payloads);
    }
}