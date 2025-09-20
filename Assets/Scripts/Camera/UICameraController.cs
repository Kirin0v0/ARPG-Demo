using System;
using UnityEngine;

namespace Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class UICameraController: MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;

        private UnityEngine.Camera _uiCamera;

        private void Awake()
        {
            _uiCamera = GetComponent<UnityEngine.Camera>();
        }

        private void LateUpdate()
        {
            // UI相机位置与主相机保持同步
            transform.position = mainCamera.transform.position;
            transform.rotation = mainCamera.transform.rotation;
            // 此外，视距等也要保持同步
            _uiCamera.fieldOfView = mainCamera.fieldOfView;
            _uiCamera.nearClipPlane = mainCamera.nearClipPlane;
            _uiCamera.farClipPlane = mainCamera.farClipPlane;
            _uiCamera.rect = mainCamera.rect;
        }
    }
}