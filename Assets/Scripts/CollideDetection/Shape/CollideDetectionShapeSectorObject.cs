using System;
using System.Collections.Generic;
using Character;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CollideDetection.Shape
{
    public class CollideDetectionShapeSectorObject : BaseCollideDetectionShapeObject
    {
        [SerializeField] private bool debugWire = false;
        [ReadOnly, ShowInInspector] private float insideRadius = 0f;
        [ReadOnly, ShowInInspector] private float radius = 1f;
        [ReadOnly, ShowInInspector] private float height = 1f;
        [ReadOnly, ShowInInspector] private float anglePivot = 0f;
        [ReadOnly, ShowInInspector] private float angle = 60f;

        private readonly List<Vector3> _startPoints = new();
        private readonly List<Vector3> _endPoints = new();
        private readonly List<(Vector3, Vector3)> _raycasts = new();

        public void SetParams(
            Transform center,
            Vector3 localPosition,
            Quaternion localRotation,
            float insideRadius,
            float radius,
            float height,
            float anglePivot,
            float angle,
            bool @fixed
        )
        {
            this.center = center;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.insideRadius = insideRadius;
            this.radius = radius;
            this.height = height;
            this.anglePivot = anglePivot;
            this.angle = angle;
            this.@fixed = @fixed;
        }

        public override void Detect(Action<Collider> detectionDelegate, int layerMask = -1)
        {
            // 先粗略检测盒形范围内的碰撞体
            var boxSize = new Vector3
            {
                x = radius * 2,
                y = height,
                z = radius * 2
            };
            var boxResults = Physics.OverlapBox(Position, boxSize / 2, Rotation, layerMask);

            // 检测碰撞体是否处于外半径内或是已处于检测间隔期内
            var checkResults = new List<Collider>();
            foreach (var result in boxResults)
            {
                if (!result)
                {
                    continue;
                }

                var closestPoint = result.ClosestPoint(Position);
                var distanceDirection = closestPoint - Position;
                var sqrDistance = distanceDirection.sqrMagnitude;

                // 过滤外半径外的碰撞体
                if (sqrDistance > radius * radius)
                {
                    continue;
                }

                checkResults.Add(result);
            }

            // 如果粗略检测没有碰撞体，则直接返回
            if (checkResults.Count == 0)
            {
                return;
            }

            // 最后检测内半径到外半径之间的碰撞体，按照角度检测，规定180度检测一个周期的采样
            var angleStep = 90f;
            angle = Mathf.Clamp(angle, 0f, 360f);
            var startAngle = anglePivot + -angle / 2;
            var endAngle = anglePivot + angle / 2;
            var forwardDirection = Rotation * Vector3.forward;
            while (startAngle < endAngle && checkResults.Count != 0)
            {
                var newStartAngle = startAngle;
                var newEndAngle = Mathf.Min(newStartAngle + angleStep, endAngle);

                // 计算起始线线段和结束线线段
                var axisDirection = Rotation * Vector3.up;
                var startLineDirection = Quaternion.AngleAxis(newStartAngle, axisDirection) * forwardDirection;
                var endLineDirection = Quaternion.AngleAxis(newEndAngle, axisDirection) * forwardDirection;

                // 多次采样从起始线段的点向结束线段的点发射碰撞体
                var radiusStep = 0.5f;
                var newRadius = insideRadius;
                var stop = false;
                while (!stop && checkResults.Count != 0)
                {
                    // 如果处于扇形圆心就不发射碰撞体
                    if (Mathf.Approximately(newRadius, 0f))
                    {
                        newRadius += radiusStep;
                        continue;
                    }

                    // 在外半径上发射碰撞体
                    if (newRadius >= radius)
                    {
                        newRadius = radius;
                        stop = true;
                    }

                    // 这里起始线段的指定点向结束线段的指定点发射碰撞体
                    var startPoint = Position + newRadius * startLineDirection;
                    var endPoint = Position + newRadius * endLineDirection;
                    var direction = endPoint - startPoint;
                    var raycastHits = Physics.BoxCastAll(startPoint,
                        new Vector3(0.01f, height, 0.01f), direction,
                        Quaternion.LookRotation(direction), direction.magnitude);
                    _startPoints.Add(startPoint);
                    _endPoints.Add(endPoint);
                    _raycasts.Add(new ValueTuple<Vector3, Vector3>(startPoint, direction));

                    foreach (var raycastHit in raycastHits)
                    {
                        var result = raycastHit.collider;
                        if (!result)
                        {
                            continue;
                        }

                        // 如果是之前检测到的碰撞体，则认为处于内半径到外半径之间
                        if (checkResults.Contains(result))
                        {
                            detectionDelegate?.Invoke(result);
                            // 移除已经检测到的碰撞体，不在后续检测
                            checkResults.Remove(result);
                        }
                    }

                    newRadius += radiusStep;
                }

                startAngle += angleStep;
            }
        }

        protected override void DrawGizmosInternal()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                _startPoints.ForEach(point => { Gizmos.DrawSphere(point, 0.1f); });
                Gizmos.color = Color.green;
                _endPoints.ForEach(point => { Gizmos.DrawSphere(point, 0.1f); });
                Gizmos.color = Color.red;
                _raycasts.ForEach(ray => { Gizmos.DrawRay(ray.Item1, ray.Item2); });
            }

            Gizmos.color = Color.cyan;
            var sectorMesh = MeshUtil.Generate3DSectorMesh(
                Mathf.Abs(insideRadius),
                Mathf.Abs(radius),
                anglePivot,
                Mathf.Abs(angle),
                Vector3.forward,
                height
            );
            var matrix = new Matrix4x4();
            matrix.SetTRS(
                Position,
                Rotation,
                Vector3.one
            );
            Gizmos.matrix = matrix;
            if (debugWire)
            {
                Gizmos.DrawWireMesh(sectorMesh);
            }
            else
            {
                Gizmos.DrawMesh(sectorMesh);
            }
        }
    }
}