using System;
using Common;
using Dialogue;
using Framework.Common.Debug;
using Interact;
using NodeCanvas.DialogueTrees;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    [RequireComponent(typeof(UnityEngine.Collider))]
    public class CharacterDialogueAbility : BaseCharacterOptionalAbility, IInteractable
    {
        public event System.Action<DialogueTree> OnEnterDialogue;
        public event System.Action<DialogueTree> OnExitDialogue;

        [Title("对话树配置")] [SerializeField] private DialogueTreeController dialogueTreeController;
        [SerializeField] private DialogueCharacterActor dialogueActor;
        public DialogueCharacterActor DialogueActor => dialogueActor;

        [Inject] private GameManager _gameManager;

        protected override void OnInit()
        {
            base.OnInit();
            GetComponent<UnityEngine.Collider>().isTrigger = true;
        }

        public void SetDialogue(string name, DialogueTree dialogueTree)
        {
            dialogueActor._name = name;
            dialogueTreeController.behaviour = dialogueTree;
            dialogueTreeController.BindExposedParameters();
        }

        public void Tick(float deltaTime)
        {
            // 由于NodeCanvas框架限制，无法实现对话树的可达路径获取
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            if (dialogueTreeController.isRunning)
            {
                dialogueTreeController.StopDialogue();
            }

            dialogueTreeController.behaviour = null;
        }

        /// <summary>
        /// 允许交互函数
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool AllowInteract(GameObject target)
        {
            // 如果不是玩家角色，就不允许交互
            var targetCharacter = target.GetComponent<CharacterObject>();
            if (!targetCharacter || targetCharacter == Owner || _gameManager.Player != targetCharacter)
            {
                return false;
            }

            // 如果没有配置对话树，也不允许交互
            if (!dialogueTreeController.behaviour)
            {
                return false;
            }

            // 如果自身或对方死亡/处于对话/处于空中/处于战斗，也不允许交互
            return !Owner.Parameters.dead &&
                   !targetCharacter.Parameters.dead &&
                   !Owner.Parameters.inDialogue &&
                   !targetCharacter.Parameters.inDialogue &&
                   !Owner.Parameters.Airborne &&
                   !targetCharacter.Parameters.Airborne &&
                   Owner.Parameters.battleState != CharacterBattleState.Battle &&
                   targetCharacter.Parameters.battleState != CharacterBattleState.Battle;
        }

        /// <summary>
        /// 被动交互函数，作为可交互物体被其他角色交互
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public void Interact(GameObject target)
        {
            // 开始对话
            dialogueTreeController.StartDialogue(dialogueActor);
        }

        public string Tip(GameObject target) => "对话";

        public void EnterDialogue(DialogueTree dialogueTree)
        {
            Owner.Parameters.inDialogue = true;
            OnDialogueEnter(dialogueTree);
            OnEnterDialogue?.Invoke(dialogueTree);
        }

        protected virtual void OnDialogueEnter(DialogueTree dialogueTree)
        {
        }

        public void ExitDialogue(DialogueTree dialogueTree)
        {
            Owner.Parameters.inDialogue = false;
            OnDialogueExit(dialogueTree);
            OnExitDialogue?.Invoke(dialogueTree);
        }

        protected virtual void OnDialogueExit(DialogueTree dialogueTree)
        {
        }
    }
}