using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Common
{
    public class DragBehaviour : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UnityEvent<Vector2> onDragging = new();

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            onDragging?.Invoke(eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}