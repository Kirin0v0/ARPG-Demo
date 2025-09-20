using System;
using Character;
using Common;
using Framework.Common.Audio;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using Skill.Unit.Feature;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Skill.Unit.TimelineClip
{
    [NodeMenuItem("Timeline Clip/Sound")]
    public class SkillFlowTimelineClipSoundNode : SkillFlowTimelineClipNode
    {
        public enum TargetType
        {
            Caster,
            Target,
            AoE,
            Bullet,
        }

        [Title("音效配置")] public AudioClip audioClip;
        public float volume = 1f;
        public TargetType targetType;

        [ShowIf("targetType", TargetType.AoE)] public string aoeNodeId;

        [ShowIf("targetType", TargetType.Bullet)]
        public string bulletNodeId;

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

        private int _soundId = -1;

        public override string Title => "时间轴片段——音效节点";

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while targetType is Caster");
                        return;
                    }

                    _soundId = Caster.AudioAbility?.PlaySound(audioClip, false, volume) ?? -1;
                }
                    break;
                case TargetType.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while targetType is Target");
                        return;
                    }

                    _soundId = Target.AudioAbility?.PlaySound(audioClip, false, volume) ?? -1;
                }
                    break;
                case TargetType.AoE:
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

                    _soundId = aoeObject.PlaySound(audioClip, false, volume);
                }
                    break;
                case TargetType.Bullet:
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

                    _soundId = bulletObject.PlaySound(audioClip, false, volume);
                }
                    break;
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while targetType is Caster");
                        return;
                    }

                    Caster.AudioAbility?.StopSound(_soundId);
                }
                    break;
                case TargetType.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while targetType is Target");
                        return;
                    }

                    Target.AudioAbility?.StopSound(_soundId);
                }
                    break;
                case TargetType.AoE:
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

                    aoeObject.StopSound(_soundId);
                }
                    break;
                case TargetType.Bullet:
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

                    bulletObject.StopSound(_soundId);
                }
                    break;
            }
        }
    }
}