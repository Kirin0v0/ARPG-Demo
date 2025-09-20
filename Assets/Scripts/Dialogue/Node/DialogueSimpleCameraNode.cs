using Camera;
using Cinemachine;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Node
{
    [Name("Simple Camera")]
    [ParadoxNotion.Design.Description("Set the simple camera by the selected Dialogue Actors.")]
    public class DialogueSimpleCameraNode : DTNode
    {
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

        [SerializeField] private string _followActorName;
        [SerializeField] private string _followActorParameterID;
        [SerializeField] private string _lookAtActorName;
        [SerializeField] private string _lookAtActorParameterID;

        public string FollowActorName
        {
            get
            {
                var result = DLGTree.GetParameterByID(_followActorParameterID);
                return result != null ? result.name : _followActorName;
            }
            private set
            {
                if (_followActorName != value && !string.IsNullOrEmpty(value))
                {
                    _followActorName = value;
                    var param = DLGTree.GetParameterByName(value);
                    _followActorParameterID = param?.ID;
                }
            }
        }

        public IDialogueActor FollowActor
        {
            get
            {
                var result = DLGTree.GetActorReferenceByID(_followActorParameterID);
                return result ?? DLGTree.GetActorReferenceByName(_followActorName);
            }
        }

        public string LookAtActorName
        {
            get
            {
                var result = DLGTree.GetParameterByID(_lookAtActorParameterID);
                return result != null ? result.name : _lookAtActorName;
            }
            private set
            {
                if (_lookAtActorName != value && !string.IsNullOrEmpty(value))
                {
                    _lookAtActorName = value;
                    var param = DLGTree.GetParameterByName(value);
                    _lookAtActorParameterID = param?.ID;
                }
            }
        }

        public IDialogueActor LookAtActor
        {
            get
            {
                var result = DLGTree.GetActorReferenceByID(_lookAtActorParameterID);
                return result ?? DLGTree.GetActorReferenceByName(_lookAtActorName);
            }
        }

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
            if (FollowActor is not DialogueCharacterActor characterActor)
            {
                virtualCamera.Follow = null;
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
            CameraManager.CreateDialogueCameraTargetGroupAndHandler(
                new (Transform target, float weight, float radius)[] { (target, 1, 0) },
                followOffset,
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
            if (LookAtActor is not DialogueCharacterActor characterActor)
            {
                virtualCamera.LookAt = null;
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
            CameraManager.CreateDialogueCameraTargetGroupAndHandler(
                new (Transform target, float weight, float radius)[] { (target, 1, 0) },
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
        protected override void OnNodeInspectorGUI()
        {
            GUI.backgroundColor = Colors.lightBlue;
            FollowActorName = EditorUtils.Popup<string>("Follow", FollowActorName, DLGTree.definedActorParameterNames);
            LookAtActorName = EditorUtils.Popup<string>("LookAt", LookAtActorName, DLGTree.definedActorParameterNames);
            GUI.backgroundColor = Color.white;
            base.OnNodeInspectorGUI();
        }

        protected override void OnNodeGUI()
        {
            GUILayout.BeginVertical(Styles.roundedBox);
            GUILayout.Label($"Set camera {cameraId} that follows {FollowActorName} and looks at {LookAtActorName}");
            GUILayout.EndVertical();
        }
#endif
    }
}