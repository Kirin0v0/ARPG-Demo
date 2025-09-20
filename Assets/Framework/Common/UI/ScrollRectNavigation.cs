using System;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectNavigation : MonoBehaviour
    {
        [SerializeField] private bool autoNavigateWhenSelectChild = true; // 是否在选中子级时自动滚动
        [SerializeField] private float navigateSpacing = 10f; // 滚动间距，滚动后与子级相差的间隔（注意，目前存在navigateSpacing不为0时无法触发按钮点击事件的bug，排查不到问题所在）

        private ScrollRect _scrollRect;

        private GameObject _selectedGameObject;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void LateUpdate()
        {
            var selectedGameObject = EventSystem.current?.currentSelectedGameObject;
            if (!selectedGameObject)
            {
                _selectedGameObject = null;
                return;
            }

            // 如果选中了滚动视图的子级且设置了自动滚动，就进行滚动
            if (selectedGameObject != _selectedGameObject && autoNavigateWhenSelectChild &&
                IsDescendantOf(selectedGameObject.GetComponent<Transform>(), _scrollRect.content))
            {
                NavigateTo(selectedGameObject.GetComponent<RectTransform>());
            }

            _selectedGameObject = selectedGameObject;
        }

        public void NavigateTo(RectTransform target)
        {
            if (!target || !IsDescendantOf(target, _scrollRect.content))
            {
                return;
            }

            // 计算可滚动区域尺寸以及当前显示区域位置
            CalculateContentScrollAndDisplaySize(
                out var scrollableWidth,
                out var scrollableHeight,
                out var displayXMin,
                out var displayXMax,
                out var displayYMin,
                out var displayYMax
            );
            // 计算目标显示区域位置
            CalculateContentTargetOffsets(
                target,
                out var offsetXMin,
                out var offsetXMax,
                out var offsetYMin,
                out var offsetYMax
            );

            // DebugUtil.LogYellow($"Navigate to target: {target.name}");
            // DebugUtil.LogYellow($"scrollableWidth: {scrollableWidth}");
            // DebugUtil.LogYellow($"scrollableHeight: {scrollableHeight}");
            // DebugUtil.LogYellow($"displayXMin: {displayXMin}");
            // DebugUtil.LogYellow($"displayXMax: {displayXMax}");
            // DebugUtil.LogYellow($"displayYMin: {displayYMin}");
            // DebugUtil.LogYellow($"displayYMax: {displayYMax}");
            // DebugUtil.LogYellow($"offsetXMin: {offsetXMin}");
            // DebugUtil.LogYellow($"offsetXMax: {offsetXMax}");
            // DebugUtil.LogYellow($"offsetYMin: {offsetYMin}");
            // DebugUtil.LogYellow($"offsetYMax: {offsetYMax}");

            var targetPosition = _scrollRect.normalizedPosition;

            // 先判断是否在X轴上进行滚动，根据是否处于显示区域内进行判断
            if (offsetXMin >= displayXMin && offsetXMax <= displayXMax) // 如果处于显示区域内，就不进行滚动
            {
                targetPosition = new Vector2(targetPosition.x, targetPosition.y);
            }
            else if (offsetXMax < displayXMin || offsetXMin > displayXMax) // 如果完全处于显示区域外，则进行滚动
            {
                // 如果处于显示区域左侧，就滚动到目标最左侧
                if (offsetXMax < displayXMin)
                {
                    targetPosition = new Vector2(
                        (offsetXMin - displayXMin) / scrollableWidth + targetPosition.x,
                        targetPosition.y
                    );
                }

                // 如果处于显示区域右侧，就滚动到目标最右侧
                if (offsetXMin > displayXMax)
                {
                    targetPosition = new Vector2(
                        (offsetXMax - displayXMax) / scrollableWidth + targetPosition.x,
                        targetPosition.y
                    );
                }
            }
            else // 如果部分处于显示区域，也进行滚动
            {
                var targetWidth = offsetXMax - offsetXMin;
                var displayWidth = displayXMax - displayXMin;
                if (targetWidth > displayWidth) // 如果目标宽度大于显示区域宽度，就直接滚动到居中位置
                {
                    targetPosition = new Vector2(
                        (targetWidth / 2 - (displayXMin + displayXMax) / 2) / scrollableWidth + targetPosition.x,
                        targetPosition.y
                    );
                }
                else // 否则就根据当前未展示部分的方向滚动
                {
                    // 如果左侧不在显示区域，就滚动到目标最左侧
                    if (displayXMin > offsetXMin)
                    {
                        targetPosition = new Vector2(
                            (offsetXMin - displayXMin) / scrollableWidth + targetPosition.x,
                            targetPosition.y
                        );
                    }

                    // 如果右侧不在显示区域，就滚动到目标最右侧
                    if (displayXMax < offsetXMax)
                    {
                        targetPosition = new Vector2(
                            (offsetXMax - displayXMax) / scrollableWidth + targetPosition.x,
                            targetPosition.y
                        );
                    }
                }
            }

            // 再判断是否在Y轴上进行滚动，根据是否处于显示区域内进行判断
            if (offsetYMin >= displayYMin && offsetYMax <= displayYMax) // 如果处于显示区域内，就不进行滚动
            {
                targetPosition = new Vector2(targetPosition.x, targetPosition.y);
            }
            else if (offsetYMax < displayYMin || offsetYMin > displayYMax) // 如果完全处于显示区域外，则进行滚动
            {
                // 如果处于显示区域下侧，就滚动到目标最下侧
                if (offsetYMax < displayYMin)
                {
                    targetPosition = new Vector2(
                        targetPosition.x,
                        (offsetYMin - displayYMin) / scrollableHeight + targetPosition.y
                    );
                }

                // 如果处于显示区域上侧，就滚动到目标最上侧
                if (offsetYMin > displayYMax)
                {
                    targetPosition = new Vector2(
                        targetPosition.x,
                        (offsetYMax - displayYMax) / scrollableHeight + targetPosition.y
                    );
                }
            }
            else // 如果部分处于显示区域，也进行滚动
            {
                var targetHeight = offsetYMax - offsetYMin;
                var displayHeight = displayYMax - displayYMin;
                if (targetHeight > displayHeight) // 如果目标宽度大于显示区域宽度，就直接滚动到居中位置
                {
                    targetPosition = new Vector2(
                        targetPosition.x,
                        ((offsetYMin + offsetYMax) / 2 - (displayYMin + displayYMax) / 2) / scrollableHeight +
                        targetPosition.y
                    );
                }
                else // 否则就根据当前未展示部分的方向滚动
                {
                    // 如果下侧不在显示区域，就滚动到目标最下侧
                    if (offsetYMin < displayYMin)
                    {
                        targetPosition = new Vector2(
                            targetPosition.x,
                            (offsetYMin - displayYMin) / scrollableHeight + targetPosition.y
                        );
                    }

                    // 如果上侧不在显示区域，就滚动到目标最上侧
                    if (offsetYMax > displayYMax)
                    {
                        targetPosition = new Vector2(
                            targetPosition.x,
                            (offsetYMax - displayYMax) / scrollableHeight + targetPosition.y
                        );
                    }
                }
            }

            // DebugUtil.LogYellow($"targetPosition: {targetPosition}");
            _scrollRect.normalizedPosition = targetPosition;
        }

        /// <summary>
        /// 判断目标transform是否是指定父transform的子级或更深层级的后代
        /// </summary>
        private bool IsDescendantOf(Transform target, Transform parent)
        {
            if (target == null || parent == null)
                return false;

            var current = target.parent;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// 计算Scroll内容布局的滚动和显示区域大小
        /// </summary>
        /// <param name="scrollableWidth"></param>
        /// <param name="scrollableHeight"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        private void CalculateContentScrollAndDisplaySize(out float scrollableWidth, out float scrollableHeight, out float xMin,
            out float xMax, out float yMin, out float yMax)
        {
            var viewport = _scrollRect.viewport;
            var content = _scrollRect.content;
            // 获取实际屏幕尺寸（考虑CanvasScaler缩放）
            var viewportWidth = viewport.rect.width * viewport.lossyScale.x;
            var viewportHeight = viewport.rect.height * viewport.lossyScale.y;
            var contentWidth = content.rect.width * content.lossyScale.x;
            var contentHeight = content.rect.height * content.lossyScale.y;
            // 计算可滚动区域
            scrollableWidth = Mathf.Max(contentWidth - viewportWidth, 0f);
            scrollableHeight = Mathf.Max(contentHeight - viewportHeight, 0f);
            // 计算显示区域位置
            xMin = scrollableWidth * _scrollRect.horizontalNormalizedPosition;
            xMax = viewportWidth + scrollableWidth * _scrollRect.horizontalNormalizedPosition;
            yMin = scrollableHeight * _scrollRect.verticalNormalizedPosition;
            yMax = viewportHeight + scrollableHeight * _scrollRect.verticalNormalizedPosition;
        }

        /// <summary>
        /// 计算目标与ScrollRect内容布局的边界差值
        /// </summary>
        /// <param name="target"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        private void CalculateContentTargetOffsets(RectTransform target, out float xMin, out float xMax, out float yMin,
            out float yMax)
        {
            var targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            var contentCorners = new Vector3[4];
            _scrollRect.content.GetWorldCorners(contentCorners);
            var spacingX = navigateSpacing * _scrollRect.content.lossyScale.x;
            var spacingY = navigateSpacing * _scrollRect.content.lossyScale.y;
            xMin = Mathf.Max(targetCorners[0].x - contentCorners[0].x - spacingX, 0f);
            xMax = Mathf.Min(targetCorners[2].x - contentCorners[0].x + spacingX, contentCorners[2].x - contentCorners[0].x);
            yMin = Mathf.Max(targetCorners[0].y - contentCorners[0].y - spacingY, 0f);
            yMax = Mathf.Min(targetCorners[1].y - contentCorners[0].y + spacingY, contentCorners[1].y - contentCorners[0].y);
        }
    }
}