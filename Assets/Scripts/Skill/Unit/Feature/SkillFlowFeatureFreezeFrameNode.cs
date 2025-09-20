using System;
using System.Collections.Generic;
using Character;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Freeze Frame")]
    public class SkillFlowFeatureFreezeFrameNode : SkillFlowFeatureNode
    {
        [Title("顿帧配置")] [SerializeField] private float freezeDuration = 0.1f;
        [SerializeField] private float freezeTimeScale = 0.2f;

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

        public override string Title => "业务——帧数停顿节点\n必须有角色列表的输入";
        
        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.CharactersPayloads;
        }

        /// <summary>
        /// 帧数停顿节点执行
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
            // 遍历目标角色，当且仅当目标存在玩家才产生停顿
            targets!.ForEach(target =>
            {
                if (target == GameManager.Player)
                {
                    GameManager.AddTimeScaleComboCommand(
                        $"{Caster.Parameters.name}{Caster.Parameters.id}PlaySkill{skillFlow.Name}Node{RunningId}",
                        freezeDuration,
                        freezeTimeScale
                    );
                }
            });
        }
    }
}