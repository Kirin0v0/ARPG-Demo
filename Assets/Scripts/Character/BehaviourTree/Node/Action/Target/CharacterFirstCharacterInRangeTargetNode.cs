using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Target
{
    [NodeMenuItem("Action/Character/Target/Universal/First Character in Range")]
    public class CharacterFirstCharacterInRangeTargetNode : BaseCharacterTargetNode
    {
        [SerializeField] [MinValue(0f)] private float radius = 3f;

        [SerializeField] private bool prototypeFilterEnable = false;

        [SerializeField, ShowIf("prototypeFilterEnable")]
        private string prototype = "";

        [SerializeField] private bool sideFilterEnable = false;

        [SerializeField, ShowIf("sideFilterEnable")]
        private CharacterSide side;

        [SerializeField] private bool tagFilterEnable = false;

        [SerializeField, ShowIf("tagFilterEnable")]
        private string tag;

        public override string Description => "角色目标节点，如果在自身范围内存在指定角色共享角色作为目标角色，并返回成功，否则返回失败";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.GameManager)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            CharacterObject target = null;
            foreach (var character in parameters.GameManager.Characters)
            {
                if (character == parameters.Character
                    || (character.Parameters.position - parameters.Character.Parameters.position).sqrMagnitude >
                    radius * radius
                    || (prototypeFilterEnable && character.Parameters.prototype != prototype)
                    || (sideFilterEnable && character.Parameters.side != side)
                    || (tagFilterEnable && !character.HasTag(tag)))
                {
                    continue;
                }

                target = character;
                break;
            }

            if (target == null)
            {
                return NodeState.Failure;
            }

            ShareTarget(parameters, target);

            return NodeState.Success;
        }
    }
}