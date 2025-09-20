using UnityEngine;

namespace CollideDetection.Bind
{
    public class CollideDetectionBindSelfObject: BaseCollideDetectionBindObject
    {
        private Collider _collider;
        
        protected override Collider Collider
        {
            get
            {
                if (_collider)
                {
                    return _collider;
                }

                if (TryGetComponent<Collider>(out _collider))
                {
                    return _collider;
                }

                _collider = gameObject.AddComponent<SphereCollider>();
                return _collider;
            }
        }
    }
}