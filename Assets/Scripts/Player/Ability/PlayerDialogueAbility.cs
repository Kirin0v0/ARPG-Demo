using Camera;
using Character;
using Character.Ability;
using Cinemachine;
using Dialogue;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using NodeCanvas.DialogueTrees;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.Ability
{
    public class PlayerDialogueAbility : CharacterDialogueAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;
        
        [Inject] private CameraManager _cameraManager;
        
        [Title("对话状态切换")] [SerializeField] private PlayerActionTalkState talkState;

        [Title("对话相机设置")] [SerializeField] private float cameraHeight;
        [SerializeField] private float cameraRadius;

        protected override void OnDialogueEnter(DialogueTree dialogueTree)
        {
            base.OnDialogueEnter(dialogueTree);
            // 创建对话默认相机
            var agentCharacter = dialogueTree.agent.GetComponent<CharacterReference>().Value;
            var virtualCamera = _cameraManager.CreateDialogueDefaultCamera();
            var direction = Owner.Parameters.position - agentCharacter.Parameters.position;
            virtualCamera.transform.position = Owner.Parameters.position +
                                               new Vector3(direction.x, 0, direction.z).normalized * cameraRadius
                                               + Vector3.up * cameraHeight;
            virtualCamera.LookAt = agentCharacter.transform;
            virtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
            // 切换玩家状态
            if (Owner is PlayerCharacterObject player)
            {
                // 在切换时设置玩家朝向
                player.StateMachine.SwitchState(talkState, true);
                talkState.StartFacingTarget(agentCharacter);
            }
        }

        protected override void OnDialogueExit(DialogueTree dialogueTree)
        {
            base.OnDialogueExit(dialogueTree);
            // 清空对话相机
            _cameraManager.ClearAllDialogueCameras();
            // 切换玩家状态
            if (Owner is PlayerCharacterObject player)
            {
                player.StateMachine.SwitchToDefault();
            }
        }
    }
}