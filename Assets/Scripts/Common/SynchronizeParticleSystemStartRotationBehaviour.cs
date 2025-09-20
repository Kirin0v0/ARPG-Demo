using System;
using Framework.Common.Debug;
using UnityEngine;

namespace Common
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SynchronizeParticleSystemStartRotationBehaviour : MonoBehaviour
    {
        [SerializeField] private RotationAxis axis;

        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            SynchronizeStartRotation();
        }

        private void Update()
        {
            SynchronizeStartRotation();
        }

        private void SynchronizeStartRotation()
        {
            var rotation = axis switch
            {
                RotationAxis.Y => transform.rotation.eulerAngles.y * Mathf.Deg2Rad,
                _ => _particleSystem.startRotation
            };
            _particleSystem.startRotation = rotation;
        }

        private enum RotationAxis
        {
            Y
        }
    }
}