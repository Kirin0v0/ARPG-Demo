using System;
using Framework.Common.Debug;
using Framework.Common.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Common.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeSynchronizer : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private bool width = true;
        [SerializeField] private bool height = true;
        [SerializeField] private RectOffset padding;

        private RectTransform _rectTransform;

        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        private void LateUpdate()
        {
            Synchronize();
        }

        public void CalculateSize()
        {
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(target.gameObject);
            Synchronize();
        }

        private void OnValidate()
        {
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(target.gameObject);
            Synchronize();
        }

        private void Synchronize()
        {
            var size = RectTransform.sizeDelta;
            if (width && target)
            {
                size.x = target.sizeDelta.x + padding.horizontal;
            }

            if (height && target)
            {
                size.y = target.sizeDelta.y + padding.vertical;
            }

            RectTransform.sizeDelta = size;
        }
    }
}