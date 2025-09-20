using System;
using System.Collections.Generic;
using Player;
using Quest.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Quest.Config.Requirement
{
    [Serializable]
    public class QuestMoneyRequirement: BaseQuestRequirement
    {
        // 预定义占位符，将占位符名称映射到对应的属性获取方法
        private static readonly
            Dictionary<string, (string placeHolderMeaning, Func<QuestMoneyRequirement, string> placeHolderOutput)>
            PlaceHolderDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "targetLevel", ("玩家目标金钱", goal => goal.money.ToString())
                },
                { "currentLevel", ("玩家当前金钱", goal => goal.PlayerDataManager.Money.ToString()) },
                // 后续可根据需要添加其他属性
            };

        // 占位符格式化器
        private static readonly QuestPlaceHolderFormatter<QuestMoneyRequirement> PlaceHolderFormatter =
            new(PlaceHolderDefinitions);

        [Title("需求满足条件")] [SerializeField] private int money;

        [Title("需求动态描述")] [SerializeField, TextArea]
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
        
        public override void Update(QuestRequirement requirement, float deltaTime)
        {
            requirement.description = FormatDescription();
            requirement.meet = PlayerDataManager.Money >= money;
        }

        [Button("更新描述预览")]
        private void RefreshDescriptionPreview()
        {
            preview = FormatDescription();
        }

        private string FormatDescription()
        {
            return PlaceHolderFormatter.Format(description, this);
        }
    }
}