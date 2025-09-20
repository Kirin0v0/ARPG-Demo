using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Framework.Common.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Target Distance")]
    public class CharacterTargetDistanceConditionNode : ActionNode
    {
        private enum DistanceComparerType
        {
            Near,
            Far,
        }

        public enum DistanceAxisType
        {
            XZ,
            Y
        }

        [FormerlySerializedAs("type")] [SerializeField]
        private DistanceComparerType comparerType = DistanceComparerType.Near;

        [SerializeField] private DistanceAxisType axisType = DistanceAxisType.XZ;
        [SerializeField] private float distance = 5f;

        public override string Description => "目标距离条件节点，使用前请检查是否在该节点执行前执行了目标节点，满足比较条件返回成功，否则返回失败";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target) ||
                target is not CharacterObject targetCharacter)
            {
                DebugUtil.LogError(
                    $"The target character is not found in the shared dictionary");
                return NodeState.Failure;
            }

            switch (axisType)
            {
                case DistanceAxisType.XZ:
                {
                    switch (comparerType)
                    {
                        case DistanceComparerType.Near:
                        {
                            return !MathUtil.IsLessThanDistance(
                                parameters.Character.Parameters.position,
                                targetCharacter.Parameters.position,
                                distance + parameters.Character.CharacterController.radius +
                                targetCharacter.CharacterController.radius +
                                GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
                                MathUtil.TwoDimensionAxisType.XZ)
                                ? NodeState.Failure
                                : NodeState.Success;
                        }
                            break;
                        case DistanceComparerType.Far:
                        {
                            return !MathUtil.IsMoreThanDistance(
                                parameters.Character.Parameters.position,
                                targetCharacter.Parameters.position,
                                distance + parameters.Character.CharacterController.radius +
                                targetCharacter.CharacterController.radius +
                                GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
                                MathUtil.TwoDimensionAxisType.XZ)
                                ? NodeState.Failure
                                : NodeState.Success;
                        }
                            break;
                    }
                }
                    break;
                case DistanceAxisType.Y:
                {
                    var additionalHeight =
                        parameters.Character.Parameters.position.y >= targetCharacter.Parameters.position.y
                            ? targetCharacter.CharacterController.height
                            : parameters.Character.CharacterController.height;
                    var offset = Mathf.Abs(parameters.Character.Parameters.position.y -
                                           targetCharacter.Parameters.position.y);
                    switch (comparerType)
                    {
                        case DistanceComparerType.Near:
                        {
                            return offset < distance + additionalHeight
                                ? NodeState.Success
                                : NodeState.Failure;
                        }
                            break;
                        case DistanceComparerType.Far:
                        {
                            return offset > distance + additionalHeight
                                ? NodeState.Success
                                : NodeState.Failure;
                        }
                            break;
                    }
                }
                    break;
            }

            return NodeState.Failure;
        }
    }
}