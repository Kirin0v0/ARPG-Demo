using UnityEngine;

namespace Framework.Algorithm.AStar
{
    public interface IAStarDistanceEvaluate
    {
        public float Evaluate(Vector2Int current, Vector2Int target);
    }
}