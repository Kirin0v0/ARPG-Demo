using UnityEngine;

namespace Framework.Algorithm.AStar
{
    public class AStarGridInfo
    {
        // 格子二维索引
        public Vector2Int Index;
        // 是否为阻碍
        public bool Block;
        // 实际业务数据
        public object Payload;
    }
}