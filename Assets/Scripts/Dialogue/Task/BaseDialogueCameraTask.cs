using Camera;
using Character;
using Cinemachine;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    public abstract class BaseDialogueCameraTask : ActionTask<CharacterReference>
    {
        [RequiredField] public BBParameter<string> cameraId;

        private CameraManager _cameraManager;

        private CameraManager CameraManager
        {
            get
            {
                if (_cameraManager)
                {
                    return _cameraManager;
                }

                _cameraManager = GameEnvironment.FindEnvironmentComponent<CameraManager>();
                return _cameraManager;
            }
        }

        protected CinemachineVirtualCamera FindOrCreateCamera()
        {
            return CameraManager.CreateDialogueAppendCamera(cameraId.value);
        }

        protected void DestroyCamera()
        {
            CameraManager.DestroyDialogueAppendCamera(cameraId.value);
        }

        protected void CreateTargetGroupAndHandler(
            (Transform target, float weight, float radius)[] members,
            Vector3 offset,
            out CinemachineTargetGroup targetGroup,
            out Transform handler
        )
        {
            CameraManager.CreateDialogueCameraTargetGroupAndHandler(members, offset, out targetGroup, out handler);
        }
    }
}