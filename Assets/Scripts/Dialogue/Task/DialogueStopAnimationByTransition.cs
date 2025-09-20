using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Dialogue.Task
{
    [Category("Animation")]
    public class DialogueStopAnimationByTransition : ActionTask<CharacterReference>
    {
        [RequiredField] public BBParameter<TransitionAsset> transition;
        
        protected override string info => $"Stop animation by transition({transition.value?.name ?? ""})";
        
        protected override void OnExecute()
        {
            agent.Value.AnimationAbility?.StopAction(transition.value, true);
            EndAction();
        }
    }
}