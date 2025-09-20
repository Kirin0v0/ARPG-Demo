using System;
using Character;
using Package;
using Quest;
using UnityEngine;
using VContainer;

namespace Trade.Config.PriceFluctuationRule
{
    [Serializable]
    public class TradeSlotPriceFluctuationOwnPackageConditionalRule : TradeSlotPriceFluctuationConditionalRule
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

        protected override bool ValidateCondition(CharacterObject self, CharacterObject target)
        {
            if (PackageManager == null)
            {
                return false;
            }

            return PackageManager.GetPackageCount(packageId) >= number;
        }
    }
}