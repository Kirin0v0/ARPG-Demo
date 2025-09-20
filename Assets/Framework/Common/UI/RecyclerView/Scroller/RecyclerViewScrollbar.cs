using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView.Scroller
{
    public class RecyclerViewScrollbar : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Scrollbar scrollbar;
        public Scrollbar Scrollbar => scrollbar;

        [SerializeField] private float scale = 1.2f;
        
        private RectTransform _handle;
        private bool _dragging;
        private bool _hovering;

        public RectTransform RectTransform => GetComponent<RectTransform>();

        private void Awake()
        {
            _handle = scrollbar.handleRect;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            if (!_hovering)
            {
                if (scrollbar.direction == Scrollbar.Direction.TopToBottom ||
                    scrollbar.direction == Scrollbar.Direction.BottomToTop)
                {
                    ScaleX(_handle, _handle.localScale.x, 1f, 0.2f);
                }
                else
                {
                    ScaleY(_handle, _handle.localScale.y, 1f, 0.2f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            if (scrollbar.direction == Scrollbar.Direction.TopToBottom ||
                scrollbar.direction == Scrollbar.Direction.BottomToTop)
            {
                ScaleX(_handle, _handle.localScale.x, scale, 0.2f);
            }
            else
            {
                ScaleY(_handle, _handle.localScale.y, scale, 0.2f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (!_dragging)
            {
                if (scrollbar.direction == Scrollbar.Direction.TopToBottom ||
                    scrollbar.direction == Scrollbar.Direction.BottomToTop)
                {
                    ScaleX(_handle, _handle.localScale.x, 1f, 0.2f);
                }
                else
                {
                    ScaleY(_handle, _handle.localScale.y, 1f, 0.2f);
                }
            }
        }

        private void OnValidate()
        {
            if (!scrollbar)
            {
                scrollbar = gameObject.GetComponent<Scrollbar>();
                if (!scrollbar)
                {
                    scrollbar = gameObject.AddComponent<Scrollbar>();
                }
            }
        }

        private void ScaleX(RectTransform target, float from, float to, float time)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleXCoroutine(target, from, to, time));
        }
        
        private void ScaleY(RectTransform target, float from, float to, float time)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleYCoroutine(target, from, to, time));
        }
        
        private IEnumerator ScaleXCoroutine(RectTransform target, float from, float to, float duration)
        {
            var scale = target.localScale;
            var elapsed = 0f;
    
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var value = Mathf.Lerp(from, to, elapsed / duration);
                scale.x = value;
                target.localScale = scale;
                yield return null;
            }
    
            scale.x = to;
            target.localScale = scale;
        }

        private IEnumerator ScaleYCoroutine(RectTransform target, float from, float to, float duration)
        {
            var scale = target.localScale;
            var elapsed = 0f;
    
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var value = Mathf.Lerp(from, to, elapsed / duration);
                scale.y = value;
                target.localScale = scale;
                yield return null;
            }
    
            scale.y = to;
            target.localScale = scale;
        }
    }
}
