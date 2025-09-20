using Character.Ability;
using Character.SO;
using Common;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Brain
{
    public abstract class CharacterBrain : MonoBehaviour, ICharacterBrain, IRedirectRootMotionDelegate, IRedirectIKDelegate
    {
        protected CharacterObject Owner;

        public event System.Action<Animator> AnimatorIKDelegate;

        public void Init(CharacterObject owner)
        {
            Owner = owner;
            OnBrainInit();
        }

        public void UpdateRenderThoughts(float deltaTime)
        {
            OnRenderThoughtsUpdated(deltaTime);
        }

        public void UpdateLogicThoughts(float fixedDeltaTime)
        {
            OnLogicThoughtsUpdated(fixedDeltaTime);
        }

        public void Destroy()
        {
            OnBrainDestroy();
            Owner = null;
        }

        public virtual void HandleAnimatorMove(Animator animator)
        {
            Owner.MovementAbility?.SwitchType(CharacterMovementType.CharacterController);
            Owner.MovementAbility?.Move(animator.deltaPosition, true);
            Owner.MovementAbility?.Rotate(animator.deltaRotation, true);
        }

        public virtual void HandleAnimatorIK(Animator animator)
        {
            AnimatorIKDelegate?.Invoke(animator);
        }

        protected virtual void OnBrainInit()
        {
        }

        protected virtual void OnRenderThoughtsUpdated(float deltaTime)
        {
        }

        protected virtual void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
        }

        protected virtual void OnBrainDestroy()
        {
        }
    }
}