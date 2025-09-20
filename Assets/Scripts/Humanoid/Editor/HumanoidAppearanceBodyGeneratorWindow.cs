using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Character.Data.Extension;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Humanoid.Data;
using Humanoid.Editor.Data;
using Humanoid.Editor.Data.Extension;
using Humanoid.Model;
using Humanoid.Model.Data;
using Humanoid.Model.Extension;
using OfficeOpenXml;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Humanoid.Editor
{
    public class HumanoidAppearanceBodyGeneratorWindow : EditorWindow
    {
        private static readonly HumanoidAppearanceBodyUIData Default = new HumanoidAppearanceBodyUIData()
        {
            Id = 0,
            Races = HumanoidAppearanceRace.None,
            Models = new List<HumanoidModelInfoData>(),
            Color = HumanoidModelColor.DefaultBodyColor.ToAppearanceColor(),
        };

        private bool _init = false;
        private readonly ExcelBinaryManager _excelBinaryManager = new();
        private Vector2 _scrollPosition;

        // 导入相关
        private string _importModelInfoBinaryPath;
        private HumanoidModelInfoContainer _importModelInfoContainer;
        private readonly List<HumanoidModelInfoData> _importModelInfos = new();
        private GameObject _appearanceModelPrefab;

        private GameObject AppearanceModelPrefab
        {
            get => _appearanceModelPrefab;
            set
            {
                if (_appearanceModelPrefab == value)
                {
                    return;
                }

                _appearanceModelPrefab = value;
                RefreshConfigurableModels();
            }
        }

        private readonly Dictionary<string, HashSet<HumanoidModelRecord>> _configurableModels = new();

        // 配置相关
        private HumanoidAppearanceBodyUIData _configurationData;

        // 预览相关
        private Material _appearanceMaterial;
        private Vector3 _cameraOffset;
        private GameObject _appearanceInstance;

        // 读取和保存相关
        private string _loadBodyInfoBinaryPath;
        private bool _loadBodyFoldout;
        private bool _configurationBodyFoldout;
        private readonly List<HumanoidAppearanceBodyInfoData> _loadBodyInfos = new();
        private readonly List<HumanoidAppearanceBodyInfoData> _configurationBodyInfos = new();

        // 导出相关
        private string _exportBodyExcelPath;
        private string _exportSheetName;
        private int _exportStartRowNumber;

        [MenuItem("Tools/Game/Appearance/Body Appearance Generator")]
        public static void ShowAppearanceBodyGeneratorWindow()
        {
            var window = GetWindow<HumanoidAppearanceBodyGeneratorWindow>("Body Appearance Generator");
        }

        private void OnEnable()
        {
            // 初始化数据
            if (_init)
            {
                return;
            }

            _init = true;
            _scrollPosition = Vector2.zero;

            _configurationData = Default;
            _importModelInfoBinaryPath = $"{Application.streamingAssetsPath}";
            _importModelInfos.Clear();
            AppearanceModelPrefab = null;
            _cameraOffset = new(2f, 1.3f, 0f);
            _appearanceMaterial = null;
            _loadBodyInfoBinaryPath = $"{Application.streamingAssetsPath}";
            _loadBodyFoldout = false;
            _configurationBodyFoldout = false;
            _loadBodyInfos.Clear();
            _configurationBodyInfos.Clear();
            _exportBodyExcelPath = $"{Application.dataPath}/Configurations/";
            _exportSheetName = "HumanoidAppearanceBodyInfo";
            _exportStartRowNumber = 5;
        }

        private void OnGUI()
        {
            _configurationData ??= Default;

            EditorGUIUtility.labelWidth = 100f;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            ShowImportArea();
            EditorGUILayout.Separator();
            ShowConfigurationArea();
            EditorGUILayout.Separator();
            ShowPreviewArea();
            EditorGUILayout.Separator();
            ShowLoadAndSaveArea();
            EditorGUILayout.Separator();
            ShowExportArea();

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            if (_appearanceInstance)
            {
                DestroyImmediate(_appearanceInstance);
            }
        }

        private void ShowImportArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "导入",
                titleStyle
            );

            // 展示导入文件区域
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导入ModelInfo二进制文件");
            EditorGUILayout.TextField(_importModelInfoBinaryPath);
            if (GUILayout.Button("选择文件"))
            {
                _importModelInfoBinaryPath =
                    EditorUtility.OpenFilePanel("选择二进制文件", _importModelInfoBinaryPath, "bin");
            }

            if (GUILayout.Button("加载数据"))
            {
                ImportModelBinaryFile();
                RefreshConfigurableModels();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("外观模型预设体");
            AppearanceModelPrefab = (GameObject)EditorGUILayout.ObjectField(AppearanceModelPrefab,
                typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            return;

            // 导入文件并读取数据
            void ImportModelBinaryFile()
            {
                _excelBinaryManager.LoadContainer<HumanoidModelInfoContainer, HumanoidModelInfoData>(
                    _importModelInfoBinaryPath, true);
                _importModelInfoContainer = _excelBinaryManager.GetContainer<HumanoidModelInfoContainer>();
                _importModelInfos.Clear();
                _importModelInfos.AddRange(_importModelInfoContainer.Data.Values);
            }
        }

        private void ShowConfigurationArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "配置",
                titleStyle
            );

            EditorGUILayout.BeginHorizontal();
            _configurationData.Id = EditorGUILayout.IntField("Id", _configurationData.Id);
            var lastRaces = _configurationData.Races;
            _configurationData.Races =
                (HumanoidAppearanceRace)EditorGUILayout.EnumFlagsField("外观适用种族", _configurationData.Races);
            var currentRaces = _configurationData.Races;
            if (lastRaces != currentRaces)
            {
                RefreshConfigurableModels();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Color.SkinColor =
                EditorGUILayout.ColorField("皮肤颜色", _configurationData.Color.SkinColor);
            _configurationData.Color.HairColor =
                EditorGUILayout.ColorField("头发颜色", _configurationData.Color.HairColor);
            _configurationData.Color.StubbleColor =
                EditorGUILayout.ColorField("发角/胡茬颜色", _configurationData.Color.StubbleColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Color.EyesColor =
                EditorGUILayout.ColorField("眼睛颜色", _configurationData.Color.EyesColor);
            _configurationData.Color.ScarColor =
                EditorGUILayout.ColorField("伤疤颜色", _configurationData.Color.ScarColor);
            _configurationData.Color.BodyArtColor =
                EditorGUILayout.ColorField("身体彩绘颜色", _configurationData.Color.BodyArtColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Color.PrimaryColor =
                EditorGUILayout.ColorField("主要颜色", _configurationData.Color.PrimaryColor);
            _configurationData.Color.SecondaryColor =
                EditorGUILayout.ColorField("次要颜色", _configurationData.Color.SecondaryColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Color.MetalPrimaryColor =
                EditorGUILayout.ColorField("金属主要颜色", _configurationData.Color.MetalPrimaryColor);
            _configurationData.Color.MetalSecondaryColor =
                EditorGUILayout.ColorField("金属次要颜色", _configurationData.Color.MetalSecondaryColor);
            _configurationData.Color.MetalDarkColor =
                EditorGUILayout.ColorField("金属暗部颜色", _configurationData.Color.MetalDarkColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Color.LeatherPrimaryColor =
                EditorGUILayout.ColorField("皮革主要颜色", _configurationData.Color.LeatherPrimaryColor);
            _configurationData.Color.LeatherSecondaryColor =
                EditorGUILayout.ColorField("皮革次要颜色", _configurationData.Color.LeatherSecondaryColor);
            EditorGUILayout.EndHorizontal();

            ShowModelListArea();

            void ShowModelListArea()
            {
                if (_importModelInfos.Count == 0 || !AppearanceModelPrefab)
                {
                    EditorGUILayout.HelpBox("请导入ModelInfo二进制文件和外观模型预设体", MessageType.Warning);
                }

                _configurableModels.ForEach(pair =>
                {
                    var configurableModelRecords = pair.Value.ToList();

                    EditorGUILayout.BeginHorizontal();

                    // 查找当前模型预设体可配置的部位的模型列表是否存在已配置的模型，是则直接展示为选中模型，否则仅展示默认模型
                    var configurationModelInfo =
                        _configurationData.Models.Find(modelInfo => modelInfo.Part == pair.Key);
                    int index;
                    if (configurationModelInfo == null)
                    {
                        index = -1;
                    }
                    else
                    {
                        var configurationModelRecord = configurationModelInfo.ToModelRecord();
                        index = configurableModelRecords.FindIndex(modelRecord =>
                            modelRecord == configurationModelRecord);
                    }

                    var toggle = EditorGUILayout.ToggleLeft("设置部位", index != -1);
                    EditorGUILayout.LabelField(pair.Key);
                    var selectedIndex = EditorGUILayout.Popup(index == -1 ? 0 : index,
                        configurableModelRecords.Select(modelInfo => modelInfo.Name).ToArray());
                    // 如果有选中则添加到配置列表中，并删除旧数据
                    if (toggle)
                    {
                        var selectedModelRecord = configurableModelRecords[selectedIndex];
                        var selectedModelInfo =
                            _importModelInfos.Find(modelInfo => modelInfo.ToModelRecord() == selectedModelRecord);
                        if (selectedModelInfo != null)
                        {
                            _configurationData.Models.Remove(configurationModelInfo);
                            _configurationData.Models.Add(selectedModelInfo);
                        }
                        else
                        {
                            throw new Exception(
                                $"The modelInfo table doesn't have the model({selectedModelRecord.ToString()})");
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                });
            }
        }

        private void ShowPreviewArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "预览",
                titleStyle
            );

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("外观材质");
            _appearanceMaterial = (Material)EditorGUILayout.ObjectField(_appearanceMaterial, typeof(Material), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("相机偏移量");
            _cameraOffset = EditorGUILayout.Vector3Field("", _cameraOffset);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("在当前场景中预览"))
            {
                if (AppearanceModelPrefab == null)
                {
                    throw new Exception("You can't use the preview function unless prefab is ready");
                }

                // 在场景中创建预设体实例
                if (_appearanceInstance)
                {
                    GameObject.DestroyImmediate(_appearanceInstance);
                }

                _appearanceInstance = Instantiate(AppearanceModelPrefab);
                var modelLoader = new HumanoidModelLoader(_appearanceInstance, _appearanceMaterial);

                _configurationData.Models.ForEach(modelInfo =>
                {
                    var modelRecord = modelInfo.ToModelRecord();
                    // 过滤之前设置的不满足种族的模型
                    if (_configurationData.Races.MatchGenderRestriction(modelRecord.GenderRestriction))
                    {
                        modelLoader.ShowModel(modelRecord.Type, modelRecord.Part, modelRecord.GenderRestriction,
                            modelRecord.Name, _configurationData.Color.ToModelColor());
                    }
                });

                // 同时将摄像机挪至外观实例前方
                var mainCamera = UnityEngine.Camera.main;
                if (mainCamera)
                {
                    mainCamera.transform.position = _appearanceInstance.transform.TransformPoint(_cameraOffset);
                }
            }
        }

        private void ShowLoadAndSaveArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "加载/保存",
                titleStyle
            );

            // 展示加载文件区域
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("加载BodyInfo二进制文件");
            EditorGUILayout.TextField(_loadBodyInfoBinaryPath);
            if (GUILayout.Button("选择文件"))
            {
                _loadBodyInfoBinaryPath =
                    EditorUtility.OpenFilePanel("选择二进制文件", _loadBodyInfoBinaryPath, "bin");
            }

            if (GUILayout.Button("加载数据"))
            {
                _excelBinaryManager.LoadContainer<HumanoidAppearanceBodyInfoContainer, HumanoidAppearanceBodyInfoData>(
                    _loadBodyInfoBinaryPath, true);
                var appearanceBodyInfoContainer =
                    _excelBinaryManager.GetContainer<HumanoidAppearanceBodyInfoContainer>();
                _loadBodyInfos.Clear();
                _loadBodyInfos.AddRange(appearanceBodyInfoContainer.Data.Select(pair => pair.Value));
            }

            EditorGUILayout.EndHorizontal();

            // 加载的模型列表
            _loadBodyFoldout = EditorGUILayout.Foldout(_loadBodyFoldout, "已加载的BodyInfo列表");
            if (_loadBodyFoldout)
            {
                HumanoidAppearanceBodyInfoData deleteData = null;
                foreach (var bodyInfoData in _loadBodyInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(bodyInfoData.Id.ToString());
                    EditorGUILayout.LabelField(bodyInfoData.Races);
                    EditorGUILayout.LabelField(bodyInfoData.Models);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = bodyInfoData.ToUIData(_importModelInfoContainer);
                        RefreshConfigurableModels();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = bodyInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _loadBodyInfos.Remove(deleteData);
                }
            }

            if (GUILayout.Button("保存当前配置"))
            {
                // 过滤相同id模型
                if (_loadBodyInfos.Find(data => data.Id == _configurationData.Id) != null ||
                    _configurationBodyInfos.Find(data => data.Id == _configurationData.Id) != null)
                {
                    throw new Exception("Can't add the same id BodyInfo");
                }

                // 删除不满足种族的模型
                _configurationData.Models.RemoveAll(modelInfo => !_configurationData.Races.MatchGenderRestriction(
                    modelInfo.ToModelRecord()
                        .GenderRestriction));

                // 这里仅克隆值而不是使用引用
                _configurationBodyInfos.Add(_configurationData.ToInfoData());
                // 生成后id+1
                _configurationData.Id++;
            }

            // 配置的模型列表
            _configurationBodyFoldout =
                EditorGUILayout.Foldout(_configurationBodyFoldout, "已配置的BodyInfo列表");
            if (_configurationBodyFoldout)
            {
                HumanoidAppearanceBodyInfoData deleteData = null;
                foreach (var bodyInfoData in _configurationBodyInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(bodyInfoData.Id.ToString());
                    EditorGUILayout.LabelField(bodyInfoData.Races);
                    EditorGUILayout.LabelField(bodyInfoData.Models);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = bodyInfoData.ToUIData(_importModelInfoContainer);
                        RefreshConfigurableModels();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = bodyInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _configurationBodyInfos.Remove(deleteData);
                }
            }

            // 这里检查是否存在相同id的模型，是则提醒用户
            var bodyInfoIdSet = new HashSet<int>();
            foreach (var loadBodyInfo in _loadBodyInfos)
            {
                if (!bodyInfoIdSet.Add(loadBodyInfo.Id))
                {
                    throw new Exception("The BodyInfo list contains the same id");
                }
            }

            foreach (var configurationBodyInfo in _configurationBodyInfos)
            {
                if (!bodyInfoIdSet.Add(configurationBodyInfo.Id))
                {
                    throw new Exception("The BodyInfo list contains the same id");
                }
            }
        }

        private void ShowExportArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "导出",
                titleStyle
            );

            // 展示导出文件区域
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出至BodyInfo Excel文件");
            EditorGUILayout.TextField(_exportBodyExcelPath);
            if (GUILayout.Button("选择文件"))
            {
                _exportBodyExcelPath =
                    EditorUtility.OpenFilePanel("选择Excel文件", _exportBodyExcelPath, "xls,xlsx");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _exportSheetName = EditorGUILayout.TextField("Excel分页名称", _exportSheetName);
            _exportStartRowNumber = EditorGUILayout.IntField("起始行号", _exportStartRowNumber);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("导出"))
            {
                ExportToExcelFile();
            }

            void ExportToExcelFile()
            {
                var list = new List<HumanoidAppearanceBodyInfoData>();
                list.AddRange(_loadBodyInfos);
                list.AddRange(_configurationBodyInfos);
                list.Sort((a, b) => a.Id <= b.Id ? -1 : 1);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(_exportBodyExcelPath));

                // 查找或创建sheet
                var workbookWorksheet = package.Workbook.Worksheets[_exportSheetName];
                if (workbookWorksheet == null)
                {
                    workbookWorksheet = package.Workbook.Worksheets.Add(_exportSheetName);
                }

                // 写入数据
                for (var i = 0; i < list.Count; i++)
                {
                    var row = _exportStartRowNumber + i;
                    var bodyInfo = list[i];
                    workbookWorksheet.Cells[row, 1].Value = bodyInfo.Id;
                    workbookWorksheet.Cells[row, 2].Value = bodyInfo.Races;
                    workbookWorksheet.Cells[row, 3].Value = bodyInfo.Models;
                    workbookWorksheet.Cells[row, 4].Value = bodyInfo.SkinColor;
                    workbookWorksheet.Cells[row, 5].Value = bodyInfo.HairColor;
                    workbookWorksheet.Cells[row, 6].Value = bodyInfo.StubbleColor;
                    workbookWorksheet.Cells[row, 7].Value = bodyInfo.EyesColor;
                    workbookWorksheet.Cells[row, 8].Value = bodyInfo.ScarColor;
                    workbookWorksheet.Cells[row, 9].Value = bodyInfo.BodyArtColor;
                    workbookWorksheet.Cells[row, 10].Value = bodyInfo.PrimaryColor;
                    workbookWorksheet.Cells[row, 11].Value = bodyInfo.SecondaryColor;
                    workbookWorksheet.Cells[row, 12].Value = bodyInfo.MetalPrimaryColor;
                    workbookWorksheet.Cells[row, 13].Value = bodyInfo.MetalSecondaryColor;
                    workbookWorksheet.Cells[row, 14].Value = bodyInfo.MetalDarkColor;
                    workbookWorksheet.Cells[row, 15].Value = bodyInfo.LeatherPrimaryColor;
                    workbookWorksheet.Cells[row, 16].Value = bodyInfo.LeatherSecondaryColor;
                }

                // 保存数据
                package.Save();

                EditorUtility.DisplayDialog("导出结果", "成功！！！", "好的");
            }
        }

        private void RefreshConfigurableModels()
        {
            _configurableModels.Clear();

            if (!AppearanceModelPrefab || _configurationData.Races == HumanoidAppearanceRace.None)
            {
                return;
            }

            var modelLoader = new HumanoidModelLoader(AppearanceModelPrefab);
            modelLoader.GetModels(false).ForEach(pair =>
            {
                if (pair.modelRecord.Type != HumanoidModelType.Body)
                {
                    return;
                }

                if (!_configurationData.Races.MatchGenderRestriction(pair.modelRecord.GenderRestriction))
                {
                    return;
                }

                if (_importModelInfos.FindIndex(modelInfoData => modelInfoData.ToModelRecord() == pair.modelRecord) ==
                    -1)
                {
                    return;
                }

                if (_configurableModels.TryGetValue(pair.modelRecord.Part, out var hashSet))
                {
                    hashSet.Add(pair.modelRecord);
                }
                else
                {
                    hashSet = new HashSet<HumanoidModelRecord> { pair.modelRecord };
                    _configurableModels.Add(pair.modelRecord.Part, hashSet);
                }
            });
        }
    }
}