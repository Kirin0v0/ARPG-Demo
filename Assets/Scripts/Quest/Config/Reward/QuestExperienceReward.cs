using System;
using Player;
using UnityEngine;
using VContainer;

namespace Quest.Config.Reward
{
    [Serializable]
    public class QuestExperienceReward: BaseQuestReward
    {
        [SerializeField] private int experience;

        [Inject] private PlayerDataManager _playerDataManager;
        
        public override void GiveQuestReward()
        {
            _playerDataManager.AddExperience(experience);
        }
    }
}