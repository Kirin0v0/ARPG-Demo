using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Ability
{
    public abstract class BaseCharacterOptionalAbility : SerializedMonoBehaviour, ICharacterAbility
    {
        protected CharacterObject Owner;

        public void Init(CharacterObject owner)
        {
            Owner = owner;
            OnInit();
        }

        public void Dispose()
        {
            OnDispose();
            Owner = null;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}