using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Common.Util
{
    public static class MeshUtil
    {
        public static Mesh CombineMaterialMeshes(
            MeshFilter[] toCombineMeshFilters,
            Material material,
            Matrix4x4 worldToLocalMatrix,
            bool allowMatchByName = false
        )
        {
            // 1.收集目标材质的合并实例
            var combines = new List<CombineInstance>();
            for (var index = 0; index < toCombineMeshFilters.Length; index++)
            {
                var filter = toCombineMeshFilters[index];
                if (!filter.TryGetComponent<MeshRenderer>(out var renderer))
                {
                    UnityEngine.Debug.LogWarning($"{filter.name} don't have MeshRenderer");
                    continue;
                }

                // 遍历材质直到出现目标材质才会合并网格
                for (var subMeshIndex = 0; subMeshIndex < renderer.sharedMaterials.Length; subMeshIndex++)
                {
                    var sharedMaterial = renderer.sharedMaterials[subMeshIndex];
                    if (sharedMaterial != material && (!allowMatchByName || sharedMaterial.name != material.name))
                    {
                        continue;
                    }

                    var instance = new CombineInstance();
                    // 设置想要合并的网格信息
                    instance.mesh = filter.sharedMesh;
                    // 设置子网格的索引
                    instance.subMeshIndex = subMeshIndex;
                    // 设置变化矩阵，将子网格的顶点位置从当前本地空间变换到对应的本地空间
                    instance.transform = worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    // 添加到合并列表中
                    combines.Add(instance);
                    break;
                }
            }

            // 2.创建新网格
            var mesh = new Mesh();
            // 统计合并顶点数，如果超过了UInt16的限制，则设置为UInt32
            var totalVertices = 0;
            foreach (var item in combines)
            {
                // 累加想要合并的小网格的顶点数
                totalVertices += item.mesh.vertexCount;
            }

            if (totalVertices > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            // 3.合并网格
            mesh.CombineMeshes(combines.ToArray(), true);
            // 4.重新计算数据
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void CombineAllMeshes(
            MeshFilter[] toCombineMeshFilters,
            Matrix4x4 worldToLocalMatrix,
            out Mesh mesh,
            out Material[] materials
        )
        {
            // 1.收集所有材质
            var targetMaterials = new HashSet<Material>();
            foreach (var filter in toCombineMeshFilters)
            {
                if (!filter.TryGetComponent<MeshRenderer>(out var renderer))
                {
                    continue;
                }

                var localMaterials = renderer.sharedMaterials;
                foreach (var localMaterial in localMaterials)
                {
                    targetMaterials.Add(localMaterial);
                }
            }

            // 2.根据材质进行合并子网格
            var materialSubMeshes = new Dictionary<Material, Mesh>();
            foreach (var material in targetMaterials)
            {
                var materialMesh = CombineMaterialMeshes(toCombineMeshFilters, material, worldToLocalMatrix);
                materialSubMeshes.Add(material, materialMesh);
            }

            // 3.合并为最终网格
            var finalMesh = new Mesh();
            var vertexOffset = 0;
            finalMesh.subMeshCount = materialSubMeshes.Count;
            var finalVertices = new List<Vector3>();
            var finalUVs = new List<Vector2>();
            var finalTris = new List<List<int>>();
            foreach (var materialSubMesh in materialSubMeshes)
            {
                finalVertices.AddRange(materialSubMesh.Value.vertices);
                finalUVs.AddRange(materialSubMesh.Value.uv);
                finalTris.Add(materialSubMesh.Value.triangles.Select(x => x + vertexOffset).ToList());
                vertexOffset += materialSubMesh.Value.vertexCount;
            }

            finalMesh.SetVertices(finalVertices);
            finalMesh.SetUVs(0, finalUVs);
            for (int i = 0; i < materialSubMeshes.Count; i++)
            {
                finalMesh.SetTriangles(finalTris[i], i);
            }

            finalMesh.RecalculateTangents();
            finalMesh.RecalculateNormals();

            mesh = finalMesh;
            materials = materialSubMeshes.Keys.ToArray();
        }

        public static Mesh Generate2DSectorMesh(
            float insideRadius,
            float radius,
            float anglePivot,
            float angle,
            Vector3 forwardDirection,
            float deltaAngle = 2f
        )
        {
            return GenerateSectorTopAndBottomSurfaceMesh(insideRadius, radius, anglePivot, angle, forwardDirection, 0f,
                    deltaAngle)
                .mesh;
        }

        private static (Mesh mesh, int rectCount, int lineCount) GenerateSectorTopAndBottomSurfaceMesh(
            float insideRadius,
            float radius,
            float anglePivot,
            float angle,
            Vector3 forwardDirection,
            float height,
            float deltaAngle = 2f
        )
        {
            // 检查数据
            radius = Mathf.Max(radius, 0f);
            insideRadius = Mathf.Clamp(insideRadius, 0f, radius);
            height = Mathf.Abs(height);

            var mesh = new Mesh();
            var position = Vector3.zero;
            var startLineDirection =
                Quaternion.AngleAxis(anglePivot + angle / 2, Vector3.up) * forwardDirection; // 起始线方向
            var rectCount = (int)(angle / deltaAngle);
            var lineCount = rectCount + 1;

            // 扇形顶点数组
            var vertexes = new Vector3[lineCount * 2 * 2];
            // 扇形三角形顶点索引数组
            var triangleVertexIndexes = new int[rectCount * 6 * 2];

            // 计算底面
            for (var i = 0; i < lineCount; i++)
            {
                // 计算扇形分区的顶点
                var lineDirection = Quaternion.AngleAxis(-i * deltaAngle, Vector3.up) * startLineDirection;
                var insideVertex = position + insideRadius * lineDirection - Vector3.up * height / 2;
                var outsideVertex = position + radius * lineDirection - Vector3.up * height / 2;
                vertexes[i * 2] = insideVertex;
                vertexes[i * 2 + 1] = outsideVertex;

                // 计算扇形分区的三角形顶点索引
                if (i < lineCount - 1)
                {
                    triangleVertexIndexes[i * 6] = i * 2 + 1;
                    triangleVertexIndexes[i * 6 + 1] = i * 2 + 2;
                    triangleVertexIndexes[i * 6 + 2] = i * 2;
                    triangleVertexIndexes[i * 6 + 3] = i * 2 + 1;
                    triangleVertexIndexes[i * 6 + 4] = i * 2 + 3;
                    triangleVertexIndexes[i * 6 + 5] = i * 2 + 2;
                }
            }

            // 计算顶面
            for (var i = lineCount; i < 2 * lineCount; i++)
            {
                // 计算扇形分区的顶点
                var lineDirection = Quaternion.AngleAxis(-(i - lineCount) * deltaAngle, Vector3.up) *
                                    startLineDirection;
                var insideVertex = position + insideRadius * lineDirection + Vector3.up * height / 2;
                var outsideVertex = position + radius * lineDirection + Vector3.up * height / 2;
                vertexes[i * 2] = insideVertex;
                vertexes[i * 2 + 1] = outsideVertex;

                // 计算扇形分区的三角形顶点索引
                if (i < 2 * lineCount - 1)
                {
                    triangleVertexIndexes[(i - 1) * 6] = i * 2 + 1;
                    triangleVertexIndexes[(i - 1) * 6 + 1] = i * 2;
                    triangleVertexIndexes[(i - 1) * 6 + 2] = i * 2 + 2;
                    triangleVertexIndexes[(i - 1) * 6 + 3] = i * 2 + 1;
                    triangleVertexIndexes[(i - 1) * 6 + 4] = i * 2 + 2;
                    triangleVertexIndexes[(i - 1) * 6 + 5] = i * 2 + 3;
                }
            }

            mesh.vertices = vertexes;
            mesh.triangles = triangleVertexIndexes;
            mesh.RecalculateNormals();

            return (mesh, rectCount, lineCount);
        }

        public static Mesh Generate3DSectorMesh(
            float insideRadius,
            float radius,
            float anglePivot,
            float angle,
            Vector3 forwardDirection,
            float height,
            float deltaAngle = 2f
        )
        {
            var mesh = new Mesh();
            var result =
                GenerateSectorTopAndBottomSurfaceMesh(insideRadius, radius, anglePivot, angle, forwardDirection, height,
                    deltaAngle);
            var rectCount = result.rectCount;
            var oldSectorMesh = result.mesh;
            var oldVertexes = oldSectorMesh.vertices;
            var oldTriangleVertexIndexes = oldSectorMesh.triangles;
            var newTriangleVertexIndexes = new int[oldTriangleVertexIndexes.Length + 3 * 2 * 2 + 3 * 2 * rectCount * 2];
            Array.Copy(oldTriangleVertexIndexes, newTriangleVertexIndexes, oldTriangleVertexIndexes.Length);

            // 计算扇形起始侧面的三角形顶点索引
            {
                var start = oldTriangleVertexIndexes.Length;
                newTriangleVertexIndexes[start] = 0;
                newTriangleVertexIndexes[start + 1] = oldVertexes.Length / 2;
                newTriangleVertexIndexes[start + 2] = 1;
                newTriangleVertexIndexes[start + 3] = 1;
                newTriangleVertexIndexes[start + 4] = oldVertexes.Length / 2;
                newTriangleVertexIndexes[start + 5] = oldVertexes.Length / 2 + 1;
            }

            // 计算扇形结束侧面的三角形顶点索引
            {
                var start = oldTriangleVertexIndexes.Length + 3 * 2;
                newTriangleVertexIndexes[start] = oldVertexes.Length / 2 - 2;
                newTriangleVertexIndexes[start + 1] = oldVertexes.Length / 2 - 1;
                newTriangleVertexIndexes[start + 2] = oldVertexes.Length - 2;
                newTriangleVertexIndexes[start + 3] = oldVertexes.Length / 2 - 1;
                newTriangleVertexIndexes[start + 4] = oldVertexes.Length - 1;
                newTriangleVertexIndexes[start + 5] = oldVertexes.Length - 2;
            }

            // 计算扇形内侧面的三角形顶点索引
            {
                var start = oldTriangleVertexIndexes.Length + 3 * 2 * 2;
                for (int i = 0; i < rectCount; i++)
                {
                    newTriangleVertexIndexes[start + i * 6] = i * 2;
                    newTriangleVertexIndexes[start + i * 6 + 1] = (i + 1) * 2 + oldVertexes.Length / 2;
                    newTriangleVertexIndexes[start + i * 6 + 2] = i * 2 + oldVertexes.Length / 2;
                    newTriangleVertexIndexes[start + i * 6 + 3] = i * 2;
                    newTriangleVertexIndexes[start + i * 6 + 4] = (i + 1) * 2;
                    newTriangleVertexIndexes[start + i * 6 + 5] = (i + 1) * 2 + oldVertexes.Length / 2;
                }
            }

            // 计算扇形外侧面的三角形顶点索引
            {
                var start = oldTriangleVertexIndexes.Length + 3 * 2 * 2 + 3 * 2 * rectCount;
                for (int i = 0; i < rectCount; i++)
                {
                    newTriangleVertexIndexes[start + i * 6] = 1 + i * 2;
                    newTriangleVertexIndexes[start + i * 6 + 1] = 1 + i * 2 + oldVertexes.Length / 2;
                    newTriangleVertexIndexes[start + i * 6 + 2] = 1 + (i + 1) * 2 + oldVertexes.Length / 2;
                    newTriangleVertexIndexes[start + i * 6 + 3] = 1 + i * 2;
                    ;
                    newTriangleVertexIndexes[start + i * 6 + 4] = 1 + (i + 1) * 2 + oldVertexes.Length / 2;
                    newTriangleVertexIndexes[start + i * 6 + 5] = 1 + (i + 1) * 2;
                }
            }

            mesh.vertices = oldVertexes;
            mesh.triangles = newTriangleVertexIndexes;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}