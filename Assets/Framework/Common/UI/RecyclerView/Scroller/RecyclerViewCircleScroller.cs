using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Common.UI.RecyclerView.Scroller
{
    public class RecyclerViewCircleScroller : RecyclerViewScroller
    {
        public Vector2 Center { get; set; } = Vector2.zero;
        public float Interval { get; set; } = 0f;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            StopSmoothScroll();
            Scrolling = true;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.position.x >= Center.x)
            {
                Velocity = eventData.delta.y * DragSpeed / Time.deltaTime;
            }
            else
            {
                Velocity = -eventData.delta.y * DragSpeed / Time.deltaTime;
            }

            if (Snap)
            {
                Position = Mathf.Clamp(Position + Time.deltaTime * Velocity, MinPosition, MaxPosition);
            }
            else
            {
                Position += Time.deltaTime * Velocity;
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            InertiaAndElastic();
        }

        public override void OnScroll(PointerEventData eventData)
        {
            StopSmoothScroll();
            Velocity = eventData.scrollDelta.y * WheelSpeed / Time.deltaTime;
            if (Snap)
            {
                Position = Mathf.Clamp(Position + Time.deltaTime * Velocity, MinPosition, MaxPosition);
            }
            else
            {
                Position += Time.deltaTime * Velocity;
            }

            InertiaAndElastic();
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

        private void InertiaAndElastic()
        {
            if (Snap)
            {
                Elastic();
            }
            else
            {
                Inertia();
            }
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
            var duration = 0.5f;
            while (timer < duration)
            {
                Velocity = Mathf.Lerp(Velocity, 0, Time.deltaTime / duration);
                Position += Velocity * Time.deltaTime;
                timer += Time.deltaTime;

                yield return new WaitForEndOfFrame();
            }

            Scrolling = false;
        }

        /// <summary>
        /// 弹性滑动，用于使滚动复位的平滑滑动
        /// </summary>
        private void Elastic()
        {
            StopAllCoroutines();
            var count = Mathf.FloorToInt(Position / Interval);
            var position1 = count * Interval;
            var position2 = (count + 1) * Interval;
            var target = Mathf.Abs(position1 - Position) <= Mathf.Abs(position2 - Position) ? position1 : position2;
            StartCoroutine(ElasticInternal(target));
        }

        private IEnumerator ElasticInternal(float position)
        {
            yield return new WaitForEndOfFrame();
            var duration = 0.5f;
            yield return MoveInternal(position, Mathf.Abs((position - Position) / duration));
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
                while (position - Position > 0)
                {
                    Velocity = speed;
                    Position += Velocity * Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }

                Velocity = 0f;
                Position = position;
            }
            else
            {
                while (position - Position < 0)
                {
                    Velocity = speed;
                    Position += Velocity * Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }

                Velocity = 0f;
                Position = position;
            }
        }
    }
}