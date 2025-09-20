using System;
using Character;
using Cinemachine;
using Framework.Common.Debug;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Camera")]
    public class DialogueSetCameraLookAt : BaseDialogueCameraTask
    {
        public enum LookAtPosition
        {
            Top,
            Center,
            Bottom,
        }

        [RequiredField] public BBParameter<LookAtPosition> position;
        [RequiredField] public BBParameter<Vector3> offset;
        [RequiredField] public BBParameter<bool> @fixed;

        protected override string info => $"Set camera {cameraId} to look at {agentInfo}";

        protected override void OnExecute()
        {
            var virtualCamera = FindOrCreateCamera();
            var target = position.value switch
            {
                LookAtPosition.Top => agent.Value.Position.Top,
                LookAtPosition.Center => agent.Value.Position.Center,
                LookAtPosition.Bottom => agent.Value.Position.Bottom,
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
                virtualCamera.LookAt = null;
                virtualCamera.transform.rotation = Quaternion.LookRotation(handler.position - virtualCamera.transform.position);
                virtualCamera.DestroyCinemachineComponent<CinemachineHardLookAt>();
            }
            else
            {
                virtualCamera.LookAt = handler.transform;
                var hardLookAt = virtualCamera.GetCinemachineComponent<CinemachineHardLookAt>();
                if (!hardLookAt)
                {
                    virtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
                }
            }

            EndAction();
        }
    }
}