using System;
using System.Collections.Generic;
using Bullet;
using Bullet.Data;
using Character;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Bullet")]
    public class SkillFlowFeatureBulletNode : SkillFlowFeatureNode
    {
        public enum NoTargetLocomotionType
        {
            Simple,
        }

        public enum TargetLocomotionType
        {
            StraightFollow,
            BezierFollow,
        }

        private const string OnCreatePort = "onCreate";
        private const string OnHitTargetPort = "onHitTarget";
        private const string OnHitOthersPort = "onHitAlly";
        private const string OnDestroyPort = "onDestroy";

        [Title("子弹外观配置")] public GameObject prefab;
        public Vector3 prefabLocalPosition;
        public Vector3 prefabLocalEulerAngle;

        [Title("子弹命中配置")] [MinValue(0)] public int hitTimes = 999;
        [MinValue(0)] public float hitColdDown = 10;
        public bool destroyOnObstacle = true;
        public LayerMask obstacleLayers;
        public bool hitEnemy = true;
        public bool hitAlly = false;
        public bool hitSelf = false;

        [Title("子弹发射配置")] [InfoBox("开火时与发射者的相对位置")]
        public Vector3 fireRelativePosition;

        [InfoBox("开火时与发射者和目标向量（如果没有目标则采用发射者的正方向）的相对旋转欧拉角")]
        public Vector3 fireRelativeEulerAngle;

        public float fireSpeed = 10;

        [Title("子弹生命周期配置")] [MinValue(0f)] public float duration = 1;
        [MinValue(0f)] public float allowHitAfterCreate;

        [FormerlySerializedAs("destroyTime")] [MinValue(0f)]
        public float destroyDelay;

        [Title("子弹碰撞配置")] public BulletColliderType colliderType;

        #region 盒状参数配置

        [ShowIf("colliderType", BulletColliderType.Box)] [BoxGroup("Box", false)]
        public Vector3 boxSize;

        #endregion

        #region 球状参数配置

        [ShowIf("colliderType", BulletColliderType.Sphere)] [BoxGroup("Sphere", false)]
        public float sphereRadius;

        #endregion

        [Title("子弹目标及运动配置")] public bool toTarget;
        [ShowIf("@!toTarget", true, true)] public NoTargetLocomotionType noTargetLocomotionType;
        [ShowIf("@toTarget", true, true)] public TargetLocomotionType targetLocomotionType;

        [ShowIf("@toTarget", true, true)] public bool hasFollowTime = false;

        [ShowIf("@toTarget && hasFollowTime", true, true)] [MinValue(0.1f)]
        public float followTime = 0.1f;

        #region 无目标简单运动配置

        [ShowIf("@!toTarget && noTargetLocomotionType == NoTargetLocomotionType.Simple", true, true)]
        [BoxGroup("NoTargetSimpleLocomotion", false)]
        public float linearAccelerateSpeed;

        [ShowIf("@!toTarget && noTargetLocomotionType == NoTargetLocomotionType.Simple", true, true)]
        [BoxGroup("NoTargetSimpleLocomotion", false)]
        public Vector3 angleSpeed;

        [ShowIf("@!toTarget && noTargetLocomotionType == NoTargetLocomotionType.Simple", true, true)]
        [BoxGroup("NoTargetSimpleLocomotion", false)]
        public Vector3 angleAccelerateSpeed;

        #endregion

        #region 有目标直线运动配置

        [ShowIf("@toTarget && targetLocomotionType == TargetLocomotionType.StraightFollow", true, true)]
        [BoxGroup("TargetStraightLocomotion", false)]
        public float straightLinearAccelerateSpeed;

        [ShowIf("@toTarget && targetLocomotionType == TargetLocomotionType.StraightFollow", true, true)]
        [BoxGroup("TargetStraightLocomotion", false)]
        public float straightRotationSpeed = 10f;

        #endregion

        #region 有目标贝塞尔运动配置

        [ShowIf("@toTarget && targetLocomotionType == TargetLocomotionType.BezierFollow", true, true)]
        [BoxGroup("TargetBezierLocomotion", false)]
        public Vector3 bezierControlPointOffset;

        // 专门用于储存在子弹对象中的数据，Node节点不储存运行时数据
        private const string SegmentStart = "segmentStart"; // 曲线段的起点
        private const string SegmentControl = "segmentControl"; // 曲线段的控制点
        private const string SegmentEnd = "segmentEnd"; // 曲线段的终点
        private const string SegmentProgress = "segmentProgress"; // 当前段的进度（0到1）
        private const string SegmentLength = "segmentLength"; // 当前段的估计长度

        #endregion

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

        public override string Title => "业务——子弹节点";

#if UNITY_EDITOR
        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = OnCreatePort,
                    title = "创建时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnHitTargetPort,
                    title = "命中目标角色时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnHitOthersPort,
                    title = "命中非目标角色时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnDestroyPort,
                    title = "销毁时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case OnCreatePort:
                case OnDestroyPort:
                {
                    if (child is SkillFlowTimelineNode timelineNode)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }

                    if (child is SkillFlowFeatureNode featureNode &&
                        (featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire ||
                         featureNode.GetPayloadsRequire() is SkillFlowFeatureCharactersPayloadsRequire))
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }
                }
                    break;
                case OnHitTargetPort:
                case OnHitOthersPort:
                {
                    if (child is SkillFlowFeatureNode featureNode &&
                        (featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire ||
                         featureNode.GetPayloadsRequire() is SkillFlowFeatureCharactersPayloadsRequire))
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }
                }
                    break;
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case OnCreatePort:
                case OnDestroyPort:
                {
                    if (child is SkillFlowFeatureNode featureNode || child is SkillFlowTimelineNode timelineNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
                case OnHitTargetPort:
                case OnHitOthersPort:
                {
                    if (child is SkillFlowFeatureNode featureNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
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
            if (toTarget && !Target)
            {
                DebugUtil.LogError("The target is not existing while toTarget is true");
                return;
            }

            // 创建子弹发射类并发射子弹
            var bulletInfo = new BulletInfo
            {
                id = RunningId,
                hitTimes = hitTimes,
                hitColdDown = hitColdDown,
                colliderType = colliderType,
                ColliderTypeParams = colliderType switch
                {
                    BulletColliderType.Box => new object[]
                    {
                        Vector3.zero,
                        Quaternion.identity,
                        boxSize
                    },
                    BulletColliderType.Sphere => new object[]
                    {
                        Vector3.zero,
                        sphereRadius
                    },
                    _ => Array.Empty<object>(),
                },
                destroyOnObstacle = destroyOnObstacle,
                obstacleLayers = obstacleLayers,
                hitEnemy = hitEnemy,
                hitAlly = hitAlly,
                hitSelf = hitSelf,
                OnCreate = (bulletObject) =>
                {
                    // 如果子弹采用贝塞尔曲线运动，则在创建时初始化分段
                    if (toTarget && targetLocomotionType == TargetLocomotionType.BezierFollow)
                    {
                        InitializeNewSegment(bulletObject);
                    }

                    ExecuteChildNodes(OnCreatePort, timelineInfo, new List<CharacterObject>());
                },
                OnHit = (bulletObject, hitCharacter) =>
                {
                    if (hitCharacter == bulletObject.target)
                    {
                        if (debug)
                        {
                            DebugUtil.LogOrange($"子弹({bulletObject.info.id})命中目标({hitCharacter.Parameters.DebugName})");
                        }

                        ExecuteChildNodes(
                            OnHitTargetPort,
                            timelineInfo,
                            new List<CharacterObject> { hitCharacter }
                        );
                    }
                    else
                    {
                        if (debug)
                        {
                            DebugUtil.LogOrange($"子弹({bulletObject.info.id})命中其他({hitCharacter.Parameters.DebugName})");
                        }

                        ExecuteChildNodes(
                            OnHitOthersPort,
                            timelineInfo,
                            new List<CharacterObject> { hitCharacter }
                        );
                    }
                },
                OnDestroy = (bulletObject) =>
                {
                    ExecuteChildNodes(OnDestroyPort, timelineInfo, new List<CharacterObject>());
                },
            };
            var firePosition = Caster.Visual.TransformCenterPoint(fireRelativePosition);
            var bulletLauncher = new BulletLauncher(
                prefab,
                prefabLocalPosition,
                Quaternion.Euler(prefabLocalEulerAngle),
                Caster,
                firePosition,
                toTarget
                    ? Quaternion.LookRotation(Target!.Visual.Center.position - firePosition) *
                      Quaternion.Euler(fireRelativeEulerAngle)
                    : Caster.transform.rotation * Quaternion.Euler(fireRelativeEulerAngle),
                fireSpeed,
                duration,
                GetBulletTarget,
                SetBulletLocomotion,
                allowHitAfterCreate,
                destroyDelay,
                new Dictionary<string, object>
                {
                    { BulletLauncher.Debug, debug }
                }
            );
            GameManager.CreateBullet(bulletLauncher, bulletInfo);
        }

        private void ExecuteChildNodes(
            string key,
            TimelineInfo timelineInfo,
            List<CharacterObject> characters
        )
        {
            GetChildNodes(key).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                            case SkillFlowFeatureCharactersPayloadsRequire charactersPayloadsRequire:
                            {
                                featureNode.Execute(timelineInfo, charactersPayloadsRequire.ProvideContext(characters));
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private CharacterObject GetBulletTarget(BulletObject bulletObject, CharacterObject caster)
        {
            return toTarget ? Target : null;
        }

        private void SetBulletLocomotion(
            float timeElapsed,
            BulletObject bullet,
            CharacterObject target,
            float deltaTime
        )
        {
            // 如果配置了存在目标但没有目标，则默认向前运动，这是兜底策略
            if ((target == null || target.Parameters.dead) && toTarget)
            {
                bullet.transform.Translate(Vector3.forward * bullet.speed * deltaTime);
                return;
            }

            // 判断子弹是否有目标
            if (!toTarget)
            {
                // 不存在目标，则根据其运动类型计算运动结果
                switch (noTargetLocomotionType)
                {
                    case NoTargetLocomotionType.Simple:
                    {
                        bullet.speed += deltaTime * linearAccelerateSpeed;
                        bullet.transform.rotation *= Quaternion.AngleAxis(
                            (angleSpeed.x + angleAccelerateSpeed.x * timeElapsed) * deltaTime, Vector3.right);
                        bullet.transform.rotation *=
                            Quaternion.AngleAxis((angleSpeed.y + angleAccelerateSpeed.y * timeElapsed) * deltaTime,
                                Vector3.up);
                        bullet.transform.rotation *= Quaternion.AngleAxis(
                            (angleSpeed.z + angleAccelerateSpeed.z * timeElapsed) * deltaTime, Vector3.forward);
                        bullet.transform.Translate(Vector3.forward * bullet.speed * deltaTime);
                    }
                        break;
                }
            }
            else
            {
                // 存在目标，则根据其运动类型计算运动结果
                switch (targetLocomotionType)
                {
                    case TargetLocomotionType.StraightFollow:
                    {
                        bullet.speed += deltaTime * straightLinearAccelerateSpeed;
                        // 如果不存在追踪时间或处于追踪时间内，改变子弹朝向
                        if (!hasFollowTime || bullet.timeElapsed <= followTime)
                        {
                            // 保持子弹朝向目标角色方向改变
                            bullet.transform.rotation = Quaternion.Lerp(bullet.transform.rotation,
                                Quaternion.LookRotation(target.Visual.Center.position - bullet.transform.position),
                                straightRotationSpeed * deltaTime);
                        }

                        bullet.transform.Translate(Vector3.forward * bullet.speed * deltaTime);
                    }
                        break;
                    case TargetLocomotionType.BezierFollow:
                    {
                        // 如果不存在追踪时间或处于追踪时间内，执行贝塞尔曲线运动，否则就执行简单直线运动
                        if (!hasFollowTime || bullet.timeElapsed <= followTime)
                        {
                            UpdateSegmentEnd(bullet); // 每帧更新终点为最新预测位置

                            var segmentStart = (Vector3)bullet.RuntimeParams[SegmentStart];
                            var segmentControl = (Vector3)bullet.RuntimeParams[SegmentControl];
                            var segmentEnd = (Vector3)bullet.RuntimeParams[SegmentEnd];
                            var segmentLength = (float)bullet.RuntimeParams[SegmentLength];
                            var segmentProgress = (float)bullet.RuntimeParams[SegmentProgress];

                            // 更新段落进度
                            segmentProgress += Time.deltaTime * bullet.speed / segmentLength;
                            segmentProgress = Mathf.Clamp01(segmentProgress);
                            bullet.RuntimeParams[SegmentProgress] = segmentProgress;

                            // 计算贝塞尔曲线上的位置
                            Vector3 currentPos = BezierCurveUtil.CalculateBezierCurve(
                                segmentStart,
                                segmentControl,
                                segmentEnd,
                                segmentProgress
                            );
                            bullet.transform.position = currentPos;

                            // 计算朝向（沿曲线切线方向）
                            Vector3 tangent = CalculateBezierTangent(
                                segmentStart,
                                segmentControl,
                                segmentEnd,
                                segmentProgress
                            );
                            if (tangent != Vector3.zero)
                            {
                                bullet.transform.rotation = Quaternion.LookRotation(tangent);
                            }

                            // 当接近段结束时，初始化新段
                            if (segmentProgress >= 0.98f)
                            {
                                InitializeNewSegment(bullet);
                            }
                        }
                        else
                        {
                            bullet.transform.Translate(Vector3.forward * bullet.speed * deltaTime);
                        }
                    }
                        break;
                }
            }
        }

        private void InitializeNewSegment(BulletObject bulletObject)
        {
            var segmentStart = bulletObject.transform.position;
            var segmentEnd = GetPredictedTargetPosition(bulletObject.target);
            var segmentControl = CalculateControlPoint(segmentStart, segmentEnd);
            var segmentLength = EstimateBezierLength(segmentStart, segmentControl, segmentEnd);

            bulletObject.RuntimeParams[SegmentStart] = segmentStart;
            bulletObject.RuntimeParams[SegmentControl] = segmentControl;
            bulletObject.RuntimeParams[SegmentEnd] = segmentEnd;
            bulletObject.RuntimeParams[SegmentLength] = segmentLength;
            bulletObject.RuntimeParams[SegmentProgress] = 0f;
        }

        private void UpdateSegmentEnd(BulletObject bulletObject)
        {
            // 动态更新终点为最新的预测位置
            var segmentStart = (Vector3)bulletObject.RuntimeParams[SegmentStart];
            var segmentEnd = (Vector3)bulletObject.RuntimeParams[SegmentEnd];
            var newEnd = GetPredictedTargetPosition(bulletObject.target);
            // 判断当前终点与预测终点距离
            if (Vector3.Distance(segmentEnd, newEnd) > 0.1f)
            {
                // 如果当前终点与预测终点距离大于阈值，则更新控制点和终点位置
                segmentEnd = newEnd;
                var segmentControl = CalculateControlPoint(segmentStart, segmentEnd);
                var segmentLength = EstimateBezierLength(segmentStart, segmentControl, segmentEnd);

                bulletObject.RuntimeParams[SegmentControl] = segmentControl;
                bulletObject.RuntimeParams[SegmentEnd] = segmentEnd;
                bulletObject.RuntimeParams[SegmentLength] = segmentLength;
            }
        }

        private Vector3 GetPredictedTargetPosition(CharacterObject target)
        {
            return target.Visual.Center.position + target.Parameters.movementInFrame;
        }

        private Vector3 CalculateControlPoint(Vector3 start, Vector3 end)
        {
            if (Vector3.Distance(start, end) > 1f)
            {
                return (start + end) * 0.5f + bezierControlPointOffset;
            }

            return (start + end) * 0.5f;
        }

        private float EstimateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // 估算贝塞尔曲线长度
            float estimatedLength = Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2);
            return Mathf.Max(estimatedLength, 0.1f); // 避免除零
        }

        private Vector3 CalculateBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // 计算切线（导数）
            return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        }
    }
}