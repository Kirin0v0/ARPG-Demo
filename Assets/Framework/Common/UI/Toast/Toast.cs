using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Framework.Core.Singleton;
using UnityEngine;

namespace Framework.Common.UI.Toast
{
    public class Toast : MonoSingleton<Toast>
    {
        [SerializeField] private Vector2 referencedResolution = new Vector2(1920, 1080);
        [SerializeField] private Vector2 paddingHorizontal = new Vector2(20f, 20f);
        [SerializeField] private Vector2 paddingVertical = new Vector2(10f, 10f);

        public class Builder
        {
            private readonly ToastData _data;

            public Builder SetText(string text)
            {
                _data.Text = text;
                return this;
            }

            public Builder SetTextColor(Color textColor)
            {
                _data.TextColor = textColor;
                return this;
            }

            public Builder SetTextSize(int textSize)
            {
                _data.TextSize = textSize;
                return this;
            }

            public Builder SetDuration(float duration)
            {
                _data.Duration = duration;
                return this;
            }

            public Builder SetLocation(ToastLocation location)
            {
                _data.Location = location;
                return this;
            }

            public Builder SetCustomLocation(Vector2 customCenter)
            {
                _data.CustomCenter = customCenter;
                return this;
            }

            public void CreateAndShow()
            {
                Toast.Instance.Show(_data);
            }
        }

        private Vector2 ScaleFactor =>
            new(Screen.width / referencedResolution.x, Screen.height / referencedResolution.y);
        private Vector2 PaddingHorizontal => paddingHorizontal * ScaleFactor;
        private Vector2 PaddingVertical => paddingVertical * ScaleFactor;

        private ToastData _showToastData;

        public void Show(string text, float duration = 2f, bool realTime = true,
            ToastLocation location = ToastLocation.Bottom)
        {
            Show(new ToastData
            {
                Text = text,
                TextSize = 36,
                TextColor = Color.white,
                Duration = duration,
                RealTime = realTime,
                Location = location,
            });
        }

        public void Show(ToastData toastData)
        {
            StopAllCoroutines();
            _showToastData = toastData;
            StartCoroutine(Hide(toastData.Duration, toastData.RealTime));
        }

        public void Hide()
        {
            _showToastData = null;
        }

        private void OnGUI()
        {
            if (_showToastData == null)
            {
                return;
            }

            var paddingHorizontal = PaddingHorizontal;
            var paddingVertical = PaddingVertical;

            // 文字分行
            var text = SplitTextIntoLines(_showToastData.Text, 20);

            // 先设置文字参数再计算文字尺寸
            var textStyle = new GUIStyle
            {
                fontSize = (int)(_showToastData.TextSize * Mathf.Min(ScaleFactor.x, ScaleFactor.y)),
                normal =
                {
                    textColor = _showToastData.TextColor
                }
            };
            var textRectSize = textStyle.CalcSize(new GUIContent(text));

            // 计算起始位置
            CalculateStartPosition(textRectSize, out var startX, out var startY);
            GUI.Box(
                new Rect(
                    startX,
                    startY,
                    textRectSize.x + paddingHorizontal.x + paddingHorizontal.y,
                    textRectSize.y + paddingVertical.x + paddingVertical.y
                ),
                "",
                new GUIStyle
                {
                    normal = new GUIStyleState
                    {
                        background = Texture2D.grayTexture,
                    },
                });
            GUI.Label(
                new Rect(
                    startX + paddingHorizontal.x,
                    startY + paddingVertical.x,
                    textRectSize.x,
                    textRectSize.y
                ),
                text,
                textStyle
            );
        }

        private IEnumerator Hide(float duration, bool realTime)
        {
            if (realTime)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            _showToastData = null;
        }

        private string SplitTextIntoLines(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var stringBuilder = new StringBuilder();
            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int endIndex = Mathf.Min(startIndex + maxCharsPerLine, text.Length);
                stringBuilder.Append(text.Substring(startIndex, endIndex - startIndex));
                startIndex = endIndex;
            }

            return stringBuilder.ToString();
        }

        private void CalculateStartPosition(Vector2 textRectSize, out float startX, out float startY)
        {
            var paddingHorizontal = PaddingHorizontal;
            var paddingVertical = PaddingVertical;
            switch (_showToastData.Location)
            {
                case ToastLocation.Top:
                {
                    startX = Screen.width / 2f - textRectSize.x / 2f -
                             (paddingHorizontal.x + paddingHorizontal.y) / 2f;
                    startY = Screen.height * 0.15f;
                }
                    break;
                case ToastLocation.Center:
                {
                    startX = Screen.width / 2f - textRectSize.x / 2f -
                             (paddingHorizontal.x + paddingHorizontal.y) / 2f;
                    startY = Screen.height / 2f - textRectSize.y / 2f - (paddingVertical.x + paddingVertical.y) / 2f;
                }
                    break;
                case ToastLocation.Bottom:
                {
                    startX = Screen.width / 2f - textRectSize.x / 2f -
                             (paddingHorizontal.x + paddingHorizontal.y) / 2f;
                    startY = Screen.height * 0.85f - textRectSize.y - (paddingVertical.x + paddingVertical.y);
                }
                    break;
                case ToastLocation.Custom:
                {
                    startX = _showToastData.CustomCenter.x - textRectSize.x / 2f -
                             (paddingHorizontal.x + paddingHorizontal.y) / 2f;
                    startY = _showToastData.CustomCenter.y - textRectSize.y / 2f -
                             (paddingVertical.x + paddingVertical.y) / 2f;
                }
                    break;
                default:
                {
                    startX = 0f;
                    startY = 0f;
                }
                    break;
            }
        }
    }
}