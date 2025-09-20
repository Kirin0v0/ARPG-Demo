using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Cinemachine;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Camera")]
    public class DialogueSetCameraFollowByAgents : BaseDialogueCameraTask
    {
        [Serializable]
        public class CameraTargetMember
        {
            public string name;
            public float weight;
            public float radius;
        }

        public enum FollowPosition
        {
            Top,
            Center,
            Bottom,
        }

        [ReferenceField] public BBParameter<float> selfWeight = new(1);
        [ReferenceField] public BBParameter<float> selfRadius = new(0);
        [RequiredField] public BBParameter<List<CameraTargetMember>> otherMembers = new(new List<CameraTargetMember>());
        [RequiredField] public BBParameter<FollowPosition> position = new(FollowPosition.Center);
        [RequiredField] public BBParameter<Vector3> offset = new(Vector3.zero);
        [RequiredField] public BBParameter<bool> @fixed = new(true);

        protected override string info
        {
            get
            {
                var agents = new List<string> { agentInfo };
                otherMembers.value?.ForEach(member => agents.Add(member.name));
                return $"Set camera {cameraId} to follow by agents({string.Join(", ", agents)})";
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
                    target = GetAgentFollowPosition(agent.Value),
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
                                target = GetAgentFollowPosition(dialogueCharacterActor.Reference.Value),
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
                virtualCamera.Follow = null;
                virtualCamera.transform.position = handler.position;
                virtualCamera.DestroyCinemachineComponent<CinemachineHardLockToTarget>();
            }
            else
            {
                virtualCamera.Follow = handler.transform;
                var hardLockToTarget = virtualCamera.GetCinemachineComponent<CinemachineHardLockToTarget>();
                if (!hardLockToTarget)
                {
                    virtualCamera.AddCinemachineComponent<CinemachineHardLockToTarget>();
                }
            }

            virtualCamera.gameObject.SetActive(true);
            EndAction();
            return;

            Transform GetAgentFollowPosition(CharacterObject character)
            {
                return position.value switch
                {
                    FollowPosition.Top => character.Position.Top,
                    FollowPosition.Center => character.Position.Center,
                    FollowPosition.Bottom => character.Position.Bottom,
                    _ => character.Position.Bottom,
                };
            }
        }
    }
}