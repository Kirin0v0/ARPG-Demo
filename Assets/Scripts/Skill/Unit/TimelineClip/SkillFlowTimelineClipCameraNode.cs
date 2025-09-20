using System;
using Camera;
using Cinemachine;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Skill.Unit.TimelineClip
{
    [NodeMenuItem("Timeline Clip/Camera")]
    public class SkillFlowTimelineClipCameraNode : SkillFlowTimelineClipNode
    {
        public enum Mode
        {
            Fixed,
            Dynamic,
        }

        public enum Follow
        {
            Caster,
            Target,
            GroupCenter,
        }

        public enum LookAt
        {
            Caster,
            Target,
            GroupCenter,
        }

        public enum DollyCartType
        {
            Default,
            Custom,
        }

        [Title("相机配置")] public Mode mode = Mode.Fixed;
        public Follow follow = Follow.Caster;

        [InfoBox("相机相对跟随目标的位置，以目标本地坐标系为坐标系计算")]
        public Vector3 followOffset;

        public LookAt lookAt = LookAt.Caster;

        [ShowIf("mode", Mode.Dynamic)] public DollyCartType dollyCartType;

        [ShowIf("@mode == Mode.Dynamic && dollyCartType == DollyCartType.Default", true, true)]
        public float dollyCartSpeed = 10f;

        [ShowIf("@mode == Mode.Dynamic && dollyCartType == DollyCartType.Custom", true, true)]
        public CinemachineDollyCart dollyCartPrefab;

        [ShowIf("mode", Mode.Dynamic)] public CinemachineSmoothPath smoothPathPrefab;

        [Inject] private CameraManager _cameraManager;

        private CameraManager CameraManager
        {
            get
            {
                if (!_cameraManager)
                {
                    _cameraManager = GameEnvironment.FindEnvironmentComponent<CameraManager>();
                }

                return _cameraManager;
            }
        }
        
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineDollyCart _dollyCart;
        private float _dollyCartOriginSpeed;

        public override string Title => "时间轴片段——相机节点";

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            var result = CameraManager.CreateSkillCameraGroup(this);
            var virtualCameraGroup = result.group;
            _virtualCamera = result.camera;

            // 获取跟随目标
            Transform followTarget = null;
            Vector3 followPosition = Vector3.zero;
            switch (follow)
            {
                case Follow.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError($"Target is not existing in SkillFlow({skillFlow.Id})");
                    }
                    else
                    {
                        followTarget = Target.Visual.Center.transform;
                        followPosition = Target.Visual.TransformCenterPoint(followOffset);
                    }
                }
                    break;
                case Follow.GroupCenter:
                {
                    var targetGroupGameObject = new GameObject("Follow Target Group")
                    {
                        transform = { parent = virtualCameraGroup.transform }
                    };
                    var targetGroup = targetGroupGameObject.AddComponent<CinemachineTargetGroup>();
                    targetGroup.AddMember(Caster.Position.Center.transform, 1, 0);
                    if (Target)
                    {
                        targetGroup.AddMember(Target.Position.Center.transform, 1, 0);
                    }

                    targetGroup.DoUpdate();

                    followTarget = targetGroupGameObject.transform;
                    followPosition = followTarget.TransformPoint(followOffset);
                }
                    break;
                default:
                case Follow.Caster:
                {
                    followTarget = Caster.Visual.Center.transform;
                    followPosition = Caster.Visual.TransformCenterPoint(followOffset);
                }
                    break;
            }

            // 获取看向目标
            Transform lookAtTarget = null;
            switch (lookAt)
            {
                case LookAt.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError($"Target is not existing in SkillFlow({skillFlow.Id})");
                    }
                    else
                    {
                        lookAtTarget = Target.Visual.Center.transform;
                    }
                }
                    break;
                case LookAt.GroupCenter:
                {
                    var targetGroupGameObject = new GameObject("LookAt Target Group")
                    {
                        transform = { parent = virtualCameraGroup.transform }
                    };
                    var targetGroup = targetGroupGameObject.AddComponent<CinemachineTargetGroup>();
                    targetGroup.AddMember(Caster.Visual.Center.transform, 1, 0);
                    if (Target)
                    {
                        targetGroup.AddMember(Target.Visual.Center.transform, 1, 0);
                    }

                    targetGroup.DoUpdate();

                    lookAtTarget = targetGroupGameObject.transform;
                }
                    break;
                default:
                case LookAt.Caster:
                {
                    lookAtTarget = Caster.Visual.Center.transform;
                }
                    break;
            }

            switch (mode)
            {
                case Mode.Fixed:
                {
                    _virtualCamera.transform.position = followPosition;
                    _virtualCamera.LookAt = lookAtTarget;
                    _virtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
                }
                    break;
                case Mode.Dynamic:
                {
                    CinemachineDollyCart dollyCart;
                    switch (dollyCartType)
                    {
                        case DollyCartType.Custom:
                        {
                            var dollyCartGameObject = GameObject.Instantiate(dollyCartPrefab.gameObject,
                                virtualCameraGroup.transform);
                            _dollyCart = dollyCartGameObject.GetComponent<CinemachineDollyCart>();
                        }
                            break;
                        default:
                        case DollyCartType.Default:
                        {
                            var dollyCartGameObject = new GameObject("Default Dolly Cart")
                            {
                                transform =
                                {
                                    parent = virtualCameraGroup.transform
                                }
                            };
                            _dollyCart = dollyCartGameObject.AddComponent<CinemachineDollyCart>();
                            _dollyCart.m_Speed = dollyCartSpeed;
                        }
                            break;
                    }

                    _dollyCartOriginSpeed = _dollyCart.m_Speed;
                    var smoothPathGameObject =
                        GameObject.Instantiate(smoothPathPrefab.gameObject, virtualCameraGroup.transform);
                    var smoothPath = smoothPathGameObject.GetComponent<CinemachineSmoothPath>();
                    smoothPathGameObject.transform.position = followPosition;
                    _dollyCart.m_Path = smoothPath;
                    _virtualCamera.Follow = _dollyCart.transform;
                    _virtualCamera.LookAt = lookAtTarget;
                    var trackedDolly = _virtualCamera.AddCinemachineComponent<CinemachineTrackedDolly>();
                    trackedDolly.m_Path = smoothPath;
                    trackedDolly.m_AutoDolly = new CinemachineTrackedDolly.AutoDolly(true, 0, 2, 5);
                    _virtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
                }
                    break;
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (_dollyCart)
            {
                _dollyCart.m_Speed = _dollyCartOriginSpeed * timelineInfo.Timescale;
            }
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            CameraManager.DestroySkillCameraGroup(this);
            _virtualCamera = null;
        }
    }
}