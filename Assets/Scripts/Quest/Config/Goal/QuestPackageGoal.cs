using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Package;
using Package.Data;
using Quest.Data;
using Quest.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Quest.Config.Goal
{
    [Serializable]
    public class QuestPackageGoal : BaseQuestGoal
    {
        // 预定义占位符，将占位符名称映射到对应的属性获取方法
        private static readonly
            Dictionary<string, (string placeHolderMeaning, Func<QuestPackageGoal, string> placeHolderOutput)>
            PlaceHolderDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "packageName", ("物品名称", goal =>
                    {
                        var packageInfoData = goal.GetPackageInfoData();
                        return packageInfoData.Name;
                    })
                },
                { "packageGoalNumber", ("物品目标数量", goal => goal.packageNumber.ToString()) },
                {
                    "packageCurrentNumber",
                    ("物品当前数量", goal => goal.PackageManager.GetPackageCount(goal.packageId).ToString())
                },
                // 后续可根据需要添加其他属性
            };

        // 占位符格式化器
        private static readonly QuestPlaceHolderFormatter<QuestPackageGoal> PlaceHolderFormatter =
            new(PlaceHolderDefinitions);

        [Title("目标完成条件")] [SerializeField] private int packageId;
        [SerializeField] private int packageNumber;
        public int PackageId => packageId;
        public int PackageNumber => packageNumber;

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
        private string packageInfoBinaryPath = "";
#endif

        [InfoBox("这里会展示描述的实际输出")]
        [ShowInInspector, ReadOnly]
        private string preview { set; get; }

        [Inject] private PackageManager _packageManager;

        private PackageManager PackageManager
        {
            get
            {
                if (!_packageManager)
                {
                    _packageManager = GameEnvironment.FindEnvironmentComponent<PackageManager>();
                }

                return _packageManager;
            }
        }

        protected override void OnStart(QuestGoal goal)
        {
        }

        protected override void OnUpdate(QuestGoal goal, float deltaTime)
        {
            goal.completed = PackageManager.GetPackageCount(packageId) >= packageNumber;
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

        private PackageInfoData GetPackageInfoData()
        {
            PackageInfoContainer packageInfoContainer;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                packageInfoContainer =
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
            }
            else
            {
                ExcelBinaryManager.LoadContainer<PackageInfoContainer, PackageInfoData>(
                    Path.Combine(Application.streamingAssetsPath, packageInfoBinaryPath));
                packageInfoContainer = ExcelBinaryManager.GetContainer<PackageInfoContainer>();
            }
#else
            packageInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
#endif
            if (!packageInfoContainer.Data.TryGetValue(packageId, out var packageInfoData))
            {
                throw new Exception(
                    $"The PackageInfoData whose id is {packageId} is not existed in PackageInfoContainer");
            }

            return packageInfoData;
        }

        [Button("更新描述预览")]
        private void RefreshDescriptionPreview()
        {
            preview = FormatDescription();
        }
    }
}