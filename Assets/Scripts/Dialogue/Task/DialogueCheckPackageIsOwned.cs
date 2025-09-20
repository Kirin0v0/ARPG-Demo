using NodeCanvas.Framework;
using Package;
using ParadoxNotion.Design;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Package")]
    public class DialogueCheckPackageIsOwned: ConditionTask
    {
        [RequiredField] public BBParameter<int> id;

        private PackageManager _packageManager;

        private PackageManager PackageManager
        {
            get
            {
                if (_packageManager == null)
                {
                    _packageManager = GameEnvironment.FindEnvironmentComponent<PackageManager>();
                }

                return _packageManager;
            }
        }
        
        protected override string info => $"Check package(id={id}) is owned by player";
        
        
        protected override bool OnCheck()
        {
            if (!PackageManager)
            {
                return false;
            }

            var count = PackageManager.GetPackageCount(id.value);
            return count > 0;
        }
    }
}