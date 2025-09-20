using System.Collections.Generic;
using Framework.Common.Trigger.Chain;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Framework.Common.Trigger
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class TriggerZone : MonoBehaviour
    {
        [FormerlySerializedAs("triggerChains")] [Header("触发链模板列表")] [SerializeField]
        private List<BaseTriggerChain> triggerChainTemplates = new();

        [Header("默认触发回调")] public UnityEvent<Collider> onEnterTriggerZone = new();
        public UnityEvent<Collider> onStayTriggerZone = new();
        public UnityEvent<Collider> onExitTriggerZone = new();

        private readonly HashSet<Collider> _triggerColliders = new();

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider)
            {
                collider.isTrigger = true;
            }

            _triggerColliders.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            _triggerColliders.Add(other);
            triggerChainTemplates.ForEach(triggerChain => triggerChain?.Begin(other));
            onEnterTriggerZone?.Invoke(other);
        }

        private void OnTriggerStay(UnityEngine.Collider other)
        {
            triggerChainTemplates.ForEach(triggerChain => triggerChain?.Begin(other));
            onStayTriggerZone?.Invoke(other);
        }

        private void OnTriggerExit(UnityEngine.Collider other)
        {
            _triggerColliders.Remove(other);
            triggerChainTemplates.ForEach(triggerChain => triggerChain?.Finish(other));
            onExitTriggerZone?.Invoke(other);
        }

        private void OnDestroy()
        {
            foreach (var triggerCollider in _triggerColliders)
            {
                triggerChainTemplates.ForEach(triggerChain => triggerChain?.Finish(triggerCollider));
                onExitTriggerZone?.Invoke(triggerCollider);
            }

            _triggerColliders.Clear();
        }

        private void OnValidate()
        {
            var collider = GetComponent<UnityEngine.Collider>();
            if (collider)
            {
                collider.isTrigger = true;
            }
        }
    }
}