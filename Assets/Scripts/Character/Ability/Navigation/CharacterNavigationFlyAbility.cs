using System.Collections;
using System.Collections.Generic;
using Framework.Common.Debug;
using Framework.Common.Util;
using Map;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Character.Ability.Navigation
{
    public class CharacterNavigationFlyAbility : CharacterNavigationAbility, INavigationFly
    {
        [Title("导航组件配置")] [SerializeField] [InfoBox("部分组件参数由代码内部控制，仅提前配置Agent Type、Priority及Area Mask即可")]
        private NavMeshAgent navMeshAgent;

        public NavMeshAgent NavMeshAgent => navMeshAgent;

        [SerializeField] [InfoBox("组件参数由代码内部控制，不需要提前配置")]
        private NavMeshObstacle navMeshObstacle;

        public NavMeshObstacle NavMeshObstacle => navMeshObstacle;

        [SerializeField, MinValue(0f)] private float minGreedyDistance = 0.3f;

        [Title("调试")] [SerializeField] private bool debugStartAndStop;
        [SerializeField] private bool debugTick;

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

        private Vector3 _navMeshDestination; // 导航网格目的地,并不是实际达到后的角色位置
        private NavMeshPath _navMeshPath; // 导航网格的导航路径,也不是实际的路径
        private float _height; // 距离地面的高度
        private float _verticalSpeed; // 垂直速度
        private float _heightStoppingDistance; // 高度停止距离
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

            // 设置角色导航障碍
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
                StopFlyNavigation();
                return;
            }

            // 计算导航路径
            _navMeshPath = new NavMeshPath();
            // 先拿最终目的地计算路径，如果存在路径即一定能够导航成功
            // 如果不存在路径就采用贪心策略直接取目的地和自身的方向的一小段距离作为临时目的地，如果存在路径也可认为能够导航，否则就不在导航网格上移动，仅考虑竖直移动
            if (!navMeshAgent.CalculatePath(_navMeshDestination, _navMeshPath))
            {
                var tempDestination =
                    (_navMeshDestination - navMeshAgent.transform.position).normalized *
                    Mathf.Max(minGreedyDistance, navMeshAgent.stoppingDistance + 0.1f) +
                    navMeshAgent.transform.position;
                if (!navMeshAgent.CalculatePath(tempDestination, _navMeshPath))
                {
                    if (debugTick)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})在网格上未找到可用路径");
                    }
                }
                else
                {
                    if (debugTick)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})在网格上采用贪心路径导航");
                    }
                }
            }
            else
            {
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})在网格上采用最优路径导航");
                }
            }

            var needNavigateInNavMesh = false;
            var needNavigateInAirborne = false;

            // 获取导航网格的下一个目的地，如果没有找到则认为在网格上到达终点
            if (FindNextNavMeshDestination(out var nextNavMeshPosition))
            {
                needNavigateInNavMesh = true;

                // 根据网格导航水平面方向移动角色
                var direction = new Vector3(nextNavMeshPosition.x - navMeshAgent.transform.position.x, 0,
                    nextNavMeshPosition.z - navMeshAgent.transform.position.z).normalized;
                var movement = navMeshAgent.speed * deltaTime * direction;
                Owner.MovementAbility?.Move(movement, true);
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})网格导航方向: {direction}");
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})网格导航移动: {movement}");
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
            }
            else
            {
                needNavigateInNavMesh = false;
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})无需网格导航");
                }
            }

            // 判断是否需要高度移动，需要则设置垂直速度
            if (NeedHeightMove(out var verticalSpeed))
            {
                needNavigateInAirborne = true;
                Owner.Parameters.verticalSpeed = verticalSpeed;
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})高度导航速度: {verticalSpeed}");
                }
            }
            else
            {
                needNavigateInAirborne = false;
                Owner.Parameters.verticalSpeed = 0f;
                if (debugTick)
                {
                    DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})无需高度导航");
                }
            }

            // 最终判断是否需要两个维度导航，如果都不需要，就认为已经完成导航，停止导航
            if (!needNavigateInNavMesh && !needNavigateInAirborne)
            {
                StopFlyNavigation();
            }

            return;

            bool FindNextNavMeshDestination(out Vector3 nextDestination)
            {
                nextDestination = Vector3.zero;
                if (_navMeshPath.status == NavMeshPathStatus.PathInvalid || _navMeshPath.corners.Length <= 0)
                {
                    return false;
                }

                var index = 0;
                while (ReachNavMeshDestination(_navMeshPath.corners[index], navMeshAgent.stoppingDistance))
                {
                    index++;
                    if (index >= _navMeshPath.corners.Length)
                    {
                        return false;
                    }
                }

                nextDestination = _navMeshPath.corners[index];
                return true;
            }

            bool NeedHeightMove(out float verticalSpeed)
            {
                verticalSpeed = 0f;
                var detectDistance = _height + _heightStoppingDistance;
                // 从位置向下发射射线检测导航表面层，计算距离高度是否与导航高度近似
                if (Physics.Raycast(Owner.Parameters.position, Vector3.down, out var hit, detectDistance,
                        _navMeshSurface.layerMask))
                {
                    var distance = Owner.Parameters.position.y - hit.point.y;
                    if (Mathf.Abs(distance - _height) <= _heightStoppingDistance)
                    {
                        return false;
                    }

                    verticalSpeed = distance < _height ? _verticalSpeed : -_verticalSpeed;
                    return true;
                }

                // 如果没有发生碰撞则代表处于高空
                verticalSpeed = -_verticalSpeed;
                return true;
            }
        }

        public override void LateCheckNavigation(float deltaTime)
        {
            if (!InNavigation)
            {
                return;
            }

            // 如果到达目的地和指定高度，则停止导航
            if (ReachNavMeshDestination(_navMeshDestination, navMeshAgent.stoppingDistance) &&
                ReachNavigationHeight(_height, _heightStoppingDistance))
            {
                StopFlyNavigation();
            }

            // 记录导航位置
            _navigationPath.Add(Owner.Parameters.position);

            // 先计算自身在导航网格对应的位置，再对齐导航代理位置
            if (TryGetAgentPosition(Owner.Parameters.position, out var position))
            {
                navMeshAgent.Warp(position);
            }
            else
            {
                navMeshAgent.Warp(Owner.Parameters.position);
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _navMeshSurface = null;
        }

        public void StartFlyNavigation(
            Vector3 destination,
            float height,
            float horizontalSpeed,
            float verticalSpeed,
            float angularSpeed,
            float horizontalStoppingDistance = 0.1f,
            float verticalStoppingDistance = 0.1f
        )
        {
            if (InNavigation)
            {
                StopFlyNavigation();
            }

            // 获取目的地在导航网格最近点的位置，如果没有该位置认为不可能导航到这里，就不执行导航
            var navMeshDestination = TryGetNavMeshClosestPoint(destination, out var closestPoint)
                ? closestPoint
                : Owner.Parameters.position;

            // 如果到达目的地和指定高度，则不予导航
            if (ReachNavMeshDestination(navMeshDestination, horizontalStoppingDistance) &&
                ReachNavigationHeight(height, verticalStoppingDistance))
            {
                return;
            }

            if (debugStartAndStop)
            {
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})开始导航：网格目的地({navMeshDestination}), 高度({height})");
            }

            InNavigation = true;
            _navMeshDestination = navMeshDestination;
            navMeshAgent.speed = horizontalSpeed;
            navMeshAgent.angularSpeed = angularSpeed;
            navMeshAgent.stoppingDistance = horizontalStoppingDistance;
            _height = height;
            _verticalSpeed = verticalSpeed;
            _heightStoppingDistance = verticalStoppingDistance;

            // 先计算自身在导航网格对应的位置，再对齐导航代理位置
            if (TryGetAgentPosition(Owner.Parameters.position, out var position))
            {
                navMeshAgent.Warp(position);
            }
            else
            {
                navMeshAgent.Warp(Owner.Parameters.position);
            }
        }

        public void StopFlyNavigation()
        {
            if (!InNavigation)
            {
                return;
            }

            if (debugStartAndStop)
            {
                DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})停止导航：目的地{_navMeshDestination}");
            }

            InNavigation = false;
        }

        private void OnDrawGizmosSelected()
        {
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
        }

        /// <summary>
        /// 判断是否到达导航网格的目的地
        /// </summary>
        /// <returns></returns>
        private bool ReachNavMeshDestination(Vector3 destination, float stoppingDistance)
        {
            // 仅考虑XZ轴上的距离，不考虑Y轴距离
            return !MathUtil.IsMoreThanDistance(navMeshAgent.transform.position, destination,
                Mathf.Max(stoppingDistance, 0.01f), MathUtil.TwoDimensionAxisType.XZ);
        }

        /// <summary>
        /// 判断是否到达导航高度
        /// </summary>
        /// <returns></returns>
        private bool ReachNavigationHeight(float height, float stoppingDistance)
        {
            // 从位置向下发射射线检测导航表面层，计算距离高度是否与导航高度近似
            if (Physics.Raycast(Owner.Parameters.position, Vector3.down, out var hit, _height + stoppingDistance,
                    _navMeshSurface.layerMask))
            {
                var distance = Owner.Parameters.position.y - hit.point.y;
                return Mathf.Abs(distance - height) <= stoppingDistance;
            }

            return false;
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

        private bool TryGetAgentPosition(Vector3 position, out Vector3 agentPosition)
        {
            agentPosition = Vector3.zero;
            if (_navMeshSurface &&
                Physics.Raycast(position, Vector3.down, out var hit, 100f, _navMeshSurface.layerMask))
            {
                agentPosition = hit.point;
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