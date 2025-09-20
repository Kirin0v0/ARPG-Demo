using Character;
using Cinemachine;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Camera")]
    public class DialogueSetCameraFollow : BaseDialogueCameraTask
    {
        public enum FollowPosition
        {
            Top,
            Center,
            Bottom,
        }

        [RequiredField] public BBParameter<FollowPosition> position;
        [RequiredField] public BBParameter<Vector3> offset;
        [RequiredField] public BBParameter<bool> @fixed;

        protected override string info => $"Set camera {cameraId} to follow {agentInfo}";

        protected override void OnExecute()
        {
            var virtualCamera = FindOrCreateCamera();
            virtualCamera.gameObject.SetActive(false);
            var target = position.value switch
            {
                FollowPosition.Top => agent.Value.Position.Top,
                FollowPosition.Center => agent.Value.Position.Center,
                FollowPosition.Bottom => agent.Value.Position.Bottom,
                _ => agent.Value.Position.Bottom,
            };
            CreateTargetGroupAndHandler(
                new (Transform target, float weight, float radius)[] { (target, 1, 0) },
                offset.value,
                out var targetGroup,
                out var handler
            );
            if (@fixed.value)
            {
                virtualCamera.Follow = null;
                virtualCamera.transform.position = handler.position;
                virtualCamera.DestroyCinemachineComponent<CinemachineHardLockToTarget>();
            }
            else
            {
                virtualCamera.Follow = handler.transform;
                var hardLockToTarget = virtualCamera.GetCinemachineComponent<CinemachineHardLockToTarget>();
                if (!hardLockToTarget)
                {
                    virtualCamera.AddCinemachineComponent<CinemachineHardLockToTarget>();
                }
            }

            virtualCamera.gameObject.SetActive(true);
            EndAction();
        }
    }
}