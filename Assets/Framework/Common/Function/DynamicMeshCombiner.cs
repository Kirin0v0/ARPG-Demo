using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Common.Util;
using UnityEngine;

namespace Framework.Common.Function
{
    public class DynamicMeshCombiner : MonoBehaviour
    {
        [SerializeField] private bool combineWhenAwake = true;

        private MeshFilter[] ChildrenMeshFilters
        {
            get
            {
                var meshFilters = GetComponentsInChildren<MeshFilter>(false);
                return meshFilters.Where(x => x.transform != transform).ToArray();
            }
        }

        private void Awake()
        {
            if (combineWhenAwake)
            {
                CombineChildrenMeshes();
                DestroyChildrenMeshes();
            }
        }

        public void CombineChildrenMeshes()
        {
            MeshUtil.CombineAllMeshes(ChildrenMeshFilters, transform.worldToLocalMatrix, out var mesh,
                out var materials);

            if (!TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.mesh = mesh;

            if (!TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            meshRenderer.sharedMaterials = materials;
        }

        public void DestroyChildrenMeshes()
        {
            foreach (var meshFilter in ChildrenMeshFilters)
            {
                Destroy(meshFilter);
                if (meshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    Destroy(meshRenderer);
                }
            }
        }
    }
}