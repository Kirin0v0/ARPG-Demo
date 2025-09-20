using Character;
using NodeCanvas.DialogueTrees;
using UnityEngine;

namespace Dialogue
{
    [RequireComponent(typeof(CharacterReference))]
    public class DialogueCharacterActor : DialogueActor
    {
        public CharacterReference Reference => GetComponent<CharacterReference>();
    }
}