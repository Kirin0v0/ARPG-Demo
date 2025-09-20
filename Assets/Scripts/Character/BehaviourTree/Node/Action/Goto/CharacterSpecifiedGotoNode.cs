using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Character.BehaviourTree.Node.Action.Goto
{
    [NodeMenuItem("Action/Character/Goto/Specified")]
    public class CharacterSpecifiedGotoNode : BaseCharacterGotoNode
    {
        private enum GotoType
        {
            Position,
            Target,
        }

        private enum GotoPositionType
        {
            Global,
            Local,
        }

        [SerializeField] private GotoType gotoType = GotoType.Position;

        [SerializeField] [ShowIf("@gotoType == GotoType.Position")]
        private GotoPositionType gotoPositionType = GotoPositionType.Global;

        [SerializeField] [ShowIf("@gotoType == GotoType.Position")]
        private Vector3 gotoPosition = Vector3.zero;

        public override string Description => "角色指定位置前往节点，用于共享前往数据，如果前往类型配置的是目标，请检查是否在该节点执行前执行了目标节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            switch (gotoType)
            {
                case GotoType.Position:
                {
                    var destination = gotoPositionType switch
                    {
                        GotoPositionType.Global => parameters.Character.Parameters.position + gotoPosition,
                        GotoPositionType.Local => parameters.Character.transform.TransformPoint(gotoPosition),
                        _ => parameters.Character.Parameters.position + gotoPosition,
                    };
                    ShareGoto(parameters, destination);
                }
                    break;
                case GotoType.Target:
                {
                    if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                            out var target) ||
                        target is not CharacterObject targetCharacter)
                    {
                        DebugUtil.LogError(
                            $"The target character is not found in the shared dictionary");
                        return NodeState.Failure;
                    }

                    ShareGoto(parameters, targetCharacter.Parameters.position);
                }
                    break;
            }

            return NodeState.Success;
        }
    }
}