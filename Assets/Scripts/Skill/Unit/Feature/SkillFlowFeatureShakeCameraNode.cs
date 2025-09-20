using System;
using System.Collections.Generic;
using Camera.Data;
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
    [NodeMenuItem("Feature/Shake Camera")]
    public class SkillFlowFeatureShakeCameraNode : SkillFlowFeatureNode
    {
        [SerializeField, Title("相机震动配置"), SerializeReference]
        private BaseCameraShakeData shake;

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

        public override string Title => "业务——相机震动节点\n必须有角色列表的输入";
        
        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.CharactersPayloads;
        }

        /// <summary>
        /// 相机震动节点执行
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

            // 遍历目标角色，当且仅当目标存在玩家才产生震动
            targets!.ForEach(target =>
            {
                if (shake is CameraShakeUniformData { useDamageDirectionAsVelocity: true } uniformData)
                {
                    // 玩家作为攻击方，震动速度采用伤害朝向
                    if (Caster == GameManager.Player)
                    {
                        uniformData.SetVelocity(Vector3
                            .ProjectOnPlane(target.Visual.Center.position - Caster.Visual.Center.position, Vector3.up)
                            .normalized);
                    }

                    // 玩家作为受击方，震动速度采用被伤害朝向
                    if (target == GameManager.Player)
                    {
                        uniformData.SetVelocity(-Vector3
                            .ProjectOnPlane(target.Visual.Center.position - Caster.Visual.Center.position, Vector3.up)
                            .normalized);
                    }
                }

                shake?.GenerateShake(Caster.Parameters.position);
            });
        }
    }
}