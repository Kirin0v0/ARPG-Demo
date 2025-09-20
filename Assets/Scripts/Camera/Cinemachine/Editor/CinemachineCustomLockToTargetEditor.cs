using System;
using Cinemachine.Editor;
using Cinemachine.Utility;
using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;

namespace Camera.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollowLockToTarget))]
    public class CinemachineCustomLockToTargetEditor : BaseEditor<CinemachineFollowLockToTarget>
    {
        private Texture2D _gray;

        private Texture2D Gray
        {
            get
            {
                if (_gray == null)
                {
                    var origin = Texture2D.grayTexture;
                    _gray = new Texture2D(origin.width, origin.height, origin.format, false);
                    // 获取原纹理像素并修改透明度
                    Color[] pixels = origin.GetPixels();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].a = 0.5f; // 修改透明度
                    }

                    // 设置新纹理像素并应用
                    _gray.SetPixels(pixels);
                    _gray.Apply();
                }

                return _gray;
            }
        }

        private void OnEnable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
        }

        private void OnDisable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGUI;
        }

        private void OnGUI()
        {
            if (Target == null)
            {
                return;
            }

            var softRect = new Rect(Target.SoftGuideRect.x * Screen.width, Target.SoftGuideRect.y * Screen.height,
                Target.SoftGuideRect.width * Screen.width, Target.SoftGuideRect.height * Screen.height);
            GUI.Box(softRect, new GUIContent
            {
            }, new GUIStyle
            {
                normal =
                {
                    background = Gray,
                    textColor = Color.white,
                },
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter,
            });

            var hardGuideRect = new Rect(Target.HardGuideRect.x * Screen.width, Target.HardGuideRect.y * Screen.height,
                Target.HardGuideRect.width * Screen.width, Target.HardGuideRect.height * Screen.height);
            GUI.Box(hardGuideRect, new GUIContent
            {
            }, new GUIStyle
            {
                normal =
                {
                    background = Texture2D.linearGrayTexture,
                    textColor = Color.white
                },
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter,
            });
        }
    }
}