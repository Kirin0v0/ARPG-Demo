using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.Util
{
    public static class UGUIUtil
    {
        public static bool IsWidthStretched(RectTransform rectTransform)
        {
            return !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x);
        }

        public static bool IsHeightStretched(RectTransform rectTransform)
        {
            return !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
        }

        /// <summary>
        /// 解决UGUI布局在当前帧刷新的问题，直接使用LayoutRebuilder.ForceRebuildLayoutImmediate仅刷新自身，不会刷新子节点
        /// </summary>
        /// <param name="root"></param>
        public static void RefreshLayoutGroupsImmediateAndRecursive(GameObject root)
        {
            // 优先刷新子节点布局
            var componentsInChildren = root.GetComponentsInChildren<LayoutGroup>(true);
            foreach (var layoutGroup in componentsInChildren)
            {
                if (layoutGroup.gameObject == root)
                {
                    continue;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            }

            // 最后刷新自身布局
            var parent = root.GetComponent<LayoutGroup>();
            if (parent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
            }
        }

        public static bool IsTargetDerivedFromRoot(Transform target, Transform root)
        {
            if (!target)
            {
                return false;
            }

            var parent = target.transform;
            while (parent != null)
            {
                if (parent == root)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        public static void DeselectIfSelectedInTargetChildren(EventSystem eventSystem, Transform target)
        {
            if (!eventSystem || !eventSystem.currentSelectedGameObject)
            {
                return;
            }

            if (IsTargetDerivedFromRoot(eventSystem.currentSelectedGameObject.transform, target))
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }
    }
}