using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Camera
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraLockBehaviour : MonoBehaviour
    {
        [Title("相机差值")] [MinMaxSlider(0, 20), InfoBox("锁定目标与自身的有效距离的范围，用于调整相机方位，超过范围按照极限值计算")] [SerializeField]
        private Vector2 minMaxTargetDistance;

        [MinMaxSlider(1, 10), InfoBox("根据锁定目标与自身的有效距离计算后方相机距离自身的水平距离，超过范围按照极限值计算")] [SerializeField]
        private Vector2 minMaxBackwardDistance;

        [MinMaxSlider(0, 5), InfoBox("根据锁定目标与自身的有效距离计算后方相机距离自身的竖直距离，超过范围按照极限值计算")] [SerializeField]
        private Vector2 minMaxBackwardHeight;

        [SerializeField] private float horizontalDistance;

        [Title("跟随设置")] [SerializeField, Range(0, 1)]
        private float followCenteringInDeadZone = 0.5f;

        [SerializeField, Range(1, 10)] private float followSpeedBeyondDeadZone = 2f;

        [Title("死区设置")] [SerializeField] private float deadZoneVerticalDistance = 0.5f;
        [SerializeField] private float deadZoneHorizontalDistance = 2f;

        [Title("惯性移动设置")] [SerializeField] private int inertiaMovingFrames = 300;
        [SerializeField] private int inertiaAddFrame = 1;
        [SerializeField] private int inertiaRemoveFrame = 6;
        [SerializeField, Range(0, 1)] private float inertiaFactor = 0.2f;

        [Title("调试")] [SerializeField] private bool debug;

        private bool _inDeadZone;
        private int _inertiaMovingFrames;

        private CinemachineVirtualCamera _virtualCamera;

        private CinemachineVirtualCamera VirtualCamera
        {
            get
            {
                if (!_virtualCamera)
                {
                    _virtualCamera = GetComponent<CinemachineVirtualCamera>();
                }

                return _virtualCamera;
            }
        }

        private Vector3? _followFocusPosition;

        private Vector3? FollowFocusPosition
        {
            set => _followFocusPosition = value;
            get
            {
                if (_followFocusPosition == null)
                {
                    _followFocusPosition = VirtualCamera.Follow.position;
                }

                return _followFocusPosition;
            }
        }

        private readonly List<Vector3> _deadZonePositions = new();

        private void OnEnable()
        {
            _inertiaMovingFrames = 0;
            if (VirtualCamera.Follow)
            {
                FollowFocusPosition = VirtualCamera.Follow.position;
            }
            else
            {
                FollowFocusPosition = null;
            }
        }

        private void LateUpdate()
        {
            if (!VirtualCamera.Follow || !VirtualCamera.LookAt || !FollowFocusPosition.HasValue)
            {
                return;
            }

            // 判断跟随物体现在的位置是否处于移动死区
            if (CheckInDeadZone(VirtualCamera.Follow.position))
            {
                if (debug)
                {
                    DebugUtil.LogGrey(!_inDeadZone ? "跟随物进入死区范围" : "跟随物体处于死区范围");
                }

                _inDeadZone = true;
                _inertiaMovingFrames = Mathf.Max(_inertiaMovingFrames - inertiaRemoveFrame, 0);
                if (debug)
                {
                    DebugUtil.LogGrey("移动惯性帧数: " + _inertiaMovingFrames);
                }
            }
            else
            {
                if (debug)
                {
                    DebugUtil.LogGrey(_inDeadZone ? "跟随物离开死区范围" : "跟随物体处于非死区范围");
                }

                _inDeadZone = false;
                _inertiaMovingFrames = Mathf.Min(_inertiaMovingFrames + inertiaAddFrame, inertiaMovingFrames);
                if (debug)
                {
                    DebugUtil.LogGrey("移动惯性帧数: " + _inertiaMovingFrames);
                }
            }

            // 按照每帧移动规则移动相机，这里使用惯性帧来控制死区和非死区的焦点移动速度
            var distance = CalculateDeadZoneDistance(VirtualCamera.Follow.position);
            FollowFocusPosition = Vector3.Lerp(FollowFocusPosition.Value, VirtualCamera.Follow.position,
                distance * followSpeedBeyondDeadZone * Time.deltaTime *
                Mathf.Lerp(inertiaFactor, 1f, 1f * _inertiaMovingFrames / inertiaMovingFrames));

            MoveCamera();

            bool CheckInDeadZone(Vector3 targetPosition)
            {
                // 计算相机在目标->跟随焦点位置延长线的向量
                var followDirection = FollowFocusPosition.Value - VirtualCamera.LookAt.position;
                var followDirectionProjection = new Vector3(followDirection.x, 0, followDirection.z).normalized;

                // 计算在焦点四边形死区的四个顶点
                var vertexAPosition = FollowFocusPosition.Value + deadZoneVerticalDistance * followDirectionProjection;
                var vertexCPosition =
                    FollowFocusPosition.Value + deadZoneVerticalDistance * -followDirectionProjection;
                var vertexBPosition = FollowFocusPosition.Value + deadZoneHorizontalDistance *
                    (Quaternion.AngleAxis(-90, Vector3.up) *
                     followDirectionProjection).normalized;
                var vertexDPosition = FollowFocusPosition.Value + deadZoneHorizontalDistance *
                    (Quaternion.AngleAxis(90, Vector3.up) *
                     followDirectionProjection).normalized;

                _deadZonePositions.Clear();
                _deadZonePositions.Add(vertexAPosition);
                _deadZonePositions.Add(vertexBPosition);
                _deadZonePositions.Add(vertexCPosition);
                _deadZonePositions.Add(vertexDPosition);

                return MathUtil.InQuadrilateral(vertexAPosition, vertexBPosition, vertexCPosition, vertexDPosition,
                    targetPosition);
            }

            float CalculateDeadZoneDistance(Vector3 targetPosition)
            {
                var offset = targetPosition - FollowFocusPosition.Value;
                var value = 1f;
                foreach (var vertexPosition in _deadZonePositions)
                {
                    var vertexOffset = vertexPosition - FollowFocusPosition.Value;
                    value = Mathf.Max(value, Vector3.Dot(offset, vertexOffset) / vertexOffset.magnitude);
                }

                return value;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!VirtualCamera.Follow || !VirtualCamera.LookAt || !FollowFocusPosition.HasValue)
            {
                return;
            }

            // 绘制相机—目标的线段
            var lookAtDirectionProjection =
                Vector3.ProjectOnPlane(VirtualCamera.LookAt.position - transform.position, Vector3.up);
            var cameraPositionProjection =
                new Vector3(transform.position.x, VirtualCamera.Follow.position.y, transform.position.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(cameraPositionProjection,
                new Vector3(lookAtDirectionProjection.x, 0, lookAtDirectionProjection.z));

            // 绘制死区四边形范围
            Gizmos.color = Color.green;
            Gizmos.DrawLineStrip(_deadZonePositions.ToArray(), true);

            // 绘制焦点目标位置
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(FollowFocusPosition.Value, 0.1f);
        }

        private void MoveCamera()
        {
            if (!VirtualCamera.Follow || !VirtualCamera.LookAt || !FollowFocusPosition.HasValue)
            {
                return;
            }

            // 计算相机在目标->跟随焦点位置延长线的向量
            var followDirection = FollowFocusPosition.Value - VirtualCamera.LookAt.position;
            var followDirectionProjection = new Vector3(followDirection.x, 0, followDirection.z);

            // 计算在跟随物体位置背后（背后指的是看向物体->跟随物体的向量）的相机位置
            var distance = Vector3.Distance(VirtualCamera.Follow.position, VirtualCamera.LookAt.position);
            var ratio = Mathf.Clamp(distance - minMaxTargetDistance.x, 0,
                            minMaxTargetDistance.y - minMaxTargetDistance.x) /
                        (minMaxTargetDistance.y - minMaxTargetDistance.x);
            var backwardDistance =
                ratio * (minMaxBackwardDistance.y - minMaxBackwardDistance.x) + minMaxBackwardDistance.x;
            var backwardHeight =
                ratio * (minMaxBackwardHeight.y - minMaxBackwardHeight.x) + minMaxBackwardHeight.x;
            var backwardDistanceOffset = followDirectionProjection.normalized * backwardDistance;
            var backwardHeightOffset = new Vector3(0, backwardHeight, 0);
            var horizontalDistanceOffset = (Quaternion.AngleAxis(-90, Vector3.up) *
                                            followDirectionProjection).normalized * horizontalDistance;
            var cameraPosition = FollowFocusPosition.Value + backwardDistanceOffset + backwardHeightOffset +
                                 horizontalDistanceOffset;

            // 移动相机
            VirtualCamera.ForceCameraPosition(cameraPosition, VirtualCamera.transform.rotation);
        }
    }
}