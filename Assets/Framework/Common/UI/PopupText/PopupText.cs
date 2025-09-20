using Framework.Common.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Common.UI.PopupText
{
    public enum PopupTextPositionType
    {
        Screen,
        World,
    }
    
    public class PopupText : MonoBehaviour
    {
        private TextMeshProUGUI _textMeshProUGUI;
        private CanvasGroup _canvasGroup;

        private float _time;
        private bool _showing;

        private PopupTextAsset _asset;
        private PopupTextPositionType _positionType;
        private Vector3 _originPosition;
        private Vector3 _randomOffset;
        private bool _toRight;
        private System.Action _onHide;

        private void Awake()
        {
            _textMeshProUGUI = gameObject.GetComponent<TextMeshProUGUI>();
            if (!_textMeshProUGUI)
            {
                _textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
            }

            _textMeshProUGUI.alignment = TextAlignmentOptions.Center;

            var contentSizeFitter = gameObject.GetComponent<ContentSizeFitter>();
            if (!contentSizeFitter)
            {
                contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (!_canvasGroup)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Show(
            PopupTextAsset asset,
            string text,
            PopupTextPositionType positionType,
            Vector3 originPosition,
            Vector3 randomOffset,
            bool toRight,
            System.Action onHide
        )
        {
            // 设置数据
            _showing = true;
            _time = 0f;
            _asset = asset;
            _positionType = positionType;
            _textMeshProUGUI.text = text;
            _textMeshProUGUI.font = asset.font;
            _textMeshProUGUI.fontSize = asset.fontSize;
            _textMeshProUGUI.color = asset.fontColor;
            _originPosition = originPosition;
            _randomOffset = randomOffset;
            _toRight = toRight;
            _onHide = onHide;
            
            // 重置组件
            transform.localScale = Vector3.one;
            transform.position = _originPosition;
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            gameObject.transform.SetAsLastSibling();

            // 如果跳字位置类型是世界坐标系，则先朝向相机再旋转90度
            if (_positionType == PopupTextPositionType.World)
            {
                gameObject.transform.LookAt(UnityEngine.Camera.main.transform);
                gameObject.transform.Rotate(Vector3.up, 90, Space.Self);
            }

            UpdatePopupText();
        }

        private void Hide()
        {
            _showing = false;
            _time = 0f;
            _asset = null;
            _textMeshProUGUI.text = "";
            _originPosition = Vector3.zero;
            _randomOffset = Vector3.zero;
            _toRight = false;
            _onHide?.Invoke();
            _onHide = null;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_showing)
            {
                return;
            }

            _time += Time.deltaTime;

            if (!_asset || _time >= _asset.Duration)
            {
                Hide();
                return;
            }

            UpdatePopupText();
        }

        private void UpdatePopupText()
        {
            // 更新文字位置
            transform.position = _positionType switch
            {
                PopupTextPositionType.Screen => MathUtil.GetCanvasScreenPosition(UnityEngine.Camera.main,
                    _originPosition) + new Vector2(_randomOffset.x, _randomOffset.y),
                PopupTextPositionType.World => _originPosition + _randomOffset,
            };

            var popupTextTimePoints = _asset.Evaluate(_time);
            popupTextTimePoints.ForEach(timePoint =>
            {
                switch (timePoint.Type)
                {
                    case PopupTextCurveType.Scale:
                    {
                        transform.localScale = Vector3.one * (1f + timePoint.Value);
                    }
                        break;
                    case PopupTextCurveType.HorizontalMovement:
                    {
                        transform.position += _positionType switch
                        {
                            PopupTextPositionType.Screen => (_toRight ? Vector3.right : Vector3.left) * timePoint.Value,
                            PopupTextPositionType.World => (_toRight
                                ? new Vector3(UnityEngine.Camera.main.transform.right.x, 0,
                                    UnityEngine.Camera.main.transform.right.z)
                                : new Vector3(-UnityEngine.Camera.main.transform.right.x, 0,
                                    -UnityEngine.Camera.main.transform.right.z)) * timePoint.Value,
                        };
                    }
                        break;
                    case PopupTextCurveType.VerticalMovement:
                    {
                        transform.position += Vector3.up * timePoint.Value;
                    }
                        break;
                    case PopupTextCurveType.Alpha:
                    {
                        _canvasGroup.alpha = timePoint.Value;
                    }
                        break;
                }
            });
        }
    }
}