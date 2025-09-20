using System;
using Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CollideDetection.Shape
{
    public class CollideDetectionShapeSphereObject : BaseCollideDetectionShapeObject
    {
        [SerializeField] private bool debugWire = false;
        [ReadOnly, ShowInInspector] private float radius = 1f;

        public void SetParams(Transform center, Vector3 localPosition, float radius, bool @fixed)
        {
            this.center = center;
            this.localPosition = localPosition;
            this.radius = radius;
            this.@fixed = @fixed;
        }

        public override void Detect(Action<Collider> detectionDelegate, int layerMask = -1)
        {
            var results = Physics.OverlapSphere(Position, radius, layerMask);
            foreach (var result in results)
            {
                if (result)
                {
                    detectionDelegate?.Invoke(result);
                }
            }
        }

        protected override void DrawGizmosInternal()
        {
            Gizmos.color = Color.yellow;
            if (debugWire)
            {
                Gizmos.DrawWireSphere(Position, radius);
            }
            else
            {
                Gizmos.DrawSphere(Position, radius);
            }
        }
    }
}