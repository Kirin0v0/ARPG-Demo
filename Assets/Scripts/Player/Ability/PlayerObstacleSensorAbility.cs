using System;
using System.Collections.Generic;
using Character;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Ability
{
    [Serializable]
    public class ObstacleData
    {
        public bool exist;
        public string name;
        public Vector3 position;
        public float distance;
        public Vector3 collideNormal;
        public bool hasPeak;
        public Vector3 peak;
        public bool allowPass;
        public ObstacleHeightType height;
        public ObstacleWidthType width;
        public ObstacleThicknessType thickness;
    }

    public enum ObstacleHeightType
    {
        Low,
        Medium,
        High,
        CantClimb,
        Unknown,
    }

    public enum ObstacleWidthType
    {
        OverNarrow,
        Narrow,
        Broad,
        Unknown,
    }

    public enum ObstacleThicknessType
    {
        OverThin,
        Thin,
        Thick,
        Unknown,
    }

    /// <summary>
    /// 角色障碍物检测逻辑类，内部使用分段射线检测实现，根据其内部逻辑不支持以下障碍物碰撞器类型：
    /// 1.障碍物下方镂空无碰撞但是上方有碰撞器，静止场景容易漏检；
    /// 2.障碍物顶端上方也有障碍且上方障碍物碰撞器底面不是水平的，在翻越、攀爬等场景上去后容易卡住；
    /// 3.障碍物碰撞器顶面不是规则形状，容易在检测厚度时漏检；
    /// 4.障碍物碰撞器正面是向内倾斜平面的，容易在检测高度时漏检。
    /// 注意，可通行的物体不要配置为障碍物，例如斜坡等，由于斜坡形状不规则在检测时很容易视为无障碍物或低矮障碍物。
    /// </summary>
    public class PlayerObstacleSensorAbility : BaseCharacterOptionalAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;
        
        [FoldoutGroup("检测基础配置")] public LayerMask obstacleLayer; // 障碍物层级
        [FoldoutGroup("检测基础配置")] public float maxObstacleNormalAngle = 80f; // 障碍物法线最大角度，超过则认为障碍物并不阻止玩家移动，不认为算是障碍物
        [FoldoutGroup("检测基础配置")] public float maxSenseDistance = 1f;

        [FoldoutGroup("允许通过范围")] [InfoBox("最小可通行高度")] [SerializeField]
        private float allowPassHeight = 1.5f;

        [FoldoutGroup("障碍物高度检测")]
        [InfoBox("玩家角度：Low可翻越\nLow~Medium可翻上\nHigh可爬上")]
        [ShowInInspector]
        private float lowHeightThreshold => Owner?.CharacterController.stepOffset ?? 0f;

        [FoldoutGroup("障碍物高度检测")] [SerializeField]
        private float mediumHeightThreshold = 0.5f;

        [FoldoutGroup("障碍物高度检测")] [SerializeField]
        private float highHeightThreshold = 1.6f;

        [FoldoutGroup("障碍物高度检测")] [SerializeField]
        private float cantClimbHeightThreshold = 3f;

        [FoldoutGroup("障碍物宽度检测")] [InfoBox("玩家角度：低于Narrow阈值视为过窄无法立足，不可翻上和爬上")] [SerializeField]
        private float narrowWidthThreshold = 0.1f;

        [FoldoutGroup("障碍物宽度检测")] [SerializeField]
        private float broadWidthThreshold = 0.7f;

        [FoldoutGroup("障碍物厚度检测")] [InfoBox("玩家角度：低于Thin阈值视为过薄无法立足，不可翻上和爬上，高于Thick阈值视为较厚，不可翻越")] [SerializeField]
        private float thinThicknessThreshold = 0.1f;

        [FoldoutGroup("障碍物厚度检测")] [SerializeField]
        private float thickThicknessThreshold = 0.7f;

        [FoldoutGroup("调试参数")] [SerializeField]
        private bool showHeightSensorGizmos;

        [FoldoutGroup("调试参数")] [SerializeField]
        private bool showWidthSensorGizmos;

        [FoldoutGroup("调试参数")] [SerializeField]
        private bool showThicknessSensorGizmos;

        [FoldoutGroup("调试参数")] [SerializeField]
        private bool showObstacle;

        [FoldoutGroup("运行时参数")] [ShowInInspector, ReadOnly]
        private ObstacleData frontObstacleData;

        private readonly List<(Vector3 from, Vector3 distance)> _heightSensorRays = new();
        private readonly List<(Vector3 from, Vector3 distance)> _widthSensorRays = new();
        private readonly List<(Vector3 from, Vector3 distance, bool down)> _thicknessSensorRays = new();

        public void Tick()
        {
            frontObstacleData = SenseObstacle();
            Owner.PlayerParameters.obstacleData = frontObstacleData;
        }

        private void OnDrawGizmosSelected()
        {
            if (showHeightSensorGizmos)
            {
                Gizmos.color = Color.magenta;
                foreach (var heightSensorRay in _heightSensorRays)
                {
                    Gizmos.DrawLine(heightSensorRay.from, heightSensorRay.from + heightSensorRay.distance);
                }
            }

            if (showWidthSensorGizmos)
            {
                Gizmos.color = Color.green;
                foreach (var widthSensorRay in _widthSensorRays)
                {
                    Gizmos.DrawLine(widthSensorRay.from, widthSensorRay.from + widthSensorRay.distance);
                }
            }

            if (showThicknessSensorGizmos)
            {
                foreach (var thicknessSensorRay in _thicknessSensorRays)
                {
                    if (thicknessSensorRay.down)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(thicknessSensorRay.from, thicknessSensorRay.from + thicknessSensorRay.distance);
                    }
                    else
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(thicknessSensorRay.from, thicknessSensorRay.from + thicknessSensorRay.distance);
                    }
                }
            }

            if (showObstacle)
            {
                if (frontObstacleData != null && frontObstacleData.exist && frontObstacleData.hasPeak)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(frontObstacleData.peak, 0.1f);
                }
            }
        }

        private ObstacleData SenseObstacle()
        {
            _heightSensorRays.Clear();
            _widthSensorRays.Clear();
            _thicknessSensorRays.Clear();

            if (Owner.Parameters.dead)
            {
                return new ObstacleData
                {
                    exist = false,
                };
            }

            // 优先检测正前方障碍物，只要正前方有障碍物就直接返回
            var frontObstacle = SenseFrontObstacle(
                Owner.Parameters.position + Owner.CharacterController.radius * Owner.transform.forward,
                Owner.transform.forward,
                maxSenseDistance,
                obstacleLayer
            );
            if (frontObstacle.exist)
            {
                return frontObstacle;
            }

            // 如果正前方没有的话才检测左右两边障碍物，并返回最近的障碍物
            var leftFrontObstacleData =
                SenseFrontObstacle(
                    Owner.Parameters.position + -Owner.transform.right * Owner.CharacterController.radius / 2,
                    Owner.transform.forward,
                    maxSenseDistance,
                    obstacleLayer
                );
            var rightFrontObstacleData =
                SenseFrontObstacle(
                    Owner.Parameters.position + Owner.transform.right * Owner.CharacterController.radius / 2,
                    Owner.transform.forward,
                    maxSenseDistance,
                    obstacleLayer
                );
            if (leftFrontObstacleData.exist && rightFrontObstacleData.exist)
            {
                return rightFrontObstacleData.distance <= leftFrontObstacleData.distance
                    ? rightFrontObstacleData
                    : leftFrontObstacleData;
            }

            if (rightFrontObstacleData.exist)
            {
                return rightFrontObstacleData;
            }

            if (leftFrontObstacleData.exist)
            {
                return leftFrontObstacleData;
            }

            return new ObstacleData
            {
                exist = false,
            };
        }

        private ObstacleData SenseFrontObstacle(
            Vector3 originalPosition,
            Vector3 obstacleSenseDirection,
            float obstacleSenseDistance,
            LayerMask obstacleLayer
        )
        {
            // 从起始检测前方有无符合最低高度的障碍物，该障碍物将作为默认障碍物，后续检测中如果发现障碍物改变了就直接中断检测
            Physics.Raycast(
                originalPosition + Vector3.up * lowHeightThreshold,
                obstacleSenseDirection,
                out var defaultObstacleHit,
                obstacleSenseDistance,
                obstacleLayer
            );
            _heightSensorRays.Add((originalPosition + Vector3.up * lowHeightThreshold,
                obstacleSenseDistance * obstacleSenseDirection));

            if (defaultObstacleHit.collider == null)
            {
                return new ObstacleData { exist = false };
            }

            // 获取障碍物与当前检测面向的夹角，如果超出检测角度范围就不认为是障碍物
            var normalAngle = Vector3.Angle(-defaultObstacleHit.normal, obstacleSenseDirection);
            if (normalAngle > maxObstacleNormalAngle)
            {
                return new ObstacleData { exist = false };
            }

            // 视为障碍物
            var obstacleData = new ObstacleData
            {
                exist = true,
                name = defaultObstacleHit.transform.name,
                position = defaultObstacleHit.transform.position,
                distance = defaultObstacleHit.distance,
            };

            // 从起始位置获取该障碍物高度，如果射线第一个碰撞的是其他障碍物就直接返回
            var heightSenseResult = SenseObstacleHeightAndPeak(
                originalPosition,
                defaultObstacleHit.point,
                defaultObstacleHit.normal,
                defaultObstacleHit.transform,
                Owner.transform.forward,
                maxSenseDistance,
                obstacleLayer
            );
            obstacleData.height = heightSenseResult.heightType;
            obstacleData.collideNormal = new Vector3(heightSenseResult.lastCollidedNormal.x, 0,
                heightSenseResult.lastCollidedNormal.z); // 这里法线是在XZ轴的投影

            // 从能获得到的最高点获取障碍物宽度，如果射线第一个碰撞的是其他障碍物就直接返回
            var widthSenseResult = SenseObstacleWidth(
                new Vector3(originalPosition.x, heightSenseResult.lastCollidedPoint.y, originalPosition.z),
                -Owner.transform.right,
                Owner.transform.right,
                defaultObstacleHit.transform,
                Owner.transform.forward,
                maxSenseDistance,
                obstacleLayer
            );
            obstacleData.width = widthSenseResult;

            // 过高和未知的障碍物就不必检测厚度
            if (heightSenseResult.heightType == ObstacleHeightType.CantClimb ||
                heightSenseResult.heightType == ObstacleHeightType.Unknown)
            {
                obstacleData.thickness = ObstacleThicknessType.Unknown;
                obstacleData.hasPeak = false;
                obstacleData.peak = Vector3.zero;
                obstacleData.allowPass = false;
            }
            else
            {
                // 计算沿障碍物接受射线检测的平面向量的上方的位置，用于向平面向量的下方发射射线检测厚度
                var startPosition = heightSenseResult.lastCollidedPoint +
                                    heightSenseResult.thicknessSenseDistance *
                                    Vector3.ProjectOnPlane(Vector3.up, heightSenseResult.lastCollidedNormal);
                // 获取障碍物厚度和是否允许通过，如果射线第一个碰撞的是其他障碍物就直接返回
                var thicknessSenseResult = SenseObstacleThicknessAndAllowPass(
                    startPosition,
                    defaultObstacleHit.transform,
                    heightSenseResult.lastCollidedNormal.y == 0
                        ? obstacleSenseDirection
                        : Vector3.ProjectOnPlane(obstacleSenseDirection, heightSenseResult.lastCollidedNormal),
                    Vector3.ProjectOnPlane(Vector3.down, heightSenseResult.lastCollidedNormal),
                    heightSenseResult.thicknessSenseDistance,
                    obstacleLayer
                );
                obstacleData.thickness = thicknessSenseResult.thicknessType;
                obstacleData.hasPeak = thicknessSenseResult.hasPeak;
                obstacleData.peak = thicknessSenseResult.peak;
                obstacleData.allowPass = thicknessSenseResult.allowPass;
            }

            return obstacleData;
        }

        private (ObstacleHeightType heightType, Vector3 lastCollidedPoint, Vector3 lastCollidedNormal, float
            thicknessSenseDistance) SenseObstacleHeightAndPeak(
                Vector3 originalPosition,
                Vector3 minHeightCollidedPoint,
                Vector3 minHeightCollidedNormal,
                Transform defaultCollider,
                Vector3 obstacleSenseDirection,
                float obstacleSenseDistance,
                LayerMask obstacleLayer
            )
        {
            // 从起始位置的medium高度检测前方有无障碍物
            var mediumHeightSenseResult =
                SenseObstacleHeightInternal(mediumHeightThreshold, minHeightCollidedNormal);
            if (!mediumHeightSenseResult.continueSense)
            {
                if (mediumHeightSenseResult.unknownHeight)
                {
                    return (ObstacleHeightType.Unknown, minHeightCollidedPoint, minHeightCollidedNormal,
                        float.MaxValue);
                }

                return (ObstacleHeightType.Low, minHeightCollidedPoint, minHeightCollidedNormal,
                    mediumHeightThreshold - lowHeightThreshold);
            }

            // 从起始位置high高度检测前方有无障碍物
            var highHeightSenseResult =
                SenseObstacleHeightInternal(highHeightThreshold, mediumHeightSenseResult.senseHit.normal);
            if (!highHeightSenseResult.continueSense)
            {
                if (highHeightSenseResult.unknownHeight)
                {
                    return (ObstacleHeightType.Unknown, mediumHeightSenseResult.senseHit.point,
                        mediumHeightSenseResult.senseHit.normal, float.MaxValue);
                }

                return (ObstacleHeightType.Medium, mediumHeightSenseResult.senseHit.point,
                    mediumHeightSenseResult.senseHit.normal,
                    highHeightThreshold - mediumHeightThreshold);
            }

            // 从起始位置cantClimb高度检测前方有无障碍物
            var cantClimbHeightSenseResult =
                SenseObstacleHeightInternal(cantClimbHeightThreshold, highHeightSenseResult.senseHit.normal);
            if (!cantClimbHeightSenseResult.continueSense)
            {
                if (cantClimbHeightSenseResult.unknownHeight)
                {
                    return (ObstacleHeightType.Unknown, highHeightSenseResult.senseHit.point,
                        highHeightSenseResult.senseHit.normal,
                        float.MaxValue);
                }

                return (ObstacleHeightType.High, highHeightSenseResult.senseHit.point,
                    highHeightSenseResult.senseHit.normal,
                    cantClimbHeightThreshold - highHeightThreshold);
            }

            return (ObstacleHeightType.CantClimb, cantClimbHeightSenseResult.senseHit.point,
                cantClimbHeightSenseResult.senseHit.normal, float.MaxValue);

            (bool continueSense, bool unknownHeight, RaycastHit senseHit) SenseObstacleHeightInternal(
                float height, Vector3 lastCollidedNormal)
            {
                // 从起始位置加上检测高度来检测前方有无障碍物
                var startPosition = originalPosition + Vector3.up * height;
                Physics.Raycast(
                    startPosition,
                    obstacleSenseDirection,
                    out var frontHit,
                    obstacleSenseDistance,
                    obstacleLayer
                );
                _heightSensorRays.Add((startPosition, obstacleSenseDistance * obstacleSenseDirection));

                if (frontHit.collider == null)
                {
                    // 如果之前的平面倾斜就需要额外判断
                    if (lastCollidedNormal.y != 0)
                    {
                        // 在射线检测的终点直接向下发射射线检测碰撞
                        var topPosition = startPosition + obstacleSenseDistance * obstacleSenseDirection;
                        Physics.Raycast(
                            topPosition,
                            Vector3.down,
                            out var downHit,
                            height,
                            obstacleLayer
                        );
                        _heightSensorRays.Add((topPosition, height * Vector3.down));

                        // 如果没有碰撞或与非目标物体碰撞，则认为之前障碍物高度不可确定
                        if (downHit.collider == null || downHit.transform != defaultCollider)
                        {
                            return (false, true, frontHit);
                        }

                        // 如果与目标碰撞就判断法线是否向上，如果是则代表平面向上高度渐增，至少当前位置障碍物可以抵达，否则代表平面向下高度渐降到当前位置不可预测是否可以抵达
                        return Vector3.Dot(Vector3.down, downHit.normal) < 0
                            ? (false, false, frontHit)
                            : (false, true, frontHit);
                    }

                    // 之前的平面水平就代表可以抵达障碍物位置
                    return (false, false, frontHit);
                }

                // 如果与非目标物体碰撞，则认为之前障碍物高度不可确定
                if (frontHit.transform != defaultCollider)
                {
                    return (false, true, frontHit);
                }

                // 如果与目标物体碰撞，则认为要继续检测
                return (true, false, frontHit);
            }
        }

        private ObstacleWidthType SenseObstacleWidth(
            Vector3 originPosition,
            Vector3 leftDirection,
            Vector3 rightDirection,
            Transform defaultCollider,
            Vector3 obstacleSenseDirection,
            float obstacleSenseDistance,
            LayerMask obstacleLayer
        )
        {
            // 从起始位置narrow宽度检测前方两侧有无障碍物
            Physics.Raycast(
                originPosition + narrowWidthThreshold / 2 * leftDirection,
                obstacleSenseDirection,
                out var narrowLeftFrontHit,
                obstacleSenseDistance,
                obstacleLayer
            );
            _widthSensorRays.Add((originPosition + narrowWidthThreshold / 2 * leftDirection,
                obstacleSenseDistance * obstacleSenseDirection));
            Physics.Raycast(
                originPosition + narrowWidthThreshold / 2 * rightDirection,
                obstacleSenseDirection,
                out var narrowRightFrontHit,
                obstacleSenseDistance,
                obstacleLayer
            );
            _widthSensorRays.Add((originPosition + narrowWidthThreshold / 2 * rightDirection,
                obstacleSenseDistance * obstacleSenseDirection));

            // 如果前方两侧无障碍物，则认为当前位置过窄
            if (narrowLeftFrontHit.collider == null || narrowRightFrontHit.collider == null)
            {
                return ObstacleWidthType.OverNarrow;
            }

            // 如果前方两侧障碍物不为默认物体，则认为宽度未知
            if (narrowRightFrontHit.transform != defaultCollider || narrowLeftFrontHit.transform != defaultCollider)
            {
                return ObstacleWidthType.Unknown;
            }

            // 从起始位置broad宽度检测前方两侧有无障碍物
            Physics.Raycast(
                originPosition + broadWidthThreshold / 2 * leftDirection,
                obstacleSenseDirection,
                out var broadLeftFrontHit,
                obstacleSenseDistance,
                obstacleLayer
            );
            _widthSensorRays.Add((originPosition + broadWidthThreshold / 2 * leftDirection,
                obstacleSenseDistance * obstacleSenseDirection));
            Physics.Raycast(
                originPosition + broadWidthThreshold / 2 * rightDirection,
                obstacleSenseDirection,
                out var broadRightFrontHit,
                obstacleSenseDistance,
                obstacleLayer
            );
            _widthSensorRays.Add((originPosition + broadWidthThreshold / 2 * rightDirection,
                obstacleSenseDistance * obstacleSenseDirection));

            // 如果前方两侧无障碍物，则认为当前位置为narrow
            if (broadLeftFrontHit.collider == null || broadRightFrontHit.collider == null)
            {
                return ObstacleWidthType.Narrow;
            }

            // 如果前方两侧障碍物不为默认物体，则认为宽度未知
            if (broadRightFrontHit.transform != defaultCollider || broadLeftFrontHit.transform != defaultCollider)
            {
                return ObstacleWidthType.Unknown;
            }

            // 这里认为当前位置为broad
            return ObstacleWidthType.Broad;
        }

        private (ObstacleThicknessType thicknessType, bool hasPeak, Vector3 peak, bool allowPass)
            SenseObstacleThicknessAndAllowPass(
                Vector3 originalPosition,
                Transform defaultCollider,
                Vector3 thicknessDirection,
                Vector3 obstacleSenseDirection,
                float obstacleSenseDistance,
                LayerMask obstacleLayer
            )
        {
            var allowPass = true;

            // 0.01f值厚度检测
            var zeroThicknessSenseResult = SenseObstacleInternal(originalPosition + thicknessDirection * 0.01f);
            allowPass &= zeroThicknessSenseResult.allowPass;
            // 如果不继续就直接返回
            if (!zeroThicknessSenseResult.continueSense)
            {
                return (ObstacleThicknessType.OverThin, false, Vector3.zero, allowPass);
            }

            // thin值厚度检测
            var thinThicknessSenseResult =
                SenseObstacleInternal(originalPosition + thicknessDirection * thinThicknessThreshold);
            allowPass &= thinThicknessSenseResult.allowPass;
            // 如果不继续就直接返回
            if (!thinThicknessSenseResult.continueSense)
            {
                return (ObstacleThicknessType.OverThin, true, zeroThicknessSenseResult.thicknessHit.point, allowPass);
            }

            // thick值厚度检测
            var thickThicknessSenseResult =
                SenseObstacleInternal(originalPosition + thicknessDirection * thickThicknessThreshold);
            allowPass &= thickThicknessSenseResult.allowPass;
            // 如果不继续就直接返回
            if (!thickThicknessSenseResult.continueSense)
            {
                return (ObstacleThicknessType.Thin, true, zeroThicknessSenseResult.thicknessHit.point, allowPass);
            }

            return (ObstacleThicknessType.Thick, true, zeroThicknessSenseResult.thicknessHit.point, allowPass);

            (bool continueSense, bool allowPass, RaycastHit thicknessHit) SenseObstacleInternal(Vector3 position)
            {
                // 利用平面反方向向量检测厚度
                Physics.Raycast(
                    position,
                    obstacleSenseDirection,
                    out var thicknessHit,
                    obstacleSenseDistance,
                    obstacleLayer
                );
                _thicknessSensorRays.Add((position, obstacleSenseDistance * obstacleSenseDirection, true));

                // 未发生碰撞或碰撞物体不是目标物体就直接返回
                if (thicknessHit.collider == null)
                {
                    return (false, true, thicknessHit);
                }

                // 利用碰撞点和通过量的向上向量去检测通过量
                Physics.Raycast(
                    thicknessHit.point,
                    Vector3.up,
                    out var allowPassHit,
                    allowPassHeight,
                    obstacleLayer
                );
                _thicknessSensorRays.Add((thicknessHit.point, allowPassHeight * Vector3.up, false));

                return (allowPassHit.collider == null, allowPassHit.collider == null, thicknessHit);
            }
        }
    }
}