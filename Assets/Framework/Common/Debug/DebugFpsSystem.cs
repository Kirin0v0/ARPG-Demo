using Framework.Core.Singleton;
using UnityEngine;

namespace Framework.Common.Debug
{
    /// <summary>
    /// FPS调试类
    /// </summary>
    internal class DebugFpsSystem : MonoGlobalSingleton<DebugFpsSystem>
    {
        [SerializeField] private Vector2 referencedResolution = new Vector2(1920, 1080);
        [SerializeField] private int textSize = 35;

        private Vector2 ScaleFactor =>
            new(Screen.width / referencedResolution.x, Screen.height / referencedResolution.y);

        private float _deltaTime = 0.0f;

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            var scaleFactor = ScaleFactor;
            var fps = 1.0f / _deltaTime;
            var text = string.Format(" FPS:{0:N0} ", fps);
            var style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal =
                {
                    background = null,
                    textColor = Color.red
                },
                fontSize = (int)(textSize * Mathf.Min(scaleFactor.x, scaleFactor.y))
            };
            var rect = new Rect(0, 0, 500 * scaleFactor.x, 300 * scaleFactor.y);
            GUI.Label(rect, text, style);
        }
    }
}