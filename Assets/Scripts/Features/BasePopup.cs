using System;
using UnityEngine;

namespace Features
{
    /// <summary>
    /// 弹窗基类，由于弹窗业务特性通常自身默认失活状态，因此展示和隐藏逻辑是设置活跃状态，而不是设置透明度
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public abstract class BasePopup<T> : MonoBehaviour
    {
        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (!_rectTransform)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        private CanvasGroup _canvasGroup;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (!_canvasGroup)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }

                return _canvasGroup;
            }
        }

        public Func<RectTransform, Vector2> AnchoredPositionGetter { private get; set; } =
            rectTransform => rectTransform.position;

        public Action<Vector2, RectTransform> PopupPositionSetter { private get; set; } =
            (anchoredPosition, popup) => { popup.transform.position = anchoredPosition; };

        private bool _showing = false;
        private bool _delayShow = false;
        private RectTransform _target = null;
        protected T Data { get; private set; } = default;

        private void OnEnable()
        {
            _showing = false;
            _delayShow = false;
            CanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!_showing)
            {
                if (_delayShow)
                {
                    _showing = true;
                    _delayShow = false;
                }

                CanvasGroup.alpha = 0f;
                return;
            }

            _showing = true;
            CanvasGroup.alpha = 1f;
            UpdateContent();
            UpdateAnchoredPosition();
        }

        private void OnDisable()
        {
            _showing = false;
            _delayShow = false;
            CanvasGroup.alpha = 0f;
        }

        public void Show(RectTransform target, T data)
        {
            _target = target;
            Data = data;
            _delayShow = true;
            gameObject.SetActive(true);
            UpdateContent();
        }

        public void Hide()
        {
            _target = null;
            Data = default;
            _showing = false;
            gameObject.SetActive(false);
            UpdateContent();
        }

        protected abstract void UpdateContent();

        private void UpdateAnchoredPosition()
        {
            if (!_target)
            {
                return;
            }

            // 获取锚点位置并更新弹窗位置
            var anchoredPosition = AnchoredPositionGetter(_target);
            PopupPositionSetter(anchoredPosition, RectTransform);
        }
    }
}