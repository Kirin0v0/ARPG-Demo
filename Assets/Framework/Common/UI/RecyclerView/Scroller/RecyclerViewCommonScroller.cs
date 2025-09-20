using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Common.UI.RecyclerView.Scroller
{
    public class RecyclerViewCommonScroller : RecyclerViewScroller
    {
        public enum Orientation
        {
            Horizontal,
            Vertical,
        }

        public Orientation ScrollOrientation { get; set; }

        public float ViewportSize { get; set; }

        private RecyclerViewScroller _dispatchDragScroller;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (ScrollOrientation != GetDragOrientation())
            {
                _dispatchDragScroller =
                    gameObject.transform.parent.gameObject.GetComponentInParent<RecyclerViewScroller>();
                if (_dispatchDragScroller)
                {
                    _dispatchDragScroller.OnBeginDrag(eventData);
                }

                return;
            }

            _dispatchDragScroller = null;
            StopSmoothScroll();
            Scrolling = true;
            
            return;

            Orientation GetDragOrientation()
            {
                if (Mathf.Abs(eventData.delta.x) >= Mathf.Abs(eventData.delta.y))
                {
                    return Orientation.Horizontal;
                }
                else
                {
                    return Orientation.Vertical;
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_dispatchDragScroller)
            {
                _dispatchDragScroller.OnDrag(eventData);
                return;
            }

            Velocity = ScrollOrientation switch
            {
                Orientation.Horizontal => -eventData.delta.x,
                Orientation.Vertical => eventData.delta.y,
            } * DragSpeed * GetScrollDamping() / Time.unscaledDeltaTime;

            if (Snap)
            {
                Position = Mathf.Clamp(Position + Time.unscaledDeltaTime * Velocity, MinPosition, MaxPosition);
            }
            else
            {
                Position += Time.unscaledDeltaTime * Velocity;
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_dispatchDragScroller)
            {
                _dispatchDragScroller.OnEndDrag(eventData);
                return;
            }

            Inertia();
        }

        public override void OnScroll(PointerEventData eventData)
        {
            StopSmoothScroll();
            Velocity = -eventData.scrollDelta.y * WheelSpeed * GetScrollDamping() / Time.unscaledDeltaTime;
            if (Snap)
            {
                Position = Mathf.Clamp(Position + Time.unscaledDeltaTime * Velocity, MinPosition, MaxPosition);
            }
            else
            {
                Position += Time.unscaledDeltaTime * Velocity;
            }

            Inertia();
        }

        public override void ScrollToPosition(float position)
        {
            StopSmoothScroll();
            Scrolling = true;
            Position = Mathf.Clamp(position, MinPosition, MaxPosition);
            Scrolling = false;
        }

        public override void SmoothScrollToPosition(float position, float speed)
        {
            Scrolling = true;
            position = Mathf.Clamp(position, MinPosition, MaxPosition);
            StartCoroutine(SmoothScrollInternal(position, speed));
        }

        public override void StopSmoothScroll()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// 惯性滑动，用于拖动事件后的平滑减速滑动
        /// </summary>
        private void Inertia()
        {
            StopAllCoroutines();
            StartCoroutine(InertiaInternal());
        }

        private IEnumerator InertiaInternal()
        {
            yield return new WaitForEndOfFrame();
            var timer = 0f;
            var duration = Snap ? 0f : 0.5f;
            while (timer < duration)
            {
                Velocity = Mathf.Lerp(Velocity, 0, Time.unscaledDeltaTime / duration);
                Position += Velocity * Time.unscaledDeltaTime;
                timer += Time.unscaledDeltaTime;

                Elastic();

                yield return new WaitForEndOfFrame();
            }

            Scrolling = false;
        }

        /// <summary>
        /// 弹性滑动，用于超出边界后的平滑滑动
        /// </summary>
        private void Elastic()
        {
            if (Position < MinPosition)
            {
                StopAllCoroutines();
                StartCoroutine(ElasticInternal(MinPosition));
            }
            else if (Position > MaxPosition)
            {
                StopAllCoroutines();
                StartCoroutine(ElasticInternal(MaxPosition));
            }
        }

        private IEnumerator ElasticInternal(float position)
        {
            if (!Snap)
            {
                yield return new WaitForEndOfFrame();
                var duration = 0.5f;
                yield return MoveInternal(position, Mathf.Abs((position - Position) / duration));
            }

            Scrolling = false;
        }

        private IEnumerator SmoothScrollInternal(float position, float speed)
        {
            yield return MoveInternal(position, speed);
            Scrolling = false;
        }

        private IEnumerator MoveInternal(float position, float speed)
        {
            var signature = position - Position >= 0 ? 1 : -1;
            speed = signature * Mathf.Abs(speed);
            if (signature > 0)
            {
                while (Position < position)
                {
                    Velocity = speed;
                    Position = Mathf.Min(Position + Velocity * Time.unscaledDeltaTime, position);
                    yield return new WaitForEndOfFrame();
                }

                Velocity = 0f;
            }
            else
            {
                while (Position > position)
                {
                    Velocity = speed;
                    Position = Mathf.Max(Position + Velocity * Time.unscaledDeltaTime, position);
                    yield return new WaitForEndOfFrame();
                }

                Velocity = 0f;
            }
        }

        private float GetScrollDamping()
        {
            var damping = 1f;
            if (Position < MinPosition)
            {
                damping = Mathf.Max(0, 1 - Mathf.Abs(Position - MinPosition) / ViewportSize);
            }
            else if (Position > MaxPosition)
            {
                damping = Mathf.Max(0, 1 - Mathf.Abs(Position - MaxPosition) / ViewportSize);
            }

            return damping;
        }
    }
}