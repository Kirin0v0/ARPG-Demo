using System;
using Animancer;
using Character.Ability.Navigation;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Character.BehaviourTree.Node.Action.Navigate
{
    [NodeMenuItem("Action/Character/Navigation/Navigate To Position")]
    public class CharacterNavigateToPositionNode : ActionNode
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

        private float _time;

        public override string Description =>
            "角色导航节点，使用前请检查先执行的节点是否在运行时数据共享前往地点，直到导航结束才会返回成功";

        protected override void OnStart(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.GotoParams,
                    out var value) ||
                value is not Vector3 gotoPosition)
            {
                DebugUtil.LogError(
                    $"The goto position is not found in the shared dictionary");
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

                    navigationWalk.StartWalkNavigation(gotoPosition, navMeshSpeed, angularSpeed,
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
                        gotoPosition,
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
            parameters.Character.NavigationAbility.OpenSynchronizeRotationWhenNavigation();
        }

        protected override void OnResume(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.NavigationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.GotoParams,
                    out var value) ||
                value is not Vector3 gotoPosition)
            {
                DebugUtil.LogError(
                    $"The goto position is not found in the shared dictionary");
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

                    navigationWalk.StartWalkNavigation(gotoPosition, navMeshSpeed, angularSpeed,
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
                        gotoPosition,
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
            parameters.Character.NavigationAbility.OpenSynchronizeRotationWhenNavigation();
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
    }
}