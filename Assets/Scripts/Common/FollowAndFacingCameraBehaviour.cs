using System;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// 跟随目标并朝向相机组件，用于3D物体UI
    /// </summary>
    public class FollowAndFacingCameraBehaviour : MonoBehaviour
    {
        public Transform follow;
        public Vector3 offset;

        private void LateUpdate()
        {
            if (follow)
            {
                transform.position = follow.position + offset;
            }

            transform.LookAt(UnityEngine.Camera.main?.transform);
            transform.Rotate(Vector3.up, 180, Space.Self);
        }
    }
}