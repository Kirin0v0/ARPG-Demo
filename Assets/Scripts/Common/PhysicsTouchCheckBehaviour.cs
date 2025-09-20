using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common
{
    public class PhysicsTouchCheckBehaviour: MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        public Vector3 Offset => offset;

        [SerializeField] private float radius;
        public float Radius => radius;

        [SerializeField] private LayerMask checkLayerMask;

        public bool InTouch { get; private set; }

        private Vector3 _defaultOffset;

        private void Awake()
        {
            _defaultOffset = offset;
        }

        private void Update()
        {
            CheckTouch();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + offset, radius);
        }

        public void ReverseOffset()
        {
            offset = new Vector3(-offset.x, offset.y, offset.z);
            CheckTouch();
        }

        public void SetExtraOffset(Vector3 extra)
        {
            offset = _defaultOffset + extra;
            CheckTouch();
        }

        private void CheckTouch()
        {
            InTouch = Physics.CheckSphere(transform.position + offset, radius, checkLayerMask);
        }
    }
}