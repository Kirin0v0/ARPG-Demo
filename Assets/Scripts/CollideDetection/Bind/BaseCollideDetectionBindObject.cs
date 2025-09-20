using System;
using Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CollideDetection.Bind
{
    public abstract class BaseCollideDetectionBindObject : MonoBehaviour
    {
        protected abstract Collider Collider { get; }

        private Action<Collider> _detectionDelegate;
        private bool _defaultColliderEnable;

        public void Detect(Action<Collider> detectionDelegate)
        {
            _detectionDelegate = detectionDelegate;
            _defaultColliderEnable = Collider?.enabled ?? false;
            if (Collider != null) Collider.enabled = true;
        }

        public void CancelDetect()
        {
            _detectionDelegate = null;
            if (Collider != null) Collider.enabled = _defaultColliderEnable;
        }

        public bool IsDetecting() => Collider && Collider.enabled;

        private void OnCollisionEnter(Collision other)
        {
            _detectionDelegate?.Invoke(other.gameObject.GetComponent<Collider>());
        }

        private void OnCollisionStay(Collision other)
        {
            _detectionDelegate?.Invoke(other.gameObject.GetComponent<Collider>());
        }

        private void OnTriggerEnter(Collider other)
        {
            _detectionDelegate?.Invoke(other.gameObject.GetComponent<Collider>());
        }

        private void OnTriggerStay(Collider other)
        {
            _detectionDelegate?.Invoke(other.gameObject.GetComponent<Collider>());
        }
    }
}