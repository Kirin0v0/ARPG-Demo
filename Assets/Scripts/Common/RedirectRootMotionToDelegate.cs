using System;
using Animancer;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Common
{
    public interface IRedirectRootMotionDelegate
    {
        void HandleAnimatorMove(Animator animator);
    }

    [RequireComponent(typeof(Animator))]
    public class RedirectRootMotionToDelegate : SerializedMonoBehaviour
    {
        [SerializeField] private Animator animator;

        [SerializeField] private IRedirectRootMotionDelegate redirectDelegate;

        private bool ApplyRootMotion
            => animator != null && animator.applyRootMotion;

        private void OnValidate()
        {
            TryGetComponent(out animator);

            if (redirectDelegate == null)
            {
                var parent = transform.parent;
                if (parent != null)
                    redirectDelegate = parent.GetComponentInParent<IRedirectRootMotionDelegate>();

                if (redirectDelegate == null)
                    TryGetComponent(out redirectDelegate);
            }
        }

        private void OnAnimatorMove()
        {
            if (!ApplyRootMotion)
            {
                return;
            }

            redirectDelegate.HandleAnimatorMove(animator);
        }
    }
}