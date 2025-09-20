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
    [NodeMenuItem("Feature/Remove/Bullet")]
    public class SkillFlowFeatureRemoveBulletNode : SkillFlowFeatureNode
    {
        [Title("删除配置")] public string bulletNodeId;

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

        public override string Title => "业务——删除子弹节点";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            var bulletNode = skillFlow.GetNode(bulletNodeId);
            if (!bulletNode)
            {
                DebugUtil.LogError($"Can't find the Bullet node that matches the specified id({bulletNodeId})");
                return;
            }

            var bulletObject = GameManager.GetBullet(bulletNode.RunningId);
            if (!bulletObject)
            {
                DebugUtil.LogError(
                    $"Can't find the Bullet object that matches the specified id({bulletNode.RunningId})");
                return;
            }

            GameManager.DestroyBullet(bulletObject);
        }
    }
}