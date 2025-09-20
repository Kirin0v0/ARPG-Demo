using System;
using Framework.Common.Debug;
using Player;
using UnityEngine;
using VContainer;

namespace Quest.Config.Reward
{
    [Serializable]
    public class QuestMoneyReward: BaseQuestReward
    {
        [SerializeField] private int money;

        [Inject] private PlayerDataManager _playerDataManager;
        
        public override void GiveQuestReward()
        {
            _playerDataManager.EarnMoney(money, true);
        }
    }
}