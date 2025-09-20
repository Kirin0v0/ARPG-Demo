using System;
using System.Collections.Generic;
using Character;
using Cinemachine;
using Framework.Common.Debug;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Camera")]
    public class DialogueSetCameraLookAtByAgents : BaseDialogueCameraTask
    {
        [Serializable]
        public class CameraTargetMember
        {
            public string name;
            public float weight;
            public float radius;
        }
        public enum LookAtPosition
        {
            Top,
            Center,
            Bottom,
        }

        [ReferenceField] public BBParameter<float> selfWeight = new(1);
        [ReferenceField] public BBParameter<float> selfRadius = new(0);
        [RequiredField] public BBParameter<List<CameraTargetMember>> otherMembers = new(new List<CameraTargetMember>());
        [RequiredField] public BBParameter<LookAtPosition> position = new(LookAtPosition.Center);
        [RequiredField] public BBParameter<Vector3> offset = new(Vector3.zero);
        [RequiredField] public BBParameter<bool> @fixed = new(true);

        protected override string info
        {
            get
            {
                var agents = new List<string> { agentInfo };
                otherMembers.value?.ForEach(member => agents.Add(member.name));
                return $"Set camera {cameraId} to look at by agents({string.Join(", ", agents)})";
            }
        }

        protected override void OnExecute()
        {
            var virtualCamera = FindOrCreateCamera();
            virtualCamera.gameObject.SetActive(false);
            // 根据字符串名称匹配对话树的代理人
            var members = new List<(Transform target, float weight, float radius)>
            {
                new()
                {
                    target = GetAgentLookAtPosition(agent.Value),
                    weight = selfWeight.value,
                    radius = selfRadius.value
                }
            };
            otherMembers.value?.ForEach(member =>
            {
                foreach (var dialogueActorParameter in DialogueTree.currentDialogue.actorParameters)
                {
                    if (dialogueActorParameter.name == member.name)
                    {
                        if (dialogueActorParameter.actor is DialogueCharacterActor dialogueCharacterActor)
                        {
                            members.Add(new()
                            {
                                target = GetAgentLookAtPosition(dialogueCharacterActor.Reference.Value),
                                weight = member.weight,
                                radius = member.radius
                            });
                        }

                        break;
                    }
                }
            });
            // 创建跟随目标组和句柄
            CreateTargetGroupAndHandler(members.ToArray(), offset.value, out var targetGroup, out var handler);
            if (@fixed.value)
            {
                virtualCamera.LookAt = null;
                virtualCamera.transform.rotation =
                    Quaternion.LookRotation(handler.position - virtualCamera.transform.position);
                virtualCamera.DestroyCinemachineComponent<CinemachineHardLookAt>();
            }
            else
            {
                virtualCamera.LookAt = handler.transform;
                var hardLookAt = virtualCamera.GetCinemachineComponent<CinemachineHardLookAt>();
                if (!hardLookAt)
                {
                    virtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
                }
            }

            virtualCamera.gameObject.SetActive(true);
            EndAction();
            return;

            Transform GetAgentLookAtPosition(CharacterObject character)
            {
                return position.value switch
                {
                    LookAtPosition.Top => character.Position.Top,
                    LookAtPosition.Center => character.Position.Center,
                    LookAtPosition.Bottom => character.Position.Bottom,
                    _ => character.Position.Bottom,
                };
            }
        }
    }
}