using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Appearance
{
    public class AppearanceWindowController : MonoBehaviour
    {
        [Title("动画配置")] [SerializeField] private RectTransform startPosition;
        [SerializeField] private RectTransform endPosition;
        [SerializeField] private float animationDuration = 1f;

        public bool Showing { private set; get; }

        public void Show()
        {
            if (Showing)
            {
                return;
            }

            gameObject.SetActive(true);
            Showing = true;

            var position = transform.position;
            position.x = startPosition.transform.position.x;
            transform.position = position;
            transform.DOMoveX(endPosition.transform.position.x, animationDuration);
        }

        public void Hide()
        {
            if (!Showing)
            {
                return;
            }

            Showing = false;
            gameObject.SetActive(false);
        }

        [Button("移至起始位置")]
        private void SetWindowStartPosition()
        {
            var position = transform.position;
            position.x = startPosition.transform.position.x;
            transform.position = position;
        }

        [Button("移至终止位置")]
        private void SetWindowEndPosition()
        {
            var position = transform.position;
            position.x = endPosition.transform.position.x;
            transform.position = position;
        }
    }
}