using UnityEngine;

namespace Framework.Algorithm.AStar
{
    public class AStarManhattanDistanceEvaluate : IAStarDistanceEvaluate
    {
        public float Evaluate(Vector2Int current, Vector2Int target)
        {
            return Mathf.Abs(target.x - current.x) + Mathf.Abs(target.y - current.y);
        }
    }
}