using System;
using Rendering;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interact
{
    [RequireComponent(typeof(Collider))]
    public abstract class InteractableObject : MonoBehaviour, IInteractable
    {
        [ReadOnly] public bool visible = false;

        [SerializeField] private Collider interactableCollider;

        public abstract bool AllowInteract(GameObject target);

        public abstract void Interact(GameObject target);

        public virtual string Tip(GameObject target) => "交互";

        private void OnValidate()
        {
            if (interactableCollider == null)
            {
                interactableCollider = GetComponent<Collider>();
            }
        }
    }
}