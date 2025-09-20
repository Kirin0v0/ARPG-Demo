using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Dialogue.Task
{
    [Category("Animation")]
    public class DialogueStopAnimationByAlias : ActionTask<CharacterReference>
    {
        [RequiredField] public BBParameter<StringAsset> alias;
        
        protected override string info => $"Stop animation by alias({alias.value?.name ?? ""})";
        
        protected override void OnExecute()
        {
            agent.Value.AnimationAbility?.StopAction(alias.value, true);
            EndAction();
        }
    }
}