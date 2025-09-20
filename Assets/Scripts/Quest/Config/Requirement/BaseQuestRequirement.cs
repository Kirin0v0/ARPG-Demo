using System;
using System.Collections.Generic;
using System.Linq;
using Quest.Config.Goal;
using Quest.Data;
using Quest.Runtime;
using Sirenix.OdinInspector;

namespace Quest.Config.Requirement
{
    [Serializable]
    public abstract class BaseQuestRequirement
    {
        [InlineButton("ResetId")] public string id = Guid.NewGuid().ToString();
        
        public abstract void Update(QuestRequirement requirement, float deltaTime); // 更新需求函数节点

        private void ResetId()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}