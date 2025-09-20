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
    public class HumanoidAppearanceGearGeneratorWindow : EditorWindow
    {
        private static readonly HumanoidAppearanceGearUIData Default = new HumanoidAppearanceGearUIData()
        {
            Id = 0,
            Races = HumanoidAppearanceRace.HumanMale | HumanoidAppearanceRace.HumanFemale,
            Models = new List<HumanoidModelInfoData>(),
            Color = HumanoidModelColor.DefaultGearColor.ToAppearanceColor(),
        };

        private bool _init = false;
        private readonly ExcelBinaryManager _excelBinaryManager = new();
        private Vector2 _scrollPosition;

        // 导入相关
        private string _importModelInfoBinaryPath;
        private HumanoidModelInfoContainer _importModelInfoContainer;
        private readonly List<HumanoidModelInfoData> _importModelInfos = new();
        private GameObject _appearanceModelPrefab;

        public GameObject AppearanceModelPrefab
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
        private HumanoidAppearanceGearUIData _configurationData;

        // 预览相关
        private Material _appearanceMaterial;
        private Vector3 _cameraOffset;
        private GameObject _appearanceInstance;

        // 读取和保存相关
        private string _loadGearInfoBinaryPath;
        private bool _loadGearFoldout;
        private bool _configurationGearFoldout;
        private readonly List<HumanoidAppearanceGearInfoData> _loadGearInfos = new();
        private readonly List<HumanoidAppearanceGearInfoData> _configurationGearInfos = new();

        // 导出相关
        private string _exportGearExcelPath;
        private string _exportSheetName;
        private int _exportStartRowNumber;

        [MenuItem("Tools/Game/Appearance/Gear Appearance Generator")]
        public static void ShowAppearanceGearGeneratorWindow()
        {
            var window = GetWindow<HumanoidAppearanceGearGeneratorWindow>("Gear Appearance Generator");
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
            _appearanceModelPrefab = null;
            _cameraOffset = new(2f, 1.3f, 0f);
            _appearanceMaterial = null;
            _loadGearInfoBinaryPath = $"{Application.streamingAssetsPath}";
            _loadGearFoldout = false;
            _configurationGearFoldout = false;
            _loadGearInfos.Clear();
            _configurationGearInfos.Clear();
            _exportGearExcelPath = $"{Application.dataPath}/Configurations/";
            _exportSheetName = "HumanoidAppearanceGearInfo";
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
            _configurationData.Part =
                (HumanoidAppearanceGearPart)EditorGUILayout.EnumPopup("外观部位", _configurationData.Part);
            _configurationData.Mark = EditorGUILayout.TextField("外观备注", _configurationData.Mark);
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

                var configurableParts = _configurableModels.Keys.ToList();
                var deleteIndex = -1;
                for (var i = 0; i < _configurationData.Models.Count; i++)
                {
                    var info = _configurationData.Models[i];

                    // 如果可配置模型不存在已配置的模型的部位，则跳过配置
                    if (!_configurableModels.TryGetValue(info.Part, out var configurableModelHashSet))
                    {
                        continue;
                    }

                    // 如果可配置模型对应的部位不存在该配置模型，也跳过配置
                    var configurableModelRecords = configurableModelHashSet.ToList();
                    if (configurableModelRecords.FindIndex(modelRecord => modelRecord == info.ToModelRecord()) == -1)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();

                    var selectedPartIndex = configurableParts.FindIndex(part => part == info.Part);
                    selectedPartIndex = EditorGUILayout.Popup(selectedPartIndex == -1 ? 0 : selectedPartIndex,
                        configurableParts.ToArray());
                    var selectedPartModels = _configurableModels[configurableParts[selectedPartIndex]].ToList();
                    var selectedModelIndex =
                        selectedPartModels.FindIndex(modelInfo => modelInfo == info.ToModelRecord());
                    selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex == -1 ? 0 : selectedModelIndex,
                        selectedPartModels.Select(modelInfo => modelInfo.Name).ToArray());

                    var currentModelRecord = selectedPartModels[selectedModelIndex];
                    var currentModelInfo =
                        _importModelInfos.Find(modelInfo => modelInfo.ToModelRecord() == currentModelRecord);

                    // 这里直接替换模型信息
                    if (currentModelInfo != null)
                    {
                        info.Id = currentModelInfo.Id;
                        info.Type = currentModelInfo.Type;
                        info.Part = currentModelInfo.Part;
                        info.GenderRestriction = currentModelInfo.GenderRestriction;
                        info.Name = currentModelInfo.Name;
                    }
                    else
                    {
                        throw new Exception(
                            $"The modelInfo table doesn't have the model({currentModelRecord.ToString()})");
                    }

                    // 最后添加删除按钮，点击后记录索引
                    if (GUILayout.Button("删除"))
                    {
                        deleteIndex = i;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // 如果有删除操作则删除该行
                if (deleteIndex != -1)
                {
                    _configurationData.Models.RemoveAt(deleteIndex);
                }

                // 添加添加按钮，点击后添加行数
                if (GUILayout.Button("添加新行"))
                {
                    foreach (var pair in _configurableModels)
                    {
                        foreach (var modelRecord in pair.Value)
                        {
                            var newModelInfo =
                                _importModelInfos.Find(modelInfo => modelInfo.ToModelRecord() == modelRecord);
                            if (newModelInfo != null)
                            {
                                _configurationData.Models.Add(newModelInfo);
                                return;
                            }
                        }
                    }
                }
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
            EditorGUILayout.LabelField("加载GearInfo二进制文件");
            EditorGUILayout.TextField(_loadGearInfoBinaryPath);
            if (GUILayout.Button("选择文件"))
            {
                _loadGearInfoBinaryPath =
                    EditorUtility.OpenFilePanel("选择二进制文件", _loadGearInfoBinaryPath, "bin");
            }
            if (GUILayout.Button("加载数据"))
            {
                _excelBinaryManager.LoadContainer<HumanoidAppearanceGearInfoContainer, HumanoidAppearanceGearInfoData>(
                    _loadGearInfoBinaryPath, true);
                var appearanceGearInfoContainer =
                    _excelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();
                _loadGearInfos.Clear();
                _loadGearInfos.AddRange(appearanceGearInfoContainer.Data.Select(pair => pair.Value));
            }

            EditorGUILayout.EndHorizontal();

            // 加载的模型列表
            _loadGearFoldout = EditorGUILayout.Foldout(_loadGearFoldout, "已加载的GearInfo列表");
            if (_loadGearFoldout)
            {
                HumanoidAppearanceGearInfoData deleteData = null;
                foreach (var gearInfoData in _loadGearInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(gearInfoData.Id.ToString());
                    EditorGUILayout.LabelField(gearInfoData.Races);
                    EditorGUILayout.LabelField(gearInfoData.Models);
                    EditorGUILayout.LabelField(gearInfoData.Mark);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = gearInfoData.ToUIData(_importModelInfoContainer);
                        RefreshConfigurableModels();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = gearInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _loadGearInfos.Remove(deleteData);
                }
            }

            if (GUILayout.Button("保存当前配置"))
            {
                // 过滤相同id模型
                if (_loadGearInfos.Find(data => data.Id == _configurationData.Id) != null ||
                    _configurationGearInfos.Find(data => data.Id == _configurationData.Id) != null)
                {
                    throw new Exception("Can't add the same id GearInfo");
                }

                // 删除不满足种族的模型
                _configurationData.Models.RemoveAll(modelInfo => !_configurationData.Races.MatchGenderRestriction(
                    modelInfo.ToModelRecord()
                        .GenderRestriction));

                // 删除当前可配置模型不存在的部位和模型
                _configurationData.Models.RemoveAll(modelInfo =>
                {
                    if (_configurableModels.TryGetValue(modelInfo.Part, out var configurableModelHashSet))
                    {
                        if (!configurableModelHashSet.Contains(modelInfo.ToModelRecord()))
                        {
                            return true;
                        }

                        return false;
                    }

                    return true;
                });

                // 这里仅克隆值而不是使用引用
                _configurationGearInfos.Add(_configurationData.ToInfoData());
                // 生成后id+1
                _configurationData.Id++;
            }

            // 配置的模型列表
            _configurationGearFoldout =
                EditorGUILayout.Foldout(_configurationGearFoldout, "已配置的GearInfo列表");
            if (_configurationGearFoldout)
            {
                HumanoidAppearanceGearInfoData deleteData = null;
                foreach (var gearInfoData in _configurationGearInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(gearInfoData.Id.ToString());
                    EditorGUILayout.LabelField(gearInfoData.Races);
                    EditorGUILayout.LabelField(gearInfoData.Models);
                    EditorGUILayout.LabelField(gearInfoData.Mark);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = gearInfoData.ToUIData(_importModelInfoContainer);
                        RefreshConfigurableModels();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = gearInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _configurationGearInfos.Remove(deleteData);
                }
            }

            // 这里检查是否存在相同id的模型，是则提醒用户
            var gearInfoIdSet = new HashSet<int>();
            foreach (var loadGearInfo in _loadGearInfos)
            {
                if (!gearInfoIdSet.Add(loadGearInfo.Id))
                {
                    throw new Exception("The GearInfo list contains the same id");
                }
            }

            foreach (var configurationGearInfo in _configurationGearInfos)
            {
                if (!gearInfoIdSet.Add(configurationGearInfo.Id))
                {
                    throw new Exception("The GearInfo list contains the same id");
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
            EditorGUILayout.LabelField("导出至GearInfo Excel文件");
            EditorGUILayout.TextField(_exportGearExcelPath);
            if (GUILayout.Button("选择文件"))
            {
                _exportGearExcelPath =
                    EditorUtility.OpenFilePanel("选择Excel文件", _exportGearExcelPath, "xls,xlsx");
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
                var list = new List<HumanoidAppearanceGearInfoData>();
                list.AddRange(_loadGearInfos);
                list.AddRange(_configurationGearInfos);
                list.Sort((a, b) => a.Id <= b.Id ? -1 : 1);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(_exportGearExcelPath));

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
                    var gearInfo = list[i];
                    workbookWorksheet.Cells[row, 1].Value = gearInfo.Id;
                    workbookWorksheet.Cells[row, 2].Value = gearInfo.Races;
                    workbookWorksheet.Cells[row, 3].Value = gearInfo.Models;
                    workbookWorksheet.Cells[row, 4].Value = gearInfo.Part;
                    workbookWorksheet.Cells[row, 5].Value = gearInfo.Mark;
                    workbookWorksheet.Cells[row, 6].Value = gearInfo.PrimaryColor;
                    workbookWorksheet.Cells[row, 7].Value = gearInfo.SecondaryColor;
                    workbookWorksheet.Cells[row, 8].Value = gearInfo.MetalPrimaryColor;
                    workbookWorksheet.Cells[row, 9].Value = gearInfo.MetalSecondaryColor;
                    workbookWorksheet.Cells[row, 10].Value = gearInfo.MetalDarkColor;
                    workbookWorksheet.Cells[row, 11].Value = gearInfo.LeatherPrimaryColor;
                    workbookWorksheet.Cells[row, 12].Value = gearInfo.LeatherSecondaryColor;
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
                if (pair.modelRecord.Type != HumanoidModelType.Gear)
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