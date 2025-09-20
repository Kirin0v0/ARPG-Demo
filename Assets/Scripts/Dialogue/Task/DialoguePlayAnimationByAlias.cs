using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Dialogue.Task
{
    [Category("Animation")]
    public class DialoguePlayAnimationByAlias : ActionTask<CharacterReference>
    {
        [RequiredField] public BBParameter<StringAsset> alias;
        
        private AnimancerState _animancerState;
        
        protected override string info => $"Play animation by alias({alias.value?.name ?? ""})";

        protected override void OnExecute()
        {
            if (agent.Value.AnimationAbility)
            {
                _animancerState = agent.Value.AnimationAbility.PlayAction(alias.value);
                if (_animancerState != null)
                {
                    _animancerState.OwnedEvents.OnEnd = OnAnimationEnd;
                }
            }

            EndAction();
        }

        private void OnAnimationEnd()
        {
            if (_animancerState == null)
            {
                return;
            }

            agent.Value.AnimationAbility?.ClearAction(true);
            _animancerState.OwnedEvents.OnEnd = null;
            _animancerState = null;
        }
    }
}