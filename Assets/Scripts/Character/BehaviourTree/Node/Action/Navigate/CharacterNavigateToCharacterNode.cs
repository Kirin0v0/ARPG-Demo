using System;
using Animancer;
using Character.Ability.Navigation;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Map;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Character.BehaviourTree.Node.Action.Navigate
{
    [NodeMenuItem("Action/Character/Navigation/Navigate To Character")]
    public class CharacterNavigateToCharacterNode : ActionNode
    {
        public enum NavigationMode
        {
            Walk,
            Fly,
        }

        [Title("导航配置")] [SerializeField] private NavigationMode mode;

        [ShowIf("mode", NavigationMode.Fly)] [SerializeField] [MinValue(0f)]
        private float flyHeight = 2f;

        [FormerlySerializedAs("speed")] [SerializeField] [MinValue(0f)]
        private float navMeshSpeed = 5f;

        [ShowIf("mode", NavigationMode.Fly)] [SerializeField] [MinValue(0f)]
        private float flySpeed = 3f;

        [SerializeField] [MinValue(0f)] private float angularSpeed = 120f;

        [FormerlySerializedAs("stoppingDistance")] [SerializeField] [MinValue(0f)]
        private float navMeshStoppingDistance = 0.1f;

        [ShowIf("mode", NavigationMode.Fly)] [SerializeField] [MinValue(0f)]
        private float flyStoppingDistance = 0.1f;

        [Title("时间限制配置")] [SerializeField] private bool openLimitTime = false;

        [SerializeField] [MinValue(0f), ShowIf("openLimitTime")]
        private float limitTime = 0f;

        [Title("看向角色配置")] [SerializeField] private bool lookAtTargetCharacter;

        private float _time;

        public override string Description =>
            "角色导航节点，使用前请检查先执行的节点是否在运行时数据共享目标角色，直到导航结束才会返回成功\n此外，允许选择是否在导航中看向目标，如未选中则默认看向导航路径";

        protected override void OnStart(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target) ||
                target is not CharacterObject targetCharacter)
            {
                DebugUtil.LogError(
                    $"The target character is not found in the shared dictionary");
                return;
            }

            // 获取目标角色的最近点作为目的地，没有获取到就直接返回，不进行导航
            if (!TryGetTargetNearestDestination(parameters.Character, targetCharacter, Mathf.Max(
                    parameters.MapManager.Map.Snapshot.Size.x, parameters.MapManager.Map.Snapshot.Size.y,
                    100f), out var destination))
            {
                return;
            }

            switch (mode)
            {
                case NavigationMode.Walk:
                {
                    if (parameters.Character.NavigationAbility is not INavigationWalk navigationWalk)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationWalk.StartWalkNavigation(destination, navMeshSpeed, angularSpeed,
                        navMeshStoppingDistance);
                }
                    break;
                case NavigationMode.Fly:
                {
                    if (parameters.Character.NavigationAbility is not INavigationFly navigationFly)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationFly.StartFlyNavigation(
                        destination,
                        flyHeight,
                        navMeshSpeed,
                        flySpeed,
                        angularSpeed,
                        navMeshStoppingDistance,
                        flyStoppingDistance
                    );
                }
                    break;
            }

            _time = 0f;
            if (lookAtTargetCharacter)
            {
                parameters.Character.NavigationAbility.CloseSynchronizeRotationWhenNavigation();
            }
            else
            {
                parameters.Character.NavigationAbility.OpenSynchronizeRotationWhenNavigation();
            }
        }

        protected override void OnResume(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target) ||
                target is not CharacterObject targetCharacter)
            {
                DebugUtil.LogError(
                    $"The target character is not found in the shared dictionary");
                return;
            }

            // 获取目标角色的最近点作为目的地，没有获取到就直接返回，不进行导航
            if (!TryGetTargetNearestDestination(parameters.Character, targetCharacter, Mathf.Max(
                    parameters.MapManager.Map.Snapshot.Size.x, parameters.MapManager.Map.Snapshot.Size.y,
                    100f), out var destination))
            {
                return;
            }

            switch (mode)
            {
                case NavigationMode.Walk:
                {
                    if (parameters.Character.NavigationAbility is not INavigationWalk navigationWalk)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationWalk.StartWalkNavigation(destination, navMeshSpeed, angularSpeed,
                        navMeshStoppingDistance);
                }
                    break;
                case NavigationMode.Fly:
                {
                    if (parameters.Character.NavigationAbility is not INavigationFly navigationFly)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationFly.StartFlyNavigation(
                        destination,
                        flyHeight,
                        navMeshSpeed,
                        flySpeed,
                        angularSpeed,
                        navMeshStoppingDistance,
                        flyStoppingDistance
                    );
                }
                    break;
            }

            _time = 0f;
            if (lookAtTargetCharacter)
            {
                parameters.Character.NavigationAbility.CloseSynchronizeRotationWhenNavigation();
            }
            else
            {
                parameters.Character.NavigationAbility.OpenSynchronizeRotationWhenNavigation();
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            if (openLimitTime && _time > limitTime)
            {
                return NodeState.Success;
            }

            _time += deltaTime;
            // 如果导航中看向目标角色，就每帧执行旋转
            if (lookAtTargetCharacter)
            {
                if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                        out var target) ||
                    target is not CharacterObject targetCharacter)
                {
                    DebugUtil.LogError(
                        $"The target character is not found in the shared dictionary");
                    return NodeState.Failure;
                }

                // 计算当前帧导航的旋转，注意，这里导航过程中会强制锁死角色面向
                var lookAtDirection = targetCharacter.Parameters.position -
                                      parameters.Character.Parameters.position;
                lookAtDirection = new Vector3(lookAtDirection.x, 0, lookAtDirection.z);
                var rotation = Quaternion.RotateTowards(
                    parameters.Character.Parameters.rotation,
                    Quaternion.LookRotation(lookAtDirection),
                    angularSpeed * deltaTime
                );
                parameters.Character.MovementAbility?.RotateTo(rotation);
            }

            return parameters.Character.NavigationAbility.InNavigation ? NodeState.Running : NodeState.Success;
        }

        protected override void OnAbort(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            switch (mode)
            {
                case NavigationMode.Walk:
                {
                    if (parameters.Character.NavigationAbility is not INavigationWalk navigationWalk)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationWalk.StopWalkNavigation();
                }
                    break;
                case NavigationMode.Fly:
                {
                    if (parameters.Character.NavigationAbility is not INavigationFly navigationFly)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationFly.StopFlyNavigation();
                }
                    break;
            }

            parameters.Character.NavigationAbility.CloseSynchronizeRotationWhenNavigation();
        }

        protected override void OnStop(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            switch (mode)
            {
                case NavigationMode.Walk:
                {
                    if (parameters.Character.NavigationAbility is not INavigationWalk navigationWalk)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationWalk.StopWalkNavigation();
                }
                    break;
                case NavigationMode.Fly:
                {
                    if (parameters.Character.NavigationAbility is not INavigationFly navigationFly)
                    {
                        DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                        return;
                    }

                    navigationFly.StopFlyNavigation();
                }
                    break;
            }

            parameters.Character.NavigationAbility.CloseSynchronizeRotationWhenNavigation();
        }

        private bool TryGetTargetNearestDestination(CharacterObject self, CharacterObject target, float maxDistance,
            out Vector3 destination)
        {
            destination = target.Parameters.position;
            // 计算与目标角色最接近的目的地位置（XZ水平面上，Y轴由于角色碰撞问题忽略了）
            var direction = target.Parameters.position - self.Parameters.position;
            var nearestDestinationDirection = new Vector3(direction.x, 0, direction.z).normalized;
            var nearestDestination = target.Parameters.position +
                                     (target.CharacterController.radius + GlobalRuleSingletonConfigSO.Instance
                                         .collideDetectionExtraRadius) * nearestDestinationDirection;
            destination = nearestDestination;
            return true;
        }
    }
}