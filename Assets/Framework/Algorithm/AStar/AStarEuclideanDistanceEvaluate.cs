using UnityEngine;

namespace Framework.Algorithm.AStar
{
    public class AStarEuclideanDistanceEvaluate : IAStarDistanceEvaluate
    {
        public float Evaluate(Vector2Int current, Vector2Int target)
        {
            return Vector2Int.Distance(target, current);
        }
    }
}