using System;
using Cinemachine;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Camera.Cinemachine
{
    public class CinemachineFollowLockToTarget : CinemachineComponentBase
    {
        [Title("相机差值")] [InfoBox("锁定目标与跟随目标的有效距离的范围，用于调整相机方位，超过范围按照极限值计算")] [SerializeField]
        private float minTargetDistance;

        [SerializeField] private float maxTargetDistance;

        [InfoBox("根据锁定目标与跟随目标的有效距离计算后方相机距离自身的水平距离，超过范围按照极限值计算")] [SerializeField]
        private float minBackwardDistance;

        [SerializeField] private float maxBackwardDistance;

        [InfoBox("根据锁定目标与跟随目标的有效距离计算后方相机距离自身的竖直距离，超过范围按照极限值计算")] [SerializeField]
        private float minBackwardHeight;

        [SerializeField] private float maxBackwardHeight;

        [Title("屏幕检测")] 
        [InfoBox("根据跟随目标在屏幕位置分为三种场景：" +
                 "\n1.在舒适区范围外则根据屏幕上对象位置计算与舒适区的二维偏移值，根据该值动态调整相机阻尼，值越大相机阻尼越小（达到相机滑动越快的效果）。" +
                 "\n2.在舒适区内且死区外的范围中则保持相机阻尼为固定阻尼值。" +
                 "\n3.在死区范围内则根据屏幕上对象位置计算与死区中心位置的二维偏移值，根据该值动态调整相机阻尼，值越小相机阻尼越大（达到相机滑动越慢的效果）")] 
        [Range(-0.5f, 1.5f)] [SerializeField]
        private float screenCenterX = 0.5f;

        [Range(-0.5f, 1.5f)] [SerializeField] private float screenCenterY = 0.5f;

        [Range(0f, 2f)] [SerializeField]
        private float deadZoneWidth = 0.5f;

        [Range(0f, 2f)] [SerializeField] private float deadZoneHeight = 0.5f;

        [Range(0f, 2f)] [SerializeField] private float softZoneWidth = 0.5f;

        [Range(0f, 2f)] [SerializeField] private float softZoneHeight = 0.5f;

        public Rect SoftGuideRect
        {
            get
            {
                var maxWidth = Mathf.Max(softZoneWidth, deadZoneWidth);
                var maxHeight = Mathf.Max(softZoneHeight, deadZoneHeight);
                return new(
                    screenCenterX - maxWidth / 2, screenCenterY - maxHeight / 2,
                    maxWidth, maxHeight
                );
            }
        }

        public Rect HardGuideRect
        {
            get
            {
                var minWidth = Mathf.Min(softZoneWidth, deadZoneWidth);
                var minHeight = Mathf.Min(softZoneHeight, deadZoneHeight);
                return new(
                    screenCenterX - minWidth / 2, screenCenterY - minHeight / 2,
                    minWidth, minHeight
                );
            }
        }

        [Title("相机跟随")] [InfoBox("跟随阻尼，影响相机跟随速度，注意，数值越小则越灵敏")] [Min(0f), SerializeField]
        private float minXDamping = 0.1f;

        [Min(0f), SerializeField] private float maxXDamping = 30f;

        [Min(0f), SerializeField] private float xDamping = 3f;

        [Min(0f), SerializeField] private float minYDamping = 0.05f;
        [Min(0f), SerializeField] private float maxYDamping = 5f;

        [Min(0f), SerializeField] private float yDamping = 0.5f;

        [Min(0f), SerializeField] private float minZDamping = 0.1f;
        [Min(0f), SerializeField] private float maxZDamping = 30f;

        [Min(0f), SerializeField] private float zDamping = 3f;

        private Vector3 _previousCameraPosition = Vector3.zero; // 先前相机位置
        private float _xDamping; // 当前X轴阻尼
        private float _yDamping; // 当前Y轴阻尼
        private float _zDamping; // 当前Z轴阻尼

        public override bool IsValid => enabled && FollowTarget != null && LookAtTarget != null;

        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        public override bool BodyAppliesAfterAim => true;

        public override bool OnTransitionFromCamera(ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            if (fromCam != null && transitionParams.m_InheritPosition
                                && !CinemachineCore.Instance.IsLiveInBlend(VirtualCamera))
            {
                _previousCameraPosition = FollowTarget?.position ?? (VirtualCamera?.transform.position ?? Vector3.zero);
                _xDamping = xDamping;
                _yDamping = yDamping;
                _zDamping = zDamping;
                return true;
            }

            _previousCameraPosition = FollowTarget?.position ?? (VirtualCamera?.transform.position ?? Vector3.zero);
            _xDamping = xDamping;
            _yDamping = yDamping;
            _zDamping = zDamping;
            return false;
        }

        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
            {
                _previousCameraPosition = FollowTarget?.position ?? (VirtualCamera?.transform.position ?? Vector3.zero);
                _xDamping = xDamping;
                _yDamping = yDamping;
                _zDamping = zDamping;
            }
        }

        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            _previousCameraPosition = pos;
            _xDamping = xDamping;
            _yDamping = yDamping;
            _zDamping = zDamping;
        }

        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
            {
                return;
            }

            // 获取当前相机局部空间下的目标坐标
            var localToWorld = curState.RawOrientation;
            var worldToLocal = Quaternion.Inverse(localToWorld);
            var followPositionInCameraDirection =
                worldToLocal * FollowTargetPosition - worldToLocal * _previousCameraPosition;

            // 获取当前镜头的正交空间大小
            var lens = curState.Lens;
            var targetZ = Mathf.Max(followPositionInCameraDirection.z, 0.1f); // 避免除零
            var screenSize = lens.Orthographic
                ? lens.OrthographicSize
                : Mathf.Tan(0.5f * lens.FieldOfView * Mathf.Deg2Rad) * targetZ;

            // 计算相机推测位置
            var predictedPosition = CalculatePredictedPosition();
            var dynamicXDamping = xDamping;
            var dynamicYDamping = yDamping;
            var dynamicZDamping = zDamping;

            // 根据相机软性区域的三维空间偏差值动态调整阻尼系数，偏差越大阻尼越小
            var softGuideOrtho = ScreenToOrtho(SoftGuideRect, screenSize, lens.Aspect);
            var cameraSoftGuideOffset = OrthoOffsetToScreenBounds(followPositionInCameraDirection, softGuideOrtho);
            dynamicXDamping *= 1 - Mathf.Abs(cameraSoftGuideOffset.x) / screenSize;
            dynamicYDamping *= 1 - Mathf.Abs(cameraSoftGuideOffset.y) / screenSize;
            dynamicZDamping *= 1 - Mathf.Abs(cameraSoftGuideOffset.magnitude) / screenSize;

            // 如果处于死区，就根据硬性区域的中心位置动态调整阻尼系数，偏差越小阻尼越大
            if (CheckInGuideRect(HardGuideRect, followPositionInCameraDirection))
            {
                var hardGuideOrtho = ScreenToOrtho(HardGuideRect, screenSize, lens.Aspect);
                var centerOffset = OrthoOffsetToScreenCenterOffset(followPositionInCameraDirection, hardGuideOrtho);
                dynamicXDamping /= Mathf.Abs(centerOffset.x / (hardGuideOrtho.width / 2f)) * 0.1f;
                dynamicYDamping /= Mathf.Abs(centerOffset.y / (hardGuideOrtho.height / 2f)) * 0.1f;
                dynamicZDamping /= Mathf.Abs(centerOffset.magnitude / (hardGuideOrtho.size.magnitude / 4f)) * 0.1f;
            }

            // 这里平滑改变阻尼系数，避免来回在多个区域切换时阻尼突然改变引起的屏幕震动
            _xDamping = Mathf.Lerp(_xDamping, dynamicXDamping, deltaTime);
            _yDamping = Mathf.Lerp(_yDamping, dynamicYDamping, deltaTime);
            _zDamping = Mathf.Lerp(_zDamping, dynamicZDamping, deltaTime);
            // 限制阻尼值
            _xDamping = Mathf.Clamp(_xDamping, minXDamping, maxXDamping);
            _yDamping = Mathf.Clamp(_yDamping, minYDamping, maxYDamping);
            _zDamping = Mathf.Clamp(_zDamping, minZDamping, maxZDamping);
            // 应用阻尼计算当前帧的相机位置
            var dampedPosition = _previousCameraPosition + VirtualCamera.DetachedFollowTargetDamp(
                predictedPosition - _previousCameraPosition,
                new Vector3(_xDamping, _yDamping, _zDamping),
                Mathf.Max(deltaTime, 0)
            );
            // 记录当前帧的相机位置
            _previousCameraPosition = dampedPosition;
            curState.RawPosition = dampedPosition;

            return;

            bool CheckInGuideRect(Rect rect, Vector3 targetPosition)
            {
                // 检查跟随目标是否处于指定范围内
                var ortho = ScreenToOrtho(rect, screenSize, lens.Aspect);
                if (targetPosition.x < ortho.xMin || targetPosition.x > ortho.xMax ||
                    targetPosition.y < ortho.yMin || targetPosition.y > ortho.yMax)
                {
                    return false;
                }

                return true;
            }

            Rect ScreenToOrtho(Rect rScreen, float orthoSize, float aspect)
            {
                var r = new Rect
                {
                    yMax = 2 * orthoSize * ((1f - rScreen.yMin) - 0.5f),
                    yMin = 2 * orthoSize * ((1f - rScreen.yMax) - 0.5f),
                    xMin = 2 * orthoSize * aspect * (rScreen.xMin - 0.5f),
                    xMax = 2 * orthoSize * aspect * (rScreen.xMax - 0.5f)
                };
                return r;
            }

            Vector2 OrthoOffsetToScreenBounds(Vector3 targetPosition, Rect screenRect)
            {
                var delta = Vector3.zero;
                if (targetPosition.x < screenRect.xMin)
                    delta.x += targetPosition.x - screenRect.xMin;
                if (targetPosition.x > screenRect.xMax)
                    delta.x += targetPosition.x - screenRect.xMax;
                if (targetPosition.y < screenRect.yMin)
                    delta.y += targetPosition.y - screenRect.yMin;
                if (targetPosition.y > screenRect.yMax)
                    delta.y += targetPosition.y - screenRect.yMax;
                return delta;
            }

            Vector2 OrthoOffsetToScreenCenterOffset(Vector3 targetPosition, Rect screenRect)
            {
                var delta = Vector3.zero;
                delta.x = targetPosition.x - screenRect.center.x;
                delta.y = targetPosition.y - screenRect.center.y;
                return delta;
            }
        }

        private Vector3 CalculatePredictedPosition()
        {
            // 计算相机在锁定目标->跟随目标延长线的向量
            var followTargetPosition = new Vector3(FollowTargetPosition.x, 0, FollowTargetPosition.z);
            var lookAtTargetPosition = new Vector3(LookAtTargetPosition.x, 0, LookAtTargetPosition.z);
            var direction = followTargetPosition - lookAtTargetPosition;
            var directionProjection = new Vector3(direction.x, 0, direction.z);
            // 计算在该延长线上的相机位置
            var distance = Vector3.Distance(followTargetPosition, lookAtTargetPosition);
            var ratio = Mathf.Clamp(distance - minTargetDistance / maxTargetDistance - minTargetDistance, 0, 1);
            var backwardDistance = ratio * (maxBackwardDistance - minBackwardDistance) + minBackwardDistance;
            var backwardHeight = ratio * (maxBackwardHeight - minBackwardHeight) + minBackwardHeight;
            var backwardDistanceOffset = directionProjection.normalized * backwardDistance;
            var backwardHeightOffset = new Vector3(0, backwardHeight, 0);
            // 最终是以跟随目标为起点计算位置
            return FollowTargetPosition + backwardDistanceOffset + backwardHeightOffset;
        }
    }
}