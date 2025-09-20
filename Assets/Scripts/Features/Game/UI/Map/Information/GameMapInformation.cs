using System;
using UnityEngine;

namespace Features.Game.UI.Map.Information
{
    [RequireComponent(typeof(RectTransform))]
    public class GameMapInformation: MonoBehaviour
    {
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            _rectTransform = null;
        }

        public void SetPosition(Vector3 position)
        {
            _rectTransform.position = position;
        }

        public void SetAnchoredPosition(Vector3 anchoredPosition)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}