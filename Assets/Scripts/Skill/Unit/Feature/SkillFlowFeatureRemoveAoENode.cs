using System;
using System.Linq;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Remove/AoE")]
    public class SkillFlowFeatureRemoveAoENode : SkillFlowFeatureNode
    {
        [Title("删除配置")] public string aoeNodeId;

        [Inject] private GameManager _gameManager;

        private GameManager GameManager
        {
            get
            {
                if (!_gameManager)
                {
                    _gameManager = GameEnvironment.FindEnvironmentComponent<GameManager>();
                }

                return _gameManager;
            }
        }

        public override string Title => "业务——删除AoE节点";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            var aoeNode = skillFlow.GetNode(aoeNodeId);
            if (!aoeNode)
            {
                DebugUtil.LogError($"Can't find the AoE node that matches the specified id({aoeNodeId})");
                return;
            }

            var aoeObject = GameManager.GetAoE(aoeNode.RunningId);
            if (!aoeObject)
            {
                DebugUtil.LogError(
                    $"Can't find the AoE object that matches the specified id({aoeNode.RunningId})");
                return;
            }

            GameManager.DestroyAoE(aoeObject);
        }
    }
}