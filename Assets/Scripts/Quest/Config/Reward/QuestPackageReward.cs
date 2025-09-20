using System;
using Package;
using UnityEngine;
using VContainer;

namespace Quest.Config.Reward
{
    [Serializable]
    public class QuestPackageReward: BaseQuestReward
    {
        [SerializeField] private int packageId;
        [SerializeField] private int packageNumber;

        [Inject] private PackageManager _packageManager;
        
        public override void GiveQuestReward()
        {
            _packageManager.AddPackage(packageId, packageNumber, false);
        }
    }
}