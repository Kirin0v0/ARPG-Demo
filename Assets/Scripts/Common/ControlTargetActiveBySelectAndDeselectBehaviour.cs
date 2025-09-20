using UnityEngine;
using UnityEngine.EventSystems;

namespace Common
{
    public class ControlTargetActiveBySelectAndDeselectBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject target;

        public void OnSelect(BaseEventData eventData)
        {
            target.SetActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            target.SetActive(false);
        }
    }
}