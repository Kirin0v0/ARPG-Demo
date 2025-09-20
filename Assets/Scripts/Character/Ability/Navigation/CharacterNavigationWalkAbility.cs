using System.Collections;
using System.Collections.Generic;
using Character.Ability.Navigation;
using Framework.Common.Debug;
using Framework.Common.Util;
using Map;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using VContainer;

namespace Character.Ability.Navigation
{
    public class CharacterNavigationWalkAbility : CharacterNavigationAbility, INavigationWalk
    {
        [Title("导航组件配置")] [SerializeField] [InfoBox("部分组件参数由代码内部控制，仅提前配置Agent Type、Priority及Area Mask即可")]
        private NavMeshAgent navMeshAgent;

        public NavMeshAgent NavMeshAgent => navMeshAgent;

        [SerializeField] [InfoBox("组件参数由代码内部控制，不需要提前配置")]
        private NavMeshObstacle navMeshObstacle;

        public NavMeshObstacle NavMeshObstacle => navMeshObstacle;

        [FormerlySerializedAs("greedyDistance")] [SerializeField, MinValue(0f)]
        private float minGreedyDistance = 0.3f;

        [Title("调试")] [FormerlySerializedAs("debug")] [SerializeField]
        private bool debugStartAndStop;

        [FormerlySerializedAs("debugMovement")] [SerializeField]
        private bool debugTick;

        private bool _inNavigation = false;

        public override bool InNavigation
        {
            get => _inNavigation;
            protected set
            {
                if (_inNavigation == value)
                {
                    return;
                }

                _inNavigation = value;
                if (_inNavigation)
                {
                    _navigationPath.Clear();
                }

                ResetNavigationComponent();
            }
        }

        private NavMeshSurface _navMeshSurface;

        // 导航中参数
        private Vector3 _destination;
        private NavMeshPath _navMeshPath;
        private readonly List<Vector3> _navigationPath = new();

        [Inject] private MapManager _mapManager;

        protected override void OnInit()
        {
            base.OnInit();

            // 获取导航Surface
            _navMeshSurface =
                NavMeshSurface.activeSurfaces.Find(surface => surface.agentTypeID == navMeshAgent.agentTypeID);

            // 设置导航代理
            navMeshAgent.enabled = false;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.height = Owner.CharacterController.height;
            navMeshAgent.radius = Owner.CharacterController.radius +
                                  2 * GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius;

            // 设置导航障碍
            navMeshObstacle.enabled = true;
            navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
            navMeshObstacle.center = Owner.CharacterController.center;
            navMeshObstacle.height = Owner.CharacterController.height;
            navMeshObstacle.radius = Owner.CharacterController.radius +
                                     2 * GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius;
            navMeshObstacle.carveOnlyStationary = false;
            navMeshObstacle.carving = true;
        }

        public override void Tick(float deltaTime)
        {
            if (!InNavigation || !navMeshAgent.enabled)
            {
                return;
            }

            // 如果角色死亡就停止导航
            if (Owner.Parameters.dead)
            {
                StopWalkNavigation();
                return;
            }

            // 计算导航路径
            _navMeshPath = new NavMeshPath();
            // 先拿最终目的地计算路径，如果存在路径即一定能够导航成功
            // 如果不存在路径就采用贪心策略直接取目的地和自身的方向的一小段距离作为临时目的地，如果存在路径也可认为能够导航，否则就认为无法抵达，停止导航
            if (!navMeshAgent.CalculatePath(_destination, _navMeshPath))
            {
                var tempDestination =
                    (_destination - navMeshAgent.transform.position).normalized *
                    Mathf.Max(minGreedyDistance, navMeshAgent.stoppingDistance + 0.1f) +
                    navMeshAgent.transform.position;
                if (!navMeshAgent.CalculatePath(tempDestination, _navMeshPath))
                {
                    if (debugTick)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})未找到可用路径");
                    }

                    StopWalkNavigation();
                    return;
                }
                else
                {
                    if (debugTick)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})采用贪心路径导航");
                    }
                }
            }
            else
            {
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})采用最优路径导航");
                }
            }

            // 获取下一个目的地，如果没有找到则认为已经到终点
            if (!FindNextDestination(_navMeshPath, out var nextPosition))
            {
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})未找到路径的下一个目的地");
                }

                StopWalkNavigation();
                return;
            }

            // 根据导航方向移动角色
            var direction = Vector3.Normalize(nextPosition - navMeshAgent.transform.position);
            var movement = navMeshAgent.speed * deltaTime * direction;
            Owner.MovementAbility?.Move(movement, true);
            if (debugTick)
            {
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})导航方向: {direction}");
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})导航移动: {movement}");
            }

            // 如果开启导航时同步面向，则执行以下逻辑
            if (SynchronizeRotationWhenNavigation)
            {
                // 计算当前帧导航的旋转，注意，这里导航过程中会强制锁死角色面向
                var rotation = Quaternion.RotateTowards(
                    Owner.Parameters.rotation,
                    Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z)),
                    navMeshAgent.angularSpeed * deltaTime
                );
                Owner.MovementAbility?.RotateTo(rotation);
            }

            return;

            bool FindNextDestination(NavMeshPath path, out Vector3 nextDestination)
            {
                nextDestination = Vector3.zero;
                if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length <= 0)
                {
                    return false;
                }

                var index = 0;
                while (ReachDestination(path.corners[index], navMeshAgent.stoppingDistance))
                {
                    index++;
                    if (index >= path.corners.Length)
                    {
                        return false;
                    }
                }

                nextDestination = path.corners[index];
                return true;
            }
        }

        public override void LateCheckNavigation(float deltaTime)
        {
            if (!InNavigation)
            {
                return;
            }

            // 如果到达目的地，则停止导航
            if (ReachDestination(_destination, navMeshAgent.stoppingDistance))
            {
                StopWalkNavigation();
            }

            // 记录导航位置
            _navigationPath.Add(Owner.Parameters.position);
            // 对齐导航代理位置
            navMeshAgent.Warp(Owner.Parameters.position);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _navMeshSurface = null;
        }

        public void StartWalkNavigation(Vector3 destination, float speed, float angularSpeed,
            float stoppingDistance = 0.1f)
        {
            if (InNavigation)
            {
                StopWalkNavigation();
            }

            // 获取目的地在导航网格最近点的位置，如果没有该位置认为不可能导航到这里，就不执行导航
            var navMeshDestination = TryGetNavMeshClosestPoint(destination, out var closestPoint)
                ? closestPoint
                : Owner.Parameters.position;

            // 如果已到达目的地，则不予导航
            if (ReachDestination(navMeshDestination, stoppingDistance))
            {
                return;
            }

            if (debugStartAndStop)
            {
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})开始导航：目的地{navMeshDestination}");
            }

            InNavigation = true;
            _destination = navMeshDestination;
            navMeshAgent.speed = speed;
            navMeshAgent.angularSpeed = angularSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;
            // 对齐导航代理位置
            navMeshAgent.Warp(Owner.Parameters.position);
        }

        public void StopWalkNavigation()
        {
            if (!InNavigation)
            {
                return;
            }

            if (debugStartAndStop)
            {
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})停止导航：目的地{_destination}");
            }

            InNavigation = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!InNavigation)
            {
                return;
            }

            // 绘制导航网格给出的当前路径
            if (_navMeshPath != null && _navMeshPath.corners.Length >= 2)
            {
                for (var i = 0; i < _navMeshPath.corners.Length - 1; i++)
                {
                    var point1 = _navMeshPath.corners[i];
                    var point2 = _navMeshPath.corners[i + 1];
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(point1, 0.5f);
                    Gizmos.DrawSphere(point2, 0.5f);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(point1, point2);
                }
            }

            // 绘制自导航始的实际路径
            if (_navigationPath.Count >= 2)
            {
                for (var i = 0; i < _navigationPath.Count - 1; i++)
                {
                    var start = _navigationPath[i];
                    var end = _navigationPath[i + 1];
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(start, end);
                }
            }

            // 绘制导航目的地
            Gizmos.color = new Color(241 / 256f, 196 / 256f, 15 / 256f);
            Gizmos.DrawSphere(_destination, 0.5f);
        }

        /// <summary>
        /// 判断是否到达目的地
        /// </summary>
        /// <returns></returns>
        private bool ReachDestination(Vector3 destination, float stoppingDistance)
        {
            // 对步行导航来说仅考虑XZ轴上的距离，不考虑Y轴距离
            return !MathUtil.IsMoreThanDistance(navMeshAgent.transform.position, destination,
                Mathf.Max(stoppingDistance, 0.01f), MathUtil.TwoDimensionAxisType.XZ);
        }

        private bool TryGetNavMeshClosestPoint(Vector3 position, out Vector3 closestPoint)
        {
            closestPoint = position;
            // 获取导航网格上最接近的点，如果没有获取到就返回false，代表该点没有导航的意义
            if (NavMesh.SamplePosition(position, out var hit, 100f, NavMesh.AllAreas))
            {
                closestPoint = hit.position;
                return true;
            }

            return false;
        }

        private void ResetNavigationComponent()
        {
            if (_inNavigation)
            {
                StartCoroutine(ToNavigate());
            }
            else
            {
                navMeshAgent.enabled = false;
                navMeshObstacle.carving = true;
                navMeshObstacle.enabled = true;
            }
        }

        private IEnumerator ToNavigate()
        {
            navMeshObstacle.carving = false;
            navMeshObstacle.enabled = false;
            yield return 0;
            navMeshAgent.enabled = true;
        }
    }
}