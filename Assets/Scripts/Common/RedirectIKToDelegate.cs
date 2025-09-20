using System;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Common
{
    public interface IRedirectIKDelegate
    {
        void HandleAnimatorIK(Animator animator);
    }

    [RequireComponent(typeof(Animator))]
    public class RedirectIKToDelegate : SerializedMonoBehaviour
    {
        [SerializeField] private Animator animator;

        [SerializeField] private IRedirectIKDelegate redirectDelegate;

        private bool ApplyRootMotion
            => animator != null && animator.applyRootMotion;

        private void OnValidate()
        {
            TryGetComponent(out animator);

            if (redirectDelegate == null)
            {
                var parent = transform.parent;
                if (parent != null)
                    redirectDelegate = parent.GetComponentInParent<IRedirectIKDelegate>();

                if (redirectDelegate == null)
                    TryGetComponent(out redirectDelegate);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            redirectDelegate.HandleAnimatorIK(animator);
        }
    }
}