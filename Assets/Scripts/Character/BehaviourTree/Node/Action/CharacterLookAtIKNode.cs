using System;
using Character.Brain;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/IK/Look At")]
    public class CharacterLookAtIKNode : ActionNode
    {
        private enum LookAtType
        {
            Target,
            Goto,
        }

        [SerializeField] private bool enable = true;
        [SerializeField] [ShowIf("@enable == true")] private LookAtType type;

        public override string Description =>
            "角色IK看向目标节点，使用前请检查先执行的节点是否在运行时数据共享目标角色或目标地点，当前帧记录IK位置并返回成功";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            parameters.Character.AnimationAbility.SetLookAtWeight(enable ? 1f : 0f);
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

                    parameters.Character.AnimationAbility.SetLookAtPosition(targetCharacter.Visual.Eye.position);
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

                    parameters.Character.AnimationAbility.SetLookAtPosition(gotoPosition);
                }
                    break;
            }
            
            return NodeState.Success;
        }
    }
}