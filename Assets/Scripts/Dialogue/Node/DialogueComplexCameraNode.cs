using System;
using System.Collections.Generic;
using System.Linq;
using Camera;
using Cinemachine;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Dialogue.Node
{
    [Name("Complex Camera")]
    [ParadoxNotion.Design.Description("Set the complex camera by the selected Dialogue Actors.")]
    public class DialogueComplexCameraNode : DTNode
    {
        [Serializable]
        public class CameraTargetMember
        {
            public string actorName;
            public string actorParameterID;
            public float weight;
            public float radius;

            public void SetActorName(DialogueTree dialogueTree, string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return;
                }

                actorName = name;
                actorParameterID = dialogueTree.GetParameterByName(name)?.ID;
            }

            public string GetActorName(DialogueTree dialogueTree)
            {
                var result = dialogueTree.GetParameterByID(actorParameterID);
                return result != null ? result.name : actorName;
            }

            public IDialogueActor GetActor(DialogueTree dialogueTree)
            {
                var result = dialogueTree.GetActorReferenceByID(actorParameterID);
                return result ?? dialogueTree.GetActorReferenceByName(actorName);
            }
        }

        public enum FollowPosition
        {
            Top,
            Center,
            Bottom,
        }

        public enum LookAtPosition
        {
            Top,
            Center,
            Bottom,
        }

        [SerializeField] private List<CameraTargetMember> _followMembers = new();
        [SerializeField] private List<CameraTargetMember> _lookAtMembers = new();

        [SerializeField] public string cameraId = "";

        [SerializeField] public FollowPosition followPosition = FollowPosition.Center;
        [SerializeField] public Vector3 followOffset = Vector3.zero;
        [SerializeField] public bool fixedFollow = false;

        [SerializeField] public LookAtPosition lookAtPosition = LookAtPosition.Center;
        [SerializeField] public Vector3 lookAtOffset = Vector3.zero;
        [SerializeField] public bool fixedLookAt = false;

        private CameraManager _cameraManager;

        private CameraManager CameraManager
        {
            get
            {
                if (_cameraManager)
                {
                    return _cameraManager;
                }

                _cameraManager = GameEnvironment.FindEnvironmentComponent<CameraManager>();
                return _cameraManager;
            }
        }

        public override bool requireActorSelection => false;

        protected override Status OnExecute(Component agent, IBlackboard blackboard)
        {
            if (!CameraManager)
            {
                DLGTree.Continue();
                return Status.Success;
            }

            var virtualCamera = CameraManager.CreateDialogueAppendCamera(cameraId);
            virtualCamera.gameObject.SetActive(false);

            virtualCamera.gameObject.SetActive(true);
            SetCameraFollow(virtualCamera);
            SetCameraLookAt(virtualCamera);
            DLGTree.Continue();
            return Status.Success;
        }

        private void SetCameraFollow(CinemachineVirtualCamera virtualCamera)
        {
            var followMembers = new List<(Transform target, float weight, float radius)>();
            _followMembers.ForEach(member =>
            {
                if (member.GetActor(DLGTree) is not DialogueCharacterActor characterActor)
                {
                    return;
                }

                var agent = characterActor.Reference.Value;
                var target = followPosition switch
                {
                    FollowPosition.Top => agent.Position.Top,
                    FollowPosition.Center => agent.Position.Center,
                    FollowPosition.Bottom => agent.Position.Bottom,
                    _ => agent.Position.Bottom,
                };
                followMembers.Add(new()
                {
                    target = target,
                    weight = member.weight,
                    radius = member.radius,
                });
            });
            if (followMembers.Count == 0)
            {
                virtualCamera.Follow = null;
                return;
            }

            CameraManager.CreateDialogueCameraTargetGroupAndHandler(
                followMembers.ToArray()
                , followOffset,
                out var targetGroup,
                out var handler
            );
            if (fixedFollow)
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
        }

        private void SetCameraLookAt(CinemachineVirtualCamera virtualCamera)
        {
            var lookAtMembers =  new List<(Transform target, float weight, float radius)>();
            _lookAtMembers.ForEach(member =>
            {
                if (member.GetActor(DLGTree) is not DialogueCharacterActor characterActor)
                {
                    return;
                }

                var agent = characterActor.Reference.Value;
                var target = lookAtPosition switch
                {
                    LookAtPosition.Top => agent.Position.Top,
                    LookAtPosition.Center => agent.Position.Center,
                    LookAtPosition.Bottom => agent.Position.Bottom,
                    _ => agent.Position.Bottom,
                };
                lookAtMembers.Add(new()
                {
                    target = target,
                    weight = member.weight,
                    radius = member.radius,
                });
            });
            if (lookAtMembers.Count == 0)
            {
                virtualCamera.LookAt = null;
                return;
            }

            CameraManager.CreateDialogueCameraTargetGroupAndHandler(
                lookAtMembers.ToArray(),
                lookAtOffset,
                out var targetGroup,
                out var handler
            );
            if (fixedLookAt)
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
        }

#if UNITY_EDITOR
        private bool _foldOutFollow = false;
        private bool _foldOutLookAt = false;

        protected override void OnNodeInspectorGUI()
        {
            GUI.backgroundColor = Colors.lightBlue;

            // 绘制Follow列表
            _foldOutFollow = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOutFollow, "Follow Members");
            if (_foldOutFollow)
            {
                _followMembers.ForEach((member, index) =>
                {
                    var actorName = EditorUtils.Popup<string>(
                        $"Follow Member {index + 1}",
                        member.GetActorName(DLGTree),
                        DLGTree.definedActorParameterNames
                    );
                    member.SetActorName(DLGTree, actorName);
                    EditorGUILayout.BeginHorizontal();
                    member.weight = EditorGUILayout.DelayedFloatField("Weight", member.weight);
                    member.radius = EditorGUILayout.DelayedFloatField("Radius", member.radius);
                    EditorGUILayout.EndHorizontal();
                });
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // 展示Follow的添加按钮
            if (GUILayout.Button("Add Follow Member"))
            {
                _followMembers.Add(new CameraTargetMember
                {
                    actorName = DialogueTree.INSTIGATOR_NAME,
                    actorParameterID = DLGTree.GetParameterByName(DialogueTree.INSTIGATOR_NAME)?.ID,
                    weight = 1,
                    radius = 0
                });
            }

            // 展示Follow的删除按钮
            if (_followMembers.Count > 0 && GUILayout.Button("Remove Follow Member"))
            {
                _followMembers.RemoveAt(_followMembers.Count - 1);
            }

            // 绘制LookAt列表
            _foldOutLookAt = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOutLookAt, "Look at Members");
            if (_foldOutLookAt)
            {
                _lookAtMembers.ForEach((member, index) =>
                {
                    var actorName = EditorUtils.Popup<string>(
                        $"Follow Member {index + 1}",
                        member.GetActorName(DLGTree),
                        DLGTree.definedActorParameterNames
                    );
                    member.SetActorName(DLGTree, actorName);
                    EditorGUILayout.BeginHorizontal();
                    member.weight = EditorGUILayout.DelayedFloatField("Weight", member.weight);
                    member.radius = EditorGUILayout.DelayedFloatField("Radius", member.radius);
                    EditorGUILayout.EndHorizontal();
                });
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // 展示LookAt的添加按钮
            if (GUILayout.Button("Add Look at Member"))
            {
                _lookAtMembers.Add(new CameraTargetMember
                {
                    actorName = DialogueTree.INSTIGATOR_NAME,
                    actorParameterID = DLGTree.GetParameterByName(DialogueTree.INSTIGATOR_NAME)?.ID,
                    weight = 1,
                    radius = 0
                });
            }

            // 展示LookAt的删除按钮
            if (_lookAtMembers.Count > 0 && GUILayout.Button("Remove Look at Member"))
            {
                _lookAtMembers.RemoveAt(_lookAtMembers.Count - 1);
            }

            GUI.backgroundColor = Color.white;
            base.OnNodeInspectorGUI();
        }

        protected override void OnNodeGUI()
        {
            GUILayout.BeginVertical(Styles.roundedBox);
            GUILayout.Label(
                $"Set camera {cameraId} that follows ({string.Join(", ", _followMembers.Select(member => member.GetActorName(DLGTree)))}) and looks at ({string.Join(", ", _lookAtMembers.Select(member => member.GetActorName(DLGTree)))})");
            GUILayout.EndVertical();
        }
#endif
    }
}