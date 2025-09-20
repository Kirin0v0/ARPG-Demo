using Framework.Common.BehaviourTree.Node.Action;

namespace Character.BehaviourTree.Node.Action.Target
{
    public abstract class BaseCharacterTargetNode : ActionNode
    {
        protected void ShareTarget(CharacterBehaviourTreeParameters parameters, CharacterObject target)
        {
            if (parameters.Shared.ContainsKey(CharacterBehaviourTreeParameters.TargetParams))
            {
                parameters.Shared[CharacterBehaviourTreeParameters.TargetParams] = target;
            }
            else
            {
                parameters.Shared.Add(CharacterBehaviourTreeParameters.TargetParams, target);
            }
        }
    }
}