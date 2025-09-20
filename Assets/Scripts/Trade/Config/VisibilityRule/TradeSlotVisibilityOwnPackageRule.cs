using System;
using Character;
using Package;
using UnityEngine;
using VContainer;

namespace Trade.Config.VisibilityRule
{
    [System.Serializable]
    public class TradeSlotVisibilityOwnPackageRule : BaseTradeSlotVisibilityRule
    {
        private PackageManager _packageManager;

        private PackageManager PackageManager
        {
            get
            {
                if (!_packageManager)
                {
                    _packageManager = GameEnvironment.FindEnvironmentComponent<PackageManager>();
                }

                return _packageManager;
            }
        }

        public int packageId;
        public int number;

        public override bool SetSlotVisibility(CharacterObject self, CharacterObject target)
        {
            if (PackageManager == null)
            {
                return false;
            }

            return PackageManager.GetPackageCount(packageId) >= number;
        }
    }
}