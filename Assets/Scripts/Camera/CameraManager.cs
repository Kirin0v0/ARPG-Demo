using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Camera.Data;
using Character;
using Cinemachine;
using Common;
using Events;
using Framework.Core.Lifecycle;
using Inputs;
using Map;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Unit.TimelineClip;
using UnityEngine;
using VContainer;

namespace Camera
{
    /// <summary>
    /// 管理运行中的各种相机的创建、切换和销毁，不负责场景的预设相机
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private CameraModel _cameraModel;
        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;

        [Title("相机池")] [SerializeField] private Transform cameraPool;

        #region 对话相机

        [Title("对话相机预设体")] [SerializeField] private CinemachineVirtualCamera dialogueCameraPrefab;
        private CinemachineVirtualCamera _dialogueDefaultCamera;
        private GameObject _dialogueCameraTargetGroupPool;
        private readonly Dictionary<string, CinemachineVirtualCamera> _dialogueAppendCameras = new();
        private readonly List<CinemachineTargetGroup> _dialogueCameraTargetGroups = new();

        #endregion

        #region 技能相机

        [Title("技能相机预设体")] [SerializeField] private CinemachineVirtualCamera skillCameraPrefab;
        private readonly Dictionary<string, GameObject> _skillCameraGroups = new();

        #endregion

        private void Awake()
        {
            _mapManager.BeforeMapLoad += ClearAllCameras;
        }

        private void OnDestroy()
        {
            _mapManager.BeforeMapLoad -= ClearAllCameras;
        }

        public CinemachineVirtualCamera CreateDialogueDefaultCamera()
        {
            if (_dialogueDefaultCamera)
            {
                return _dialogueDefaultCamera;
            }

            var visualCameraGameObject = GameObject.Instantiate(dialogueCameraPrefab.gameObject, cameraPool);
            visualCameraGameObject.name = "Dialogue Default Virtual Camera";
            visualCameraGameObject.SetActive(true);
            var virtualCamera = visualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
            _dialogueDefaultCamera = virtualCamera;
            return virtualCamera;
        }

        public void DestroyDialogueDefaultCamera()
        {
            if (!_dialogueDefaultCamera)
            {
                return;
            }

            GameObject.Destroy(_dialogueDefaultCamera.gameObject);
            _dialogueDefaultCamera = null;
        }

        public CinemachineVirtualCamera CreateDialogueAppendCamera(string cameraId)
        {
            if (_dialogueAppendCameras.TryGetValue(cameraId, out var camera))
            {
                return camera;
            }

            var visualCameraGameObject = GameObject.Instantiate(dialogueCameraPrefab.gameObject, cameraPool);
            visualCameraGameObject.name = $"Dialogue Append Virtual Camera({cameraId})";
            visualCameraGameObject.SetActive(true);
            var virtualCamera = visualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
            _dialogueAppendCameras.Add(cameraId, virtualCamera);
            return virtualCamera;
        }

        public void DestroyDialogueAppendCamera(string cameraId)
        {
            if (!_dialogueAppendCameras.TryGetValue(cameraId, out var camera))
            {
                return;
            }

            GameObject.Destroy(camera.gameObject);
            _dialogueAppendCameras.Remove(cameraId);
        }

        public void CreateDialogueCameraTargetGroupAndHandler(
            (Transform target, float weight, float radius)[] members,
            Vector3 offset,
            out CinemachineTargetGroup targetGroup,
            out Transform handler
        )
        {
            if (!_dialogueCameraTargetGroupPool)
            {
                _dialogueCameraTargetGroupPool = new GameObject("Dialogue Camera Target Group Pool")
                {
                    transform =
                    {
                        parent = cameraPool.transform,
                    },
                };
            }

            // 创建目标组
            var cinemachineTargetGroup = new GameObject("Camera Target Group")
            {
                transform = { parent = _dialogueCameraTargetGroupPool.transform }
            }.AddComponent<CinemachineTargetGroup>();
            cinemachineTargetGroup.m_PositionMode = CinemachineTargetGroup.PositionMode.GroupCenter;
            cinemachineTargetGroup.m_RotationMode = CinemachineTargetGroup.RotationMode.GroupAverage;
            cinemachineTargetGroup.m_UpdateMethod = CinemachineTargetGroup.UpdateMethod.LateUpdate;
            members.ForEach(member => cinemachineTargetGroup.AddMember(member.target, member.weight, member.radius));
            cinemachineTargetGroup.DoUpdate();
            _dialogueCameraTargetGroups.Add(cinemachineTargetGroup);
            targetGroup = cinemachineTargetGroup;

            // 创建实际句柄
            var handlerGameObject = new GameObject("Camera Handler")
            {
                transform = { parent = cinemachineTargetGroup.transform, localPosition = offset }
            };
            handler = handlerGameObject.transform;
        }

        public void ClearAllDialogueCameras()
        {
            DestroyDialogueDefaultCamera();
            _dialogueAppendCameras.ForEach(pair => { GameObject.Destroy(pair.Value.gameObject); });
            _dialogueAppendCameras.Clear();
            _dialogueCameraTargetGroups.ForEach(group => { GameObject.Destroy(group.gameObject); });
            _dialogueCameraTargetGroups.Clear();
            if (_dialogueCameraTargetGroupPool)
            {
                GameObject.Destroy(_dialogueCameraTargetGroupPool);
                _dialogueCameraTargetGroupPool = null;
            }
        }

        public (GameObject group, CinemachineVirtualCamera camera) CreateSkillCameraGroup(
            SkillFlowTimelineClipCameraNode cameraNode)
        {
            var cameraId = cameraNode.RunningId;
            var virtualCameraGroup =
                new GameObject($"Skill({cameraNode.skillFlow.Name}) Visual Camera Group({cameraId})")
                {
                    transform =
                    {
                        parent = cameraPool.transform,
                        localPosition = Vector3.zero
                    }
                };
            var visualCameraGameObject =
                GameObject.Instantiate(skillCameraPrefab.gameObject, virtualCameraGroup.transform);
            visualCameraGameObject.name = $"Skill({cameraNode.skillFlow.Name}) Visual Camera({cameraId})";
            visualCameraGameObject.SetActive(true);
            var virtualCamera = visualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
            _skillCameraGroups.Add(cameraId, virtualCameraGroup);
            return (virtualCameraGroup, virtualCamera);
        }

        public void DestroySkillCameraGroup(SkillFlowTimelineClipCameraNode cameraNode)
        {
            var cameraId = cameraNode.RunningId;
            if (!_skillCameraGroups.TryGetValue(cameraId, out var group))
            {
                return;
            }

            GameObject.Destroy(group);
            _skillCameraGroups.Remove(cameraId);
        }

        public void DestroyAllSkillCameraGroups()
        {
            _skillCameraGroups.ForEach(pair => { GameObject.Destroy(pair.Value); });
            _skillCameraGroups.Clear();
        }

        public void ClearAllCameras()
        {
            ClearAllDialogueCameras();
            DestroyAllSkillCameraGroups();
        }
    }
}