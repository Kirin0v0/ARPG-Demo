using Character;
using NodeCanvas.Framework;
using Package;
using ParadoxNotion.Design;
using Quest;
using UnityEngine;
using VContainer;

namespace Dialogue.Task
{
    [Category("Package")]
    public class DialogueAddPackage : ActionTask
    {
        [RequiredField] public BBParameter<int> id;
        [RequiredField] public BBParameter<int> number;

        private PackageManager _packageManager;

        private PackageManager PackageManager
        {
            get
            {
                if (_packageManager)
                {
                    return _packageManager;
                }

                _packageManager = GameEnvironment.FindEnvironmentComponent<PackageManager>();
                return _packageManager;
            }
        }

        protected override string info => $"Add package {id}, number: {number}";

        protected override void OnExecute()
        {
            if (!PackageManager)
            {
                EndAction();
                return;
            }

            PackageManager.AddPackage(id.value, number.value, false);
            EndAction();
        }
    }
}