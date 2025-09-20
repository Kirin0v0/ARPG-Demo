using System;
using System.Collections.Generic;
using System.IO;
using Character;
using Character.Data;
using Common;
using Framework.Common.Excel;
using Player;
using Quest.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Quest.Config.Goal
{
    [Serializable]
    public class QuestKillRecordGoal : BaseQuestGoal
    {
        // 预定义占位符，将占位符名称映射到对应的属性获取方法
        private static readonly
            Dictionary<string, (string placeHolderMeaning, Func<QuestKillRecordGoal, string> placeHolderOutput)>
            PlaceHolderDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "targetName", ("对象名称", goal =>
                    {
                        var characterInfoData = goal.GetCharacterInfoData();
                        return characterInfoData.Name;
                    })
                },
                { "killGoalNumber", ("击杀目标数量", goal => goal.killNumber.ToString()) },
                {
                    "killCurrentNumber",
                    ("击杀当前数量", goal => goal._currentKillNumber.ToString())
                },
                // 后续可根据需要添加其他属性
            };

        // 占位符格式化器
        private static readonly QuestPlaceHolderFormatter<QuestKillRecordGoal> PlaceHolderFormatter =
            new(PlaceHolderDefinitions);

        [Title("目标完成条件")] [SerializeField] private string prototype;
        [SerializeField] private int killNumber;

        [Title("目标动态描述")] [SerializeField, TextArea]
        private string description = "";

        [InfoBox("这里是预先定义好的占位符，可直接填入描述中，最终会动态输出为内部数据")]
        [ShowInInspector]
        private List<string> placeHolders => PlaceHolderFormatter.GetDefinitions();

#if UNITY_EDITOR
        private ExcelBinaryManager _excelBinaryManager;

        private ExcelBinaryManager ExcelBinaryManager
        {
            set => _excelBinaryManager = value;
            get => _excelBinaryManager ??= new ExcelBinaryManager();
        }

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string characterInfoBinaryPath = "";
#endif

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

        private int _currentKillNumber;

        protected override void OnStart(QuestGoal goal)
        {
            _currentKillNumber = 0;
        }

        protected override void OnUpdate(QuestGoal goal, float deltaTime)
        {
            _currentKillNumber = PlayerDataManager.TryGetKillPrototypeRecord(prototype, out var number) ? number : 0;
            goal.completed = _currentKillNumber >= killNumber;
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

        private CharacterInfoData GetCharacterInfoData()
        {
            CharacterInfoContainer characterInfoContainer;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                characterInfoContainer =
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<CharacterInfoContainer>();
            }
            else
            {
                ExcelBinaryManager.LoadContainer<CharacterInfoContainer, CharacterInfoData>(
                    Path.Combine(Application.streamingAssetsPath, characterInfoBinaryPath));
                characterInfoContainer = ExcelBinaryManager.GetContainer<CharacterInfoContainer>();
            }
#else
            characterInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<CharacterInfoContainer>();
#endif
            if (!characterInfoContainer.Data.TryGetValue(prototype, out var characterInfoData))
            {
                throw new Exception(
                    $"The CharacterInfoData whose id is {prototype} is not existed in CharacterInfoContainer");
            }

            return characterInfoData;
        }

        private void RecordPlayerKillNumber(CharacterObject enemy)
        {
            if (enemy.Parameters.prototype != prototype)
            {
                return;
            }

            _currentKillNumber++;
        }

        [Button("更新描述预览")]
        private void RefreshDescriptionPreview()
        {
            preview = FormatDescription();
        }
    }
}