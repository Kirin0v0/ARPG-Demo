using UnityEngine;
using UnityEngine.Serialization;

namespace Character
{
    public class CharacterReference: MonoBehaviour
    {
        [FormerlySerializedAs("character")] [SerializeField] private CharacterObject value;
        public CharacterObject Value => value;

        public void Init(CharacterObject character)
        {
            value = character;
        }
    }
}