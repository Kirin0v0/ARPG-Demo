using System;
using System.Collections.Generic;
using Character;
using Cinemachine;
using JetBrains.Annotations;
using UnityEngine;

namespace Camera
{
    [RequireComponent(typeof(CinemachineBrain))]
    public class CameraTransparentBehaviour : MonoBehaviour
    {
        [SerializeField] private Material transparentMaterial;
        [SerializeField] private LayerMask transparentLayers;

        private CinemachineBrain _cinemachineBrain;

        private CinemachineBrain CinemachineBrain
        {
            get
            {
                if (!_cinemachineBrain)
                {
                    _cinemachineBrain = GetComponent<CinemachineBrain>();
                }

                return _cinemachineBrain;
            }
        }

        private readonly List<RaycastHit> _hits = new();
        private readonly List<HitInfo> _changedInfos = new();

        private Transform LookAt
        {
            get
            {
                var lookAt = CinemachineBrain.ActiveVirtualCamera?.LookAt;
                if (!lookAt)
                {
                    return null;
                }
                
                return lookAt.TryGetComponent<CharacterObject>(out var character) ? character.Visual.Eye : lookAt;
            }
        }

        private struct HitInfo
        {
            public GameObject Obj;
            public Renderer[] Renderers;
            public List<Material> Materials;
        }

        private void Update()
        {
            ChangeObstaclesMaterial();
        }

        private void OnDrawGizmosSelected()
        {
            var target = LookAt;
            if (target != null)
            {
                var distance = Vector3.Distance(transform.position, target.position) - 1;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position,
                    transform.position + distance * (target.position - transform.position).normalized);
            }
        }

        private void ChangeObstaclesMaterial()
        {
            var target = LookAt;
            if (target == null)
            {
                return;
            }

            // 用相机位置与角色位置的差值-1是为了避免检测到地面，提高可移植性，否则射线检测到的对象还要除去地面
            _hits.AddRange(
                Physics.RaycastAll(
                    transform.position,
                    target.position - transform.position,
                    Vector3.Distance(transform.position, target.position) - 1,
                    transparentLayers
                )
            );

            // 替换材质
            for (var i = 0; i < _hits.Count; i++)
            {
                // 射线检测到的对象除去角色
                if (_hits[i].collider.gameObject != target.gameObject)
                {
                    var hit = _hits[i];
                    int findIndex = _changedInfos.FindIndex(item => item.Obj == hit.collider.gameObject);
                    var rendererArray = hit.collider.gameObject.GetComponentsInChildren<Renderer>();

                    // 没找到则添加
                    if (rendererArray.Length > 0 && findIndex < 0)
                    {
                        var changed = new HitInfo
                        {
                            Obj = hit.collider.gameObject,
                            Renderers = rendererArray,
                            Materials = new List<Material>()
                        };

                        for (var j = 0; j < rendererArray.Length; j++)
                        {
                            var materials = rendererArray[j].materials;
                            var tempMaterials = new Material[materials.Length];

                            for (var k = 0; k < materials.Length; k++)
                            {
                                changed.Materials.Add(materials[k]);
                                tempMaterials[k] = transparentMaterial;
                            }

                            rendererArray[j].materials = tempMaterials; //替换材质
                        }

                        _changedInfos.Add(changed);
                    }
                }
            }

            // 还原材质
            for (var i = 0; i < _changedInfos.Count;)
            {
                var changedInfo = _changedInfos[i];
                var findIndex = _hits.FindIndex(item => item.collider.gameObject == changedInfo.Obj);

                //没找到则移除
                if (findIndex < 0)
                {
                    if (changedInfo.Obj != null)
                    {
                        foreach (var renderer in changedInfo.Renderers)
                        {
                            renderer.materials = changedInfo.Materials.ToArray(); //还原材质
                        }
                    }

                    _changedInfos.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            _hits.Clear();
        }
    }
}