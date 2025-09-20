using System;
using Quest.Data;
using Quest.Runtime;
using Sirenix.OdinInspector;

namespace Quest.Config.Goal
{
    [Serializable]
    public abstract class BaseQuestGoal
    {
        [InlineButton("ResetId")] public string id = Guid.NewGuid().ToString();

        /// <summary>
        /// 开始目标函数节点
        /// </summary>
        /// <param name="goal">目标运行时数据</param>
        public void Start(QuestGoal goal)
        {
            DeserializeFromState(goal.state);
            OnStart(goal);
        }

        /// <summary>
        /// 更新目标函数节点
        /// </summary>
        /// <param name="goal">目标运行时数据</param>
        /// <param name="deltaTime">更新时间间隔</param>
        public void Update(QuestGoal goal, float deltaTime)
        {
            OnUpdate(goal, deltaTime);
            goal.description = FormatDescription();
            goal.state = SerializeToState();
        }

        /// <summary>
        /// 完成目标函数节点
        /// </summary>
        /// <param name="goal">目标运行时数据</param>
        public void Complete(QuestGoal goal)
        {
            OnComplete(goal);
        }

        /// <summary>
        /// 中断目标函数节点
        /// </summary>
        /// <param name="goal">目标运行时数据</param>
        public void Interrupt(QuestGoal goal)
        {
            OnInterrupt(goal);
        }

        protected abstract void OnStart(QuestGoal goal);

        protected abstract void OnUpdate(QuestGoal goal, float deltaTime);

        protected abstract void OnComplete(QuestGoal goal);

        protected abstract void OnInterrupt(QuestGoal goal);

        protected abstract string SerializeToState(); // 将目标内部数据序列化为状态字符串的函数

        protected abstract void DeserializeFromState(string state); // 从状态字符串反序列化为目标内部数据的函数

        protected abstract string FormatDescription(); // 格式化描述函数

        private void ResetId()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}