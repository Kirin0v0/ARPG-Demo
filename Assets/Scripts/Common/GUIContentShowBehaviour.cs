using System;
using System.Collections.Generic;
using System.Linq;
using Events;
using Features.Game;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Common
{
    public enum GUIShowContentType
    {
        Label,
        Texture,
    }

    public enum GUIShowDirection
    {
        FromTopToBottom,
        FromBottomToTop,
    }

    public enum GUIShowPosition
    {
        Above,
        Below,
    }

    [Serializable]
    public class GUIShowContent
    {
        public GUIShowContentType contentType = GUIShowContentType.Label;
        [InfoBox("width等于0代表采用测量尺寸")] public float width;
        [InfoBox("height等于0代表采用测量尺寸")] public float height;

        [ShowIf("contentType", GUIShowContentType.Label)]
        public string labelContent = "";

        [ShowIf("contentType", GUIShowContentType.Label)]
        public GUIStyle labelStyle = new();

        [ShowIf("contentType", GUIShowContentType.Texture)]
        public Texture textureContent = null;

        [NonSerialized] public GUIStyle MeasuredLabelStyle;
        [NonSerialized] public float MeasuredWidth;
        [NonSerialized] public float MeasuredHeight;
    }

    public class GUIContentShowBehaviour : MonoBehaviour
    {
        [SerializeField] private Vector2 referencedResolution = new Vector2(1920, 1080);

        [FormerlySerializedAs("offset")] [SerializeField]
        private Vector3 worldOffset = Vector3.zero;

        [SerializeField] private List<GUIShowContent> contents = new();
        [SerializeField] private GUIShowDirection direction = GUIShowDirection.FromTopToBottom;
        [SerializeField] private GUIShowPosition position = GUIShowPosition.Above;

        public bool AllowGUIShow { get; set; }

        private Vector2 ScaleFactor =>
            new(Screen.width / referencedResolution.x, Screen.height / referencedResolution.y);

        private void Awake()
        {
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.AllowGUIShow, HandleAllowGUIShow);
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.BanGUIShow, HandleBanGUIShow);
        }

        private void OnDestroy()
        {
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.AllowGUIShow, HandleAllowGUIShow);
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.BanGUIShow, HandleBanGUIShow);
        }

        public void SetContent(params GUIShowContent[] contents)
        {
            this.contents.Clear();
            this.contents.AddRange(contents);
        }

        private void OnGUI()
        {
            if (!AllowGUIShow)
            {
                return;
            }

            // GUI展示的世界坐标系位置
            var worldPosition = new Vector3(
                transform.position.x + worldOffset.x,
                transform.position.y + worldOffset.y,
                transform.position.z + worldOffset.z
            );

            // 不在相机视野内就不用执行后续逻辑
            if (!MathUtil.IsWorldBoxInScreen(UnityEngine.Camera.main, worldPosition, new Vector3(0.5f, 0.5f, 0.5f)))
            {
                return;
            }

            // 获取缩放因子
            var scaleFactor = ScaleFactor;

            // 转换为屏幕坐标系
            var screenPosition = MathUtil.GetGUIScreenPosition(UnityEngine.Camera.main, worldPosition);

            // 遍历内容更新测量参数
            contents.ForEach(content =>
            {
                switch (content.contentType)
                {
                    case GUIShowContentType.Label:
                    {
                        var labelStyle = new GUIStyle(content.labelStyle);
                        labelStyle.fontSize = (int)(labelStyle.fontSize * Mathf.Min(scaleFactor.x, scaleFactor.y));
                        content.MeasuredLabelStyle = labelStyle;
                        content.MeasuredWidth = content.width == 0
                            ? labelStyle.CalcSize(new GUIContent(content.labelContent)).x
                            : content.width * scaleFactor.x;
                        content.MeasuredHeight = content.height == 0
                            ? labelStyle.CalcSize(new GUIContent(content.labelContent)).y
                            : content.height * scaleFactor.y;
                    }
                        break;
                    case GUIShowContentType.Texture:
                    {
                        content.MeasuredWidth = content.width == 0
                            ? content.textureContent.width * scaleFactor.x
                            : content.width * scaleFactor.x;
                        content.MeasuredHeight = content.height == 0
                            ? content.textureContent.height * scaleFactor.y
                            : content.height * scaleFactor.y;
                    }
                        break;
                }
            });

            // 遍历内容计算开始位置
            var screenStartPosition = screenPosition;
            foreach (var content in contents)
            {
                switch (direction)
                {
                    case GUIShowDirection.FromTopToBottom:
                    case GUIShowDirection.FromBottomToTop:
                    {
                        switch (position)
                        {
                            case GUIShowPosition.Above:
                            {
                                screenStartPosition.y -= content.MeasuredHeight;
                            }
                                break;
                            case GUIShowPosition.Below:
                            {
                                screenStartPosition.y += content.MeasuredHeight;
                            }
                                break;
                        }
                    }

                        break;
                }
            }

            // 根据位置和方向摆放内容
            var screenY = screenStartPosition.y;
            switch (direction)
            {
                case GUIShowDirection.FromTopToBottom:
                {
                    switch (position)
                    {
                        case GUIShowPosition.Above:
                        {
                            for (var i = 0; i < contents.Count; i++)
                            {
                                var content = contents[i];
                                var rect = new Rect(
                                    screenStartPosition.x - content.MeasuredWidth / 2,
                                    screenY,
                                    content.MeasuredWidth,
                                    content.MeasuredHeight
                                );
                                switch (content.contentType)
                                {
                                    case GUIShowContentType.Label:
                                    {
                                        GUI.Label(rect, content.labelContent, content.MeasuredLabelStyle);
                                    }
                                        break;
                                    case GUIShowContentType.Texture:
                                    {
                                        GUI.DrawTexture(rect, content.textureContent);
                                    }
                                        break;
                                }

                                screenY += content.MeasuredHeight;
                            }
                        }

                            break;
                        case GUIShowPosition.Below:
                        {
                            for (var i = contents.Count - 1; i >= 0; i--)
                            {
                                var content = contents[i];
                                var rect = new Rect(
                                    screenStartPosition.x - content.MeasuredWidth,
                                    screenY,
                                    content.MeasuredWidth,
                                    content.MeasuredHeight
                                );
                                switch (content.contentType)
                                {
                                    case GUIShowContentType.Label:
                                    {
                                        GUI.Label(rect, content.labelContent, content.MeasuredLabelStyle);
                                    }
                                        break;
                                    case GUIShowContentType.Texture:
                                    {
                                        GUI.DrawTexture(rect, content.textureContent);
                                    }
                                        break;
                                }

                                screenY -= content.MeasuredHeight;
                            }
                        }

                            break;
                    }
                }
                    break;
                case GUIShowDirection.FromBottomToTop:
                {
                    switch (position)
                    {
                        case GUIShowPosition.Above:
                        {
                            for (var i = contents.Count - 1; i >= 0; i--)
                            {
                                var content = contents[i];
                                var rect = new Rect(
                                    screenStartPosition.x - content.MeasuredWidth / 2,
                                    screenY,
                                    content.MeasuredWidth,
                                    content.MeasuredHeight
                                );
                                switch (content.contentType)
                                {
                                    case GUIShowContentType.Label:
                                    {
                                        GUI.Label(rect, content.labelContent, content.MeasuredLabelStyle);
                                    }
                                        break;
                                    case GUIShowContentType.Texture:
                                    {
                                        GUI.DrawTexture(rect, content.textureContent);
                                    }
                                        break;
                                }

                                screenY += content.height;
                            }
                        }
                            break;
                        case GUIShowPosition.Below:
                        {
                            for (var i = 0; i < contents.Count; i++)
                            {
                                var content = contents[i];
                                var rect = new Rect(
                                    screenStartPosition.x - content.MeasuredWidth / 2,
                                    screenY,
                                    content.MeasuredWidth,
                                    content.MeasuredHeight
                                );
                                switch (content.contentType)
                                {
                                    case GUIShowContentType.Label:
                                    {
                                        GUI.Label(rect, content.labelContent, content.MeasuredLabelStyle);
                                    }
                                        break;
                                    case GUIShowContentType.Texture:
                                    {
                                        GUI.DrawTexture(rect, content.textureContent);
                                    }
                                        break;
                                }

                                screenY -= content.height;
                            }
                        }
                            break;
                    }
                }
                    break;
            }
        }

        private void HandleAllowGUIShow()
        {
            AllowGUIShow = true;
        }

        private void HandleBanGUIShow()
        {
            AllowGUIShow = false;
        }
    }
}