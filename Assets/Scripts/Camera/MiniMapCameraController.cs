using System;
using Common;
using Framework.Common.Debug;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class MiniMapCameraController : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;

        [SerializeField, MinValue(0f)] private float height = 10f;

        public UnityEngine.Camera MiniMapCamera { get; private set; }
        public Vector2 ViewportSize { get; private set; } = Vector2.zero;

        private void Awake()
        {
            MiniMapCamera = GetComponent<UnityEngine.Camera>();
            _gameManager.OnPlayerCreated += HandlePlayerCreated;
        }

        private void Update()
        {
            // 设置摄像机位置和方向
            if (_gameManager.Player)
            {
                // 位置=玩家位置+摄像机高度
                transform.position = _gameManager.Player.Parameters.position + Vector3.up * height;
                // 方向=Z轴为向下，Y轴为玩家Z轴
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(Vector3.down, _gameManager.Player.transform.forward), 3f);
            }

            // 获取视口尺寸，分为正交和透视两种计算方式
            if (MiniMapCamera.orthographic)
            {
                ViewportSize = new Vector2(MiniMapCamera.orthographicSize, MiniMapCamera.orthographicSize);
            }
            else
            {
                var frustumCorner1 = MiniMapCamera.ViewportToWorldPoint(new Vector3(0, 0, height));
                var frustumCorner2 = MiniMapCamera.ViewportToWorldPoint(new Vector3(1, 0, height));
                var frustumCorner3 = MiniMapCamera.ViewportToWorldPoint(new Vector3(1, 1, height));
                var frustumCorner4 = MiniMapCamera.ViewportToWorldPoint(new Vector3(0, 1, height));
                ViewportSize = new Vector2(Vector3.Distance(frustumCorner1, frustumCorner2),
                    Vector3.Distance(frustumCorner1, frustumCorner4));
            }
        }

        private void OnDestroy()
        {
            _gameManager.OnPlayerCreated -= HandlePlayerCreated;
            MiniMapCamera = null;
        }

        private void HandlePlayerCreated(PlayerCharacterObject player)
        {
            // 初始化摄像机位置和方向
            // 位置=玩家位置+摄像机高度
            transform.position = player.Parameters.position + Vector3.up * height;
            // 方向=Z轴为向下，Y轴为玩家Z轴
            transform.rotation = Quaternion.LookRotation(Vector3.down, player.transform.forward);
        }
    }
}