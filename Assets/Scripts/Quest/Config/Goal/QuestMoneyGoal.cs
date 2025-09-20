using System;
using System.Collections.Generic;
using System.Text;
using Framework.Common.Debug;
using Player;
using Quest.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Quest.Config.Goal
{
    [Serializable]
    public class QuestMoneyGoal : BaseQuestGoal
    {
        // 预定义占位符，将占位符名称映射到对应的属性获取方法
        private static readonly
            Dictionary<string, (string placeHolderMeaning, Func<QuestMoneyGoal, string> placeHolderOutput)>
            PlaceHolderDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "targetLevel", ("玩家目标金钱", goal => goal.money.ToString())
                },
                { "currentLevel", ("玩家当前金钱", goal => goal.PlayerDataManager.Money.ToString()) },
                // 后续可根据需要添加其他属性
            };

        // 占位符格式化器
        private static readonly QuestPlaceHolderFormatter<QuestMoneyGoal> PlaceHolderFormatter =
            new(PlaceHolderDefinitions);

        [Title("目标完成条件")] [SerializeField] private int money;

        [Title("目标动态描述")] [SerializeField, TextArea]
        private string description = "";

        [InfoBox("这里是预先定义好的占位符，可直接填入描述中，最终会动态输出为内部数据")]
        [ShowInInspector]
        private List<string> placeHolders => PlaceHolderFormatter.GetDefinitions();

        [InfoBox("这里会展示描述的实际输出")]
        [ShowInInspector, ReadOnly]
        private string preview { set; get; }

        [Inject] private PlayerDataManager _playerDataManager;

        private PlayerDataManager PlayerDataManager
        {
            get
            {
                if (!_playerDataManager)
                {
                    _playerDataManager = GameEnvironment.FindEnvironmentComponent<PlayerDataManager>();
                }

                return _playerDataManager;
            }
        }

        protected override void OnStart(QuestGoal goal)
        {
        }

        protected override void OnUpdate(QuestGoal goal, float deltaTime)
        {
            goal.completed = PlayerDataManager.Money >= money;
        }

        protected override void OnComplete(QuestGoal goal)
        {
        }

        protected override void OnInterrupt(QuestGoal goal)
        {
        }

        protected override string SerializeToState()
        {
            return "";
        }

        protected override void DeserializeFromState(string state)
        {
        }

        protected override string FormatDescription()
        {
            return PlaceHolderFormatter.Format(description, this);
        }

        [Button("更新描述预览")]
        private void RefreshDescriptionPreview()
        {
            preview = FormatDescription();
        }
    }
}