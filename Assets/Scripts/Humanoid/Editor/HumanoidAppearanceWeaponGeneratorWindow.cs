using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Character.Data.Extension;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Framework.Common.Resource;
using Humanoid.Data;
using Humanoid.Editor.Data;
using Humanoid.Editor.Data.Extension;
using Humanoid.Model;
using Humanoid.Model.Data;
using Humanoid.Model.Extension;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using OfficeOpenXml;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Humanoid.Editor
{
    public class HumanoidAppearanceWeaponGeneratorWindow : EditorWindow
    {
        private const string EditorScenePath = "Assets/Scripts/Humanoid/Editor/AppearanceWeaponGeneratorScene.unity";

        private static readonly HumanoidAppearanceWeaponUIData Default = new HumanoidAppearanceWeaponUIData()
        {
            Id = 0,
            Model = "",
            Type = HumanoidAppearanceWeaponType.Fantasy,
            Comment = "",
            FantasyColor = HumanoidModelColor.DefaultWeaponColor.ToAppearanceColor(),
            SamuraiTexture = "",
        };

        private bool _init = false;
        private readonly ExcelBinaryManager _excelBinaryManager = new();
        private Vector2 _scrollPosition;

        // 配置相关
        private HumanoidAppearanceWeaponUIData _configurationData;

        // 预览相关
        private Vector3 _cameraOffset;

        private HumanoidWeaponCreator _weaponCreator;

        private HumanoidWeaponCreator WeaponCreator
        {
            get
            {
                if (_weaponCreator != null)
                {
                    return _weaponCreator;
                }

                _weaponCreator = new(HumanoidWeaponSingletonConfigSO.Instance.GetWeaponAppearanceMaterial);
                return _weaponCreator;
            }
        }

        private GameObject _appearanceInstance;

        // 读取和保存相关
        private string _loadWeaponInfoBinaryPath;
        private bool _loadWeaponFoldout;
        private bool _configurationWeaponFoldout;
        private readonly List<HumanoidAppearanceWeaponInfoData> _loadWeaponInfos = new();
        private readonly List<HumanoidAppearanceWeaponInfoData> _configurationWeaponInfos = new();

        // 导出相关
        private string _exportExcelPath;
        private string _exportSheetName;
        private int _exportStartRowNumber;

        [MenuItem("Tools/Game/Appearance/Weapon Appearance Generator")]
        public static void ShowAppearanceWeaponGeneratorWindow()
        {
            var window = GetWindow<HumanoidAppearanceWeaponGeneratorWindow>("Weapon Appearance Generator");
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
            _cameraOffset = new(0f, 0f, 1.5f);
            _loadWeaponInfoBinaryPath = $"{Application.streamingAssetsPath}";
            _loadWeaponFoldout = false;
            _configurationWeaponFoldout = false;
            _loadWeaponInfos.Clear();
            _configurationWeaponInfos.Clear();
            _exportExcelPath = $"{Application.dataPath}/Configurations/";
            _exportSheetName = "HumanoidAppearanceWeaponInfo";
            _exportStartRowNumber = 5;
        }

        private void OnGUI()
        {
            _configurationData ??= Default;

            EditorGUIUtility.labelWidth = 100f;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

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
            WeaponCreator?.Destroy();
            if (_appearanceInstance)
            {
                DestroyImmediate(_appearanceInstance);
            }

            _init = false;
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
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(new GUIContent("外观模型采用Addressables加载，因此请填写在Addressables中的名称"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Model = EditorGUILayout.TextField("外观模型", _configurationData.Model);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Type =
                (HumanoidAppearanceWeaponType)EditorGUILayout.EnumPopup("外观类型", _configurationData.Type);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _configurationData.Comment = EditorGUILayout.TextField("外观备注", _configurationData.Comment);
            EditorGUILayout.EndHorizontal();

            // 根据不同外观类型显示不同参数
            switch (_configurationData.Type)
            {
                case HumanoidAppearanceWeaponType.Fantasy:
                {
                    EditorGUILayout.BeginHorizontal();
                    _configurationData.FantasyColor.PrimaryColor =
                        EditorGUILayout.ColorField("主要颜色", _configurationData.FantasyColor.PrimaryColor);
                    _configurationData.FantasyColor.SecondaryColor =
                        EditorGUILayout.ColorField("次要颜色", _configurationData.FantasyColor.SecondaryColor);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _configurationData.FantasyColor.MetalPrimaryColor =
                        EditorGUILayout.ColorField("金属主要颜色", _configurationData.FantasyColor.MetalPrimaryColor);
                    _configurationData.FantasyColor.MetalSecondaryColor =
                        EditorGUILayout.ColorField("金属次要颜色", _configurationData.FantasyColor.MetalSecondaryColor);
                    _configurationData.FantasyColor.MetalDarkColor =
                        EditorGUILayout.ColorField("金属暗部颜色", _configurationData.FantasyColor.MetalDarkColor);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _configurationData.FantasyColor.LeatherPrimaryColor =
                        EditorGUILayout.ColorField("皮革主要颜色", _configurationData.FantasyColor.LeatherPrimaryColor);
                    _configurationData.FantasyColor.LeatherSecondaryColor =
                        EditorGUILayout.ColorField("皮革次要颜色", _configurationData.FantasyColor.LeatherSecondaryColor);
                    EditorGUILayout.EndHorizontal();
                }
                    break;
                case HumanoidAppearanceWeaponType.Samurai:
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(new GUIContent("纹理图片路径采用Addressables加载，因此请填写在Addressables中的名称"));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    _configurationData.SamuraiTexture =
                        EditorGUILayout.TextField("纹理图片路径", _configurationData.SamuraiTexture);
                    EditorGUILayout.EndHorizontal();
                }
                    break;
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
            EditorGUILayout.LabelField("相机偏移量");
            _cameraOffset = EditorGUILayout.Vector3Field("", _cameraOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(new GUIContent("预览先请进入游戏模式，否则由于编辑模式下禁止创建单例物体而无法加载模型等数据"));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("在当前场景中预览"))
            {
                if (_configurationData.Model == null)
                {
                    throw new Exception("You can't use the preview function unless model is ready");
                }

                if (_appearanceInstance)
                {
                    GameObject.DestroyImmediate(_appearanceInstance);
                }

                if (!EditorApplication.isPlaying)
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (activeScene.path != EditorScenePath)
                    {
                        EditorSceneManager.OpenScene(EditorScenePath);
                    }
                    else
                    {
                        EditorApplication.isPlaying = true;
                    }
                }
                else
                {
                    // 在场景中创建预设体实例
                    WeaponCreator.CreateWeaponModelAsync(
                        _configurationData.Type,
                        _configurationData.Model,
                        _configurationData.FantasyColor,
                        _configurationData.SamuraiTexture,
                        (instance =>
                        {
                            _appearanceInstance = instance;
                            // 同时将摄像机挪至外观实例前方
                            var mainCamera = UnityEngine.Camera.main;
                            if (mainCamera)
                            {
                                mainCamera.transform.position =
                                    _appearanceInstance.transform.TransformPoint(_cameraOffset);
                                var direction = _appearanceInstance.transform.position - mainCamera.transform.position;
                                mainCamera.transform.rotation =
                                    Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                            }
                        })
                    );
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
            EditorGUILayout.LabelField("加载WeaponInfo二进制文件");
            EditorGUILayout.TextField(_loadWeaponInfoBinaryPath);
            if (GUILayout.Button("选择文件"))
            {
                _loadWeaponInfoBinaryPath =
                    EditorUtility.OpenFilePanel("选择二进制文件", _loadWeaponInfoBinaryPath, "bin");
            }

            if (GUILayout.Button("加载数据"))
            {
                _excelBinaryManager
                    .LoadContainer<HumanoidAppearanceWeaponInfoContainer, HumanoidAppearanceWeaponInfoData>(
                        _loadWeaponInfoBinaryPath, true);
                var appearanceWeaponInfoContainer =
                    _excelBinaryManager.GetContainer<HumanoidAppearanceWeaponInfoContainer>();
                _loadWeaponInfos.Clear();
                _loadWeaponInfos.AddRange(appearanceWeaponInfoContainer.Data.Select(pair => pair.Value));
            }

            EditorGUILayout.EndHorizontal();

            // 加载的模型列表
            _loadWeaponFoldout = EditorGUILayout.Foldout(_loadWeaponFoldout, "已加载的WeaponInfo列表");
            if (_loadWeaponFoldout)
            {
                HumanoidAppearanceWeaponInfoData deleteData = null;
                foreach (var weaponInfoData in _loadWeaponInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(weaponInfoData.Id.ToString());
                    EditorGUILayout.LabelField(weaponInfoData.Model);
                    EditorGUILayout.LabelField(weaponInfoData.Comment);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = weaponInfoData.ToUIData();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = weaponInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _loadWeaponInfos.Remove(deleteData);
                }
            }

            if (GUILayout.Button("保存当前配置"))
            {
                // 过滤相同id模型
                if (_loadWeaponInfos.Find(data => data.Id == _configurationData.Id) != null ||
                    _configurationWeaponInfos.Find(data => data.Id == _configurationData.Id) != null)
                {
                    throw new Exception("Can't add the same id WeaponInfo");
                }

                // 这里仅克隆值而不是使用引用
                _configurationWeaponInfos.Add(_configurationData.ToInfoData());
                // 生成后id+1
                _configurationData.Id++;
            }

            // 配置的模型列表
            _configurationWeaponFoldout =
                EditorGUILayout.Foldout(_configurationWeaponFoldout, "已配置的WeaponInfo列表");
            if (_configurationWeaponFoldout)
            {
                HumanoidAppearanceWeaponInfoData deleteData = null;
                foreach (var weaponInfoData in _configurationWeaponInfos)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(weaponInfoData.Id.ToString());
                    EditorGUILayout.LabelField(weaponInfoData.Model);
                    EditorGUILayout.LabelField(weaponInfoData.Comment);

                    if (GUILayout.Button("导入当前配置"))
                    {
                        _configurationData = weaponInfoData.ToUIData();
                    }

                    if (GUILayout.Button("删除"))
                    {
                        deleteData = weaponInfoData;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (deleteData != null)
                {
                    _configurationWeaponInfos.Remove(deleteData);
                }
            }

            // 这里检查是否存在相同id的模型，是则提醒用户
            var weaponInfoIdSet = new HashSet<int>();
            foreach (var loadWeaponInfo in _loadWeaponInfos)
            {
                if (!weaponInfoIdSet.Add(loadWeaponInfo.Id))
                {
                    throw new Exception("The WeaponInfo list contains the same id");
                }
            }

            foreach (var configurationWeaponInfo in _configurationWeaponInfos)
            {
                if (!weaponInfoIdSet.Add(configurationWeaponInfo.Id))
                {
                    throw new Exception("The WeaponInfo list contains the same id");
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
            EditorGUILayout.LabelField("导出至WeaponInfo Excel文件");
            EditorGUILayout.TextField(_exportExcelPath);
            if (GUILayout.Button("选择文件"))
            {
                _exportExcelPath =
                    EditorUtility.OpenFilePanel("选择Excel文件", _exportExcelPath, "xls,xlsx");
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
                var list = new List<HumanoidAppearanceWeaponInfoData>();
                list.AddRange(_loadWeaponInfos);
                list.AddRange(_configurationWeaponInfos);
                list.Sort((a, b) => a.Id <= b.Id ? -1 : 1);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(_exportExcelPath));

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
                    var weaponInfo = list[i];
                    workbookWorksheet.Cells[row, 1].Value = weaponInfo.Id;
                    workbookWorksheet.Cells[row, 2].Value = weaponInfo.Model;
                    workbookWorksheet.Cells[row, 3].Value = weaponInfo.Type;
                    workbookWorksheet.Cells[row, 4].Value = weaponInfo.Comment;
                    workbookWorksheet.Cells[row, 5].Value = weaponInfo.Payload;
                }

                // 保存数据
                package.Save();

                EditorUtility.DisplayDialog("导出结果", "成功！！！", "好的");
            }
        }
    }
}