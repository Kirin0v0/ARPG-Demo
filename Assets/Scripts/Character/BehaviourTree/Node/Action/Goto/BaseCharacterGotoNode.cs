using Framework.Common.BehaviourTree.Node.Action;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    public abstract class BaseCharacterGotoNode: ActionNode
    {
        public void ShareGoto(CharacterBehaviourTreeParameters parameters, Vector3 destination)
        {
            if (parameters.Shared.ContainsKey(CharacterBehaviourTreeParameters.GotoParams))
            {
                parameters.Shared[CharacterBehaviourTreeParameters.GotoParams] = destination;
            }
            else
            {
                parameters.Shared.Add(CharacterBehaviourTreeParameters.GotoParams, destination);
            }
        }
    }
}