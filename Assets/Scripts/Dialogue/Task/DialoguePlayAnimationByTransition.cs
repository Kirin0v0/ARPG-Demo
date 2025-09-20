using Animancer;
using Character;
using Framework.Common.Debug;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Dialogue.Task
{
    [Category("Animation")]
    public class DialoguePlayAnimationByTransition : ActionTask<CharacterReference>
    {
        [RequiredField] public BBParameter<TransitionAsset> transition;

        private AnimancerState _animancerState;
        
        protected override string info => $"Play animation by transition({transition.value?.name ?? ""})";

        protected override void OnExecute()
        {
            if (agent.Value.AnimationAbility)
            {
                _animancerState = agent.Value.AnimationAbility.PlayAction(transition.value);
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