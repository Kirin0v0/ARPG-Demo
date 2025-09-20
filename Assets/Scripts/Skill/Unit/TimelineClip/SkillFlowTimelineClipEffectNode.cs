using System;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using Skill.Unit.Feature;
using UnityEngine;
using VContainer;

namespace Skill.Unit.TimelineClip
{
    [NodeMenuItem("Timeline Clip/Effect")]
    public class SkillFlowTimelineClipEffectNode : SkillFlowTimelineClipNode
    {
        public enum TargetType
        {
            Caster,
            Target,
            AoE,
            Bullet,
        }

        public enum TargetCharacterPosition
        {
            Center,
            Top,
            Bottom,
        }

        [Title("特效配置")] public GameObject prefab;
        public float simulationSpeed = 1f;
        public TargetType targetType;

        [ShowIf("@targetType == TargetType.Caster || targetType == TargetType.Target", true, true)]
        public TargetCharacterPosition targetCharacterPosition = TargetCharacterPosition.Center;

        [ShowIf("targetType", TargetType.AoE)] public string aoeNodeId;

        [ShowIf("targetType", TargetType.Bullet)]
        public string bulletNodeId;

        [InfoBox("是否将特效物体绑定为目标物体子物体，若绑定则可能会受到目标物体自身影响")]
        public bool bindAsChild;

        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale = Vector3.one;

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

        private GameObject _effect;

        public override string Title => "时间轴片段——特效节点";

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            // 创建特效物体
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while targetType is Caster");
                        return;
                    }

                    _effect = Instantiate(prefab);
                    if (bindAsChild)
                    {
                        _effect.transform.parent = Caster.EffectAbility?.transform;
                    }

                    _effect.transform.position = targetCharacterPosition switch
                    {
                        TargetCharacterPosition.Center => Caster.Visual.TransformCenterPoint(localPosition),
                        TargetCharacterPosition.Top => Caster.Visual.TransformTopPoint(localPosition),
                        TargetCharacterPosition.Bottom => Caster.Visual.TransformBottomPoint(localPosition),
                        _ => Caster.Visual.TransformBottomPoint(localPosition),
                    };
                    _effect.transform.rotation = Caster.transform.rotation * localRotation;
                    _effect.transform.localScale = localScale;
                }
                    break;
                case TargetType.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while targetType is Target");
                        return;
                    }

                    _effect = Instantiate(prefab);
                    if (bindAsChild)
                    {
                        _effect.transform.parent = Target.EffectAbility?.transform;
                    }

                    _effect.transform.position = targetCharacterPosition switch
                    {
                        TargetCharacterPosition.Center => Target.Visual.TransformCenterPoint(localPosition),
                        TargetCharacterPosition.Top => Target.Visual.TransformTopPoint(localPosition),
                        TargetCharacterPosition.Bottom => Target.Visual.TransformBottomPoint(localPosition),
                        _ => Target.Visual.TransformBottomPoint(localPosition),
                    };
                    _effect.transform.rotation = Target.transform.rotation * localRotation;
                    _effect.transform.localScale = localScale;
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

                    _effect = Instantiate(prefab);
                    if (bindAsChild)
                    {
                        _effect.transform.parent = aoeObject.transform;
                    }

                    _effect.transform.position = aoeObject.transform.TransformPoint(localPosition);
                    _effect.transform.rotation = aoeObject.transform.rotation * localRotation;
                    _effect.transform.localScale = localScale;
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

                    _effect = Instantiate(prefab);
                    if (bindAsChild)
                    {
                        _effect.transform.parent = bulletObject.transform;
                    }

                    _effect.transform.position = bulletObject.transform.TransformPoint(localPosition);
                    _effect.transform.rotation = bulletObject.transform.rotation * localRotation;
                    _effect.transform.localScale = localScale;
                }
                    break;
            }

            // 设置特效粒子系统
            var particleSystem = _effect?.GetComponentInChildren<ParticleSystem>();
            if (particleSystem)
            {
                particleSystem.Simulate(timelineClip.Time, true, true);
                particleSystem.Pause();
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            // 每帧更新特效粒子系统
            var particleSystem = _effect?.GetComponent<ParticleSystem>();
            if (particleSystem)
            {
                particleSystem.Simulate(timelineClip.Time * simulationSpeed, true, true);
                particleSystem.Pause();
            }
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (_effect)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(_effect);
                }
                else
                {
                    DestroyImmediate(_effect);
                }

                _effect = null;
            }
        }
    }
}