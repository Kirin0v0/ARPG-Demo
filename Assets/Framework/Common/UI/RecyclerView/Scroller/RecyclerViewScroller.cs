using System;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Framework.Common.UI.RecyclerView.Scroller
{
    public abstract class RecyclerViewScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler,
        IScrollHandler
    {
        public event UnityAction<float> PositionChangedEvent;
        public event UnityAction<bool> ScrollEvent;

        private bool _scrolling = false;

        public bool Scrolling
        {
            get => _scrolling;
            protected set
            {
                _scrolling = value;
                ScrollEvent?.Invoke(value);
            }
        }

        private float _position = 0f;

        public float Position
        {
            get => _position;
            protected set
            {
                if (Mathf.Approximately(_position, value))
                {
                    return;
                }

                _position = value;
                PositionChangedEvent?.Invoke(value);
            }
        }

        private float _minPosition = 0f;

        public float MinPosition
        {
            get => _minPosition;
            set
            {
                if (Mathf.Approximately(_minPosition, value))
                {
                    return;
                }

                _minPosition = value;
                ScrollToPosition(Position);
            }
        }

        private float _maxPosition = 0f;

        public float MaxPosition
        {
            get => _maxPosition;
            set
            {
                if (Mathf.Approximately(_maxPosition, value))
                {
                    return;
                }

                _maxPosition = value;
                ScrollToPosition(Position);
            }
        }

        public bool Snap { get; set; } = false;

        public float Velocity { get; protected set; } = 0f;
        public float DragSpeed { get; set; } = 0f;
        public float WheelSpeed { get; set; } = 0f;

        public abstract void OnBeginDrag(PointerEventData eventData);

        public abstract void OnDrag(PointerEventData eventData);

        public abstract void OnEndDrag(PointerEventData eventData);

        public abstract void OnScroll(PointerEventData eventData);

        public abstract void ScrollToPosition(float position);

        public abstract void SmoothScrollToPosition(float position, float speed);

        public abstract void StopSmoothScroll();
    }
}