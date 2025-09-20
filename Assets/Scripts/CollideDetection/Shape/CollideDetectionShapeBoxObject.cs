using System;
using Character;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CollideDetection.Shape
{
    public class CollideDetectionShapeBoxObject : BaseCollideDetectionShapeObject
    {
        [SerializeField] private bool debugWire = false;
        [ReadOnly, ShowInInspector] private Vector3 size = Vector3.one;

        public void SetParams(Transform center, Vector3 localPosition, Quaternion localRotation, Vector3 size, bool @fixed)
        {
            this.center = center;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.size = size;
            this.@fixed = @fixed;
        }

        public override void Detect(Action<Collider> detectionDelegate, int layerMask = -1)
        {
            var results = Physics.OverlapBox(Position, size / 2, Rotation, layerMask);
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
            Gizmos.color = Color.green;
            var matrix = new Matrix4x4();
            matrix.SetTRS(
                Position,
                Rotation,
                Vector3.one
            );
            Gizmos.matrix = matrix;
            if (debugWire)
            {
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, size);
            }
        }
    }
}