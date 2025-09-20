using System;
using Character.Brain;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Look At")]
    public class CharacterLookAtNode : ActionNode
    {
        private enum LookAtType
        {
            Target,
            Goto,
        }

        [SerializeField] private LookAtType type;
        [SerializeField] [MinValue(0f)] private float duration = 0.1f;

        private float _time;

        public override string Description =>
            "角色看向目标节点，使用前请检查先执行的节点是否在运行时数据共享目标角色或目标地点，每帧会调整角色旋转量，旋转结束才会返回成功";

        protected override void OnStart(object payload)
        {
            _time = 0f;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            _time += deltaTime;

            switch (type)
            {
                case LookAtType.Target:
                {
                    if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                            out var target) ||
                        target is not CharacterObject targetCharacter)
                    {
                        DebugUtil.LogError(
                            $"The target character is not found in the shared dictionary");
                        return NodeState.Failure;
                    }

                    var lookAtDirection = targetCharacter.Parameters.position -
                                          parameters.Character.Parameters.position;
                    lookAtDirection = new Vector3(lookAtDirection.x, 0, lookAtDirection.z);
                    var rotation = Quaternion.Lerp(
                        parameters.Character.transform.rotation,
                        Quaternion.LookRotation(lookAtDirection),
                        deltaTime / duration
                    );
                    parameters.Character.MovementAbility?.RotateTo(rotation);
                }
                    break;
                case LookAtType.Goto:
                {
                    if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.GotoParams,
                            out var value) ||
                        value is not Vector3 gotoPosition)
                    {
                        DebugUtil.LogError(
                            $"The goto position is not found in the shared dictionary");
                        return NodeState.Failure;
                    }


                    var lookAtDirection = gotoPosition - parameters.Character.Parameters.position;
                    lookAtDirection = new Vector3(lookAtDirection.x, 0, lookAtDirection.z);
                    var rotation = Quaternion.Lerp(
                        parameters.Character.transform.rotation,
                        Quaternion.LookRotation(lookAtDirection),
                        deltaTime / duration
                    );
                    parameters.Character.MovementAbility?.RotateTo(rotation);
                }
                    break;
            }

            return _time >= duration ? NodeState.Success : NodeState.Running;
        }
    }
}