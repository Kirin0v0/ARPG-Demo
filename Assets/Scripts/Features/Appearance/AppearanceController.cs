using Framework.Common.Debug;
using Inputs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Appearance
{
    public enum CameraType
    {
        Idle,
        Global,
        Head,
    }

    public class AppearanceController : SerializedMonoBehaviour
    {
        [Title("旋转参数")] [SerializeField] private Vector3 cameraGlobalOffset;
        [SerializeField] private Vector3 cameraHeadOffset;
        [SerializeField] private Vector3 cameraHolderOffset;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float dragSpeed = 1f;

        [Inject] private PlayerInputManager _playerInputManager;

        private Transform _cameraPreviousParent;
        private Transform _cameraHolder; // 摄像机旋转柄
        private float _cameraRotationX;

        private CameraType _cameraType = CameraType.Idle;

        public CameraType CameraType
        {
            set
            {
                if (_cameraType == value)
                {
                    return;
                }

                _cameraType = value;
                SwitchCameraTypeInternal();
            }
            get => _cameraType;
        }

        private void OnEnable()
        {
            // 这里规定只要绑定该脚本就强行将摄像机挪至自身前，聚焦自身
            var camera = UnityEngine.Camera.main?.transform;
            if (camera)
            {
                _cameraPreviousParent = camera.transform.parent;
                CameraType = CameraType.Global;
                if (!_cameraHolder)
                {
                    _cameraHolder = new GameObject("Camera Holder")
                    {
                        transform = { parent = GameObject.FindWithTag("CameraPool").transform }
                    }.transform;
                }

                _cameraHolder.position = transform.TransformPoint(cameraHolderOffset);
                camera.LookAt(_cameraHolder);
                camera.SetParent(_cameraHolder);
            }
        }

        private void Update()
        {
            // 只要有摄像机旋转柄就可以监听旋转键开始旋转
            if (_cameraHolder)
            {
                // 优先级：拖动>旋转
                // 拖动操作时隐藏鼠标光标
                if (_playerInputManager.IsPressed(InputConstants.Drag))
                {
                    _cameraRotationX += dragSpeed * Mouse.current.delta.ReadValue().x;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    return;
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // 旋转操作
                var rotateInputAction = _playerInputManager.GetInputAction(InputConstants.Rotate);
                if (rotateInputAction != null)
                {
                    _cameraRotationX -= rotationSpeed * rotateInputAction.ReadValue<Vector2>().x;
                }
            }
        }

        private void LateUpdate()
        {
            // 摄像机围绕柄旋转
            if (_cameraHolder)
            {
                _cameraHolder.eulerAngles = new Vector3(0f, _cameraRotationX, 0f);
            }
        }

        private void OnDisable()
        {
            // 失活后重置摄像机并销毁旋转柄
            if (_cameraHolder)
            {
                var camera = UnityEngine.Camera.main?.transform;
                if (camera && _cameraPreviousParent)
                {
                    camera.SetParent(_cameraPreviousParent);
                }

                Destroy(_cameraHolder.gameObject);
            }
        }

        [Button]
        private void ShowGlobalCamera()
        {
            CameraType = CameraType.Global;
        }

        [Button]
        private void ShowHeadCamera()
        {
            CameraType = CameraType.Head;
        }

        private void SwitchCameraTypeInternal()
        {
            var camera = UnityEngine.Camera.main?.transform;
            if (!camera)
            {
                return;
            }

            switch (CameraType)
            {
                case CameraType.Global:
                    camera.position = transform.TransformPoint(cameraGlobalOffset);
                    break;
                case CameraType.Head:
                    camera.position = transform.TransformPoint(cameraHeadOffset);
                    break;
            }
        }
    }
}