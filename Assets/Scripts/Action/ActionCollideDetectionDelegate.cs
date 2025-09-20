using System;
using CollideDetection.Bind;
using CollideDetection.Shape;
using UnityEngine;

namespace Action
{
    public class ActionCollideDetectionDelegate
    {
        private readonly Transform _parent;
        private readonly GameObject _owner;
        private readonly ActionCollideDetectionBaseData _data;
        private readonly int _layerMask;

        private MonoBehaviour _collideDetectionObject;

        public ActionCollideDetectionDelegate(
            Transform parent,
            GameObject owner,
            ActionCollideDetectionBaseData data,
            int colliderLayerMask
        )
        {
            _parent = parent;
            _owner = owner;
            _data = data;
            _layerMask = colliderLayerMask;
        }

        public void Init(Action<Collider> detectionDelegate, bool debug)
        {
            switch (_data)
            {
                case ActionCollideDetectionBindData bindData:
                {
                    var bindObject = _owner.AddComponent<CollideDetectionBindSelfObject>();
                    bindObject.Detect(detectionDelegate);
                    _collideDetectionObject = bindObject;
                }
                    break;
                case ActionCollideDetectionBoxData boxData:
                {
                    var instance = new GameObject("Collide Detection Box")
                    {
                        transform =
                        {
                            parent = _parent,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var boxObject = instance.AddComponent<CollideDetectionShapeBoxObject>();
                    boxObject.SetParams(_owner.transform, boxData.localPosition, boxData.localRotation, boxData.size,
                        false);
                    boxObject.debug = debug;
                    _collideDetectionObject = boxObject;
                }
                    break;
                case ActionCollideDetectionSphereData sphereData:
                {
                    var instance = new GameObject("Collide Detection Sphere")
                    {
                        transform =
                        {
                            parent = _parent,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var sphereObject = instance.AddComponent<CollideDetectionShapeSphereObject>();
                    sphereObject.SetParams(_owner.transform, sphereData.localPosition, sphereData.radius, false);
                    sphereObject.debug = debug;
                    _collideDetectionObject = sphereObject;
                }
                    break;
                case ActionCollideDetectionSectorData sectorData:
                {
                    var instance = new GameObject("Collide Detection Sector")
                    {
                        transform =
                        {
                            parent = _parent,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var sectorObject = instance.AddComponent<CollideDetectionShapeSectorObject>();
                    sectorObject.SetParams(
                        _owner.transform, 
                        sectorData.localPosition, 
                        sectorData.localRotation,
                        sectorData.insideRadius, 
                        sectorData.outsideRadius, 
                        sectorData.height, 
                        sectorData.anglePivot, 
                        sectorData.angle, 
                        false
                        );
                    sectorObject.debug = debug;
                    _collideDetectionObject = sectorObject;
                }
                    break;
            }
        }

        public void Tick(Action<Collider> detectionDelegate)
        {
            switch (_collideDetectionObject)
            {
                case BaseCollideDetectionShapeObject shapeObject:
                    shapeObject.Detect(detectionDelegate, _layerMask);
                    break;
            }
        }

        public void Destroy()
        {
            if (!_collideDetectionObject) return;
            
            switch (_collideDetectionObject)
            {
                case BaseCollideDetectionBindObject bindObject:
                {
                    bindObject.CancelDetect();
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(bindObject);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(bindObject);
                    }
                }
                    break;
                case BaseCollideDetectionShapeObject shapeObject:
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(shapeObject.gameObject);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(shapeObject.gameObject);
                    }
                }
                    break;
            }
        }
    }
}