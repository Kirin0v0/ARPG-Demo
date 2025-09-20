using ParadoxNotion.Design;

namespace Dialogue.Task
{
    [Category("Camera")]
    public class DialogueDestroyCamera: BaseDialogueCameraTask
    {
        protected override string info => $"Destroy camera {cameraId}";

        protected override void OnExecute()
        {
            DestroyCamera();
            EndAction();
        }
    }
}