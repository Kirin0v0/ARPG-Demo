using System;
using Sirenix.OdinInspector;

namespace Quest.Config.Reward
{
    [Serializable]
    public abstract class BaseQuestReward
    {
        [InlineButton("ResetId")] public string id = Guid.NewGuid().ToString();
        
        public abstract void GiveQuestReward();

        private void ResetId()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}