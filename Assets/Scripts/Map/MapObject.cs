using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Map
{
    [Serializable]
    public class MapSnapshot
    {
        [SerializeField] private Vector2 center = Vector2.zero;
        [SerializeField] private Vector2 size = Vector2.zero;
        [SerializeField] private float height = 0f;
        [SerializeField, FilePath] private string path = "";
#if UNITY_EDITOR
        public Color gizmosColor = Color.red;
        public float gizmosRadius = 2f;
#endif

        public Vector3 Center3D => new Vector3(center.x, height, center.y);
        public Vector2 Center2D => center;
        public Vector2 Size => size;
        public string Path => path;

        #region 世界坐标系的矩形四角位置，从屏幕坐标系来看则LeftBottom是原点

        public Vector3 LeftTop3D => new Vector3(center.x - size.x / 2f, height, center.y + size.y / 2f);
        public Vector2 LeftTop2D => new Vector3(center.x - size.x / 2f, center.y + size.y / 2f);
        public Vector3 RightTop3D => new Vector3(center.x + size.x / 2f, height, center.y + size.y / 2f);
        public Vector2 RightTop2D => new Vector3(center.x + size.x / 2f, center.y + size.y / 2f);
        public Vector3 LeftBottom3D => new Vector3(center.x - size.x / 2f, height, center.y - size.y / 2f);
        public Vector2 LeftBottom2D => new Vector3(center.x - size.x / 2f, center.y - size.y / 2f);
        public Vector3 RightBottom3D => new Vector3(center.x + size.x / 2f, height, center.y - size.y / 2f);
        public Vector2 RightBottom2D => new Vector3(center.x + size.x / 2f, center.y - size.y / 2f);

        #endregion
    }

    public class MapObject : MonoBehaviour
    {
        [SerializeField] private MapSnapshot snapshot = new();
        public MapSnapshot Snapshot => snapshot;

        [SerializeField] private Material skybox;
        public Material Skybox => skybox;

        private void OnDrawGizmosSelected()
        {
            var color = Color.red;
            var radius = 2f;
#if UNITY_EDITOR
            color = snapshot.gizmosColor;
            radius = snapshot.gizmosRadius;
#endif
            Gizmos.color = color;
            Gizmos.DrawSphere(snapshot.LeftTop3D, radius);
            Gizmos.DrawSphere(snapshot.RightTop3D, radius);
            Gizmos.DrawSphere(snapshot.LeftBottom3D, radius);
            Gizmos.DrawSphere(snapshot.RightBottom3D, radius);
            Gizmos.DrawLine(snapshot.LeftTop3D, snapshot.RightTop3D);
            Gizmos.DrawLine(snapshot.RightTop3D, snapshot.RightBottom3D);
            Gizmos.DrawLine(snapshot.RightBottom3D, snapshot.LeftBottom3D);
            Gizmos.DrawLine(snapshot.LeftBottom3D, snapshot.LeftTop3D);
        }
    }
}