using System;
using System.Collections.Generic;
using System.Linq;
using Damage.Data;
using Framework.Common.Debug;
using Inputs;
using Interact;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace Character.Ability
{
    [RequireComponent(typeof(UnityEngine.Collider), typeof(Rigidbody))]
    public class CharacterInteractAbility : BaseCharacterOptionalAbility
    {
        // 存储游戏对象<-->可交互接口的字典，但不代表其能够交互
        private readonly Dictionary<GameObject, IInteractable> _interactableObjects = new();

        protected IInteractable CurrentInteractableObject;

        protected override void OnInit()
        {
            base.OnInit();
            var collider = GetComponent<UnityEngine.Collider>();
            if (collider)
            {
                collider.isTrigger = true;
            }
        }

        public virtual void Tick(float deltaTime)
        {
            CurrentInteractableObject = null;
            // 如果当前角色死亡，则不进行交互
            if (Owner.Parameters.dead) return;
            // 依次遍历所有可交互对象，设置当前交互对象
            foreach (var pair in _interactableObjects)
            {
                if (pair.Key && pair.Value.AllowInteract(Owner.gameObject))
                {
                    CurrentInteractableObject = pair.Value;
                }
            }
        }

        private void OnTriggerEnter(UnityEngine.Collider other)
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                _interactableObjects.Add(other.gameObject, interactable);
            }
        }

        private void OnTriggerExit(UnityEngine.Collider other)
        {
            _interactableObjects.Remove(other.gameObject);
        }

        /// <summary>
        /// 主动交互函数，由角色自身调用用于与可交互物体交互
        /// </summary>
        public void Interact()
        {
            // 角色死亡下不允许主动交互其他可交互物体
            if (Owner.Parameters.dead)
            {
                return;
            }

            CurrentInteractableObject?.Interact(Owner.gameObject);
        }

        private void OnValidate()
        {
            var collider = GetComponent<UnityEngine.Collider>();
            if (collider)
            {
                collider.isTrigger = true;
            }
        }
    }
}