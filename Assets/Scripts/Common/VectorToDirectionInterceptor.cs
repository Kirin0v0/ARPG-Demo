using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Common
{
    /// <summary>
    /// 摇杆二维矢量输入转上下左右方向的拦截器，用于获取一段时间内的方向输入
    /// </summary>
    public class VectorToDirectionInterceptor : MonoBehaviour
    {
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
        }

        public float cooldownDuration;

        private bool _isCooldown;

        private Direction _lastDirection = Direction.Up;

        public void Refresh()
        {
            StopAllCoroutines();
            _isCooldown = false;
        }

        public void Intercept(Vector2 origin, UnityAction<Direction> onExecute)
        {
            var direction = CalculateDirection();
            if (_lastDirection != direction)
            {
                Refresh();
            }

            if (_isCooldown) return;
            _lastDirection = direction;
            onExecute.Invoke(direction);
            StartCoroutine(Cooldown());

            return;

            Direction CalculateDirection()
            {
                if (Mathf.Abs(origin.x) > Mathf.Abs(origin.y))
                {
                    return origin.x >= 0 ? Direction.Right : Direction.Left;
                }
                else
                {
                    return origin.y >= 0 ? Direction.Up : Direction.Down;
                }
            }
        }

        private System.Collections.IEnumerator Cooldown()
        {
            _isCooldown = true;
            yield return new WaitForSecondsRealtime(cooldownDuration);
            _isCooldown = false;
        }
    }
}