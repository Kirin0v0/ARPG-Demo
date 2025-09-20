using NodeCanvas.Framework;
using Package;
using ParadoxNotion.Design;
using Quest;
using Quest.Config.Goal;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Dialogue.Task
{
    /// <summary>
    /// 提交任务物品的对话动作类，与QuestPackageGoal类强挂钩，使用时一定要保证存在对应的Goal
    /// </summary>
    [Category("Quest")]
    public class DialogueHandInQuestPackage : ActionTask
    {
        [RequiredField] public BBParameter<string> questId;
        [RequiredField] public BBParameter<string> goalId;

        private QuestManager _questManager;

        private QuestManager QuestManager
        {
            get
            {
                if (_questManager)
                {
                    return _questManager;
                }

                _questManager = GameEnvironment.FindEnvironmentComponent<QuestManager>();
                return _questManager;
            }
        }

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

        protected override string info => $"Hand in quest({questId}) package by goal({goalId?.value?.Truncate(5) ?? ""})";

        protected override void OnExecute()
        {
            if (!QuestManager || !PackageManager)
            {
                EndAction();
                return;
            }

            if (!QuestManager.QuestPool.TryGetQuestConfig(questId.value, out var questConfig))
            {
                EndAction();
                return;
            }

            foreach (var step in questConfig.steps)
            {
                foreach (var goal in step.goals)
                {
                    if (goal.id == goalId.value && goal is QuestPackageGoal packageGoal)
                    {
                        PackageManager.DeletePackage(packageGoal.PackageId, packageGoal.PackageNumber);
                        EndAction();
                        return;
                    }
                }
            }

            EndAction();
        }
    }
}