using Character;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace CollideDetection.Shape
{
    public abstract class BaseCollideDetectionShapeObject : MonoBehaviour
    {
        [ReadOnly] public Transform center;
        [ReadOnly] public Vector3 localPosition = Vector3.zero;
        [ReadOnly] public Quaternion localRotation = Quaternion.identity;
        [ReadOnly] public bool @fixed;
        public bool debug = false;

        private Vector3? _position = null;

        public Vector3 Position
        {
            get
            {
                if (@fixed)
                {
                    if (_position.HasValue) return _position.Value;
                    _position = center.TransformPoint(this.localPosition);
                    return _position.Value;
                }

                _position = center.TransformPoint(this.localPosition);
                return _position.Value;
            }
        }

        private Quaternion? _rotation = null;

        public Quaternion Rotation
        {
            get
            {
                if (@fixed)
                {
                    if (_rotation.HasValue) return _rotation.Value;
                    _rotation = center.rotation * localRotation;
                    return _rotation.Value;
                }

                _rotation = center.rotation * localRotation;
                return _rotation.Value;
            }
        }

        public abstract void Detect(System.Action<Collider> detectionDelegate, int layerMask = -1);

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && debug)
            {
                DrawGizmosInternal();
            }
#endif
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmosInternal();
        }

        protected abstract void DrawGizmosInternal();
    }
}