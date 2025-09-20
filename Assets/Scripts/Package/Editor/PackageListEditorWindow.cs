using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Buff;
using Buff.Config;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Framework.Common.Resource;
using Humanoid.Data;
using OfficeOpenXml;
using Package.Data;
using Package.Data.Extension;
using Package.Editor.Data;
using Package.Editor.Data.Extension;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Package.Editor
{
    public class PackageListEditorWindow : EditorWindow
    {
        private ObjectField _ofPackageBinary;
        private ObjectField _ofWeaponAppearanceBinary;
        private ObjectField _ofGearAppearanceBinary;
        private ToolbarButton _tbImport;
        private ObjectField _ofPackageExcel;
        private TextField _tfPackageSheet;
        private ToolbarButton _tbExport;
        private Button _btnAddPackage;
        private ListView _lvPackageList;
        private Button _btnCopyPackageToNew;
        private Button _btnDeletePackage;
        private IMGUIContainer _packageDetailContainer;

        private bool _imported;

        private bool Imported
        {
            get => _imported;
            set
            {
                _imported = value;
                if (value)
                {
                    _btnAddPackage.visible = true;
                }
                else
                {
                    _btnAddPackage.visible = false;
                }

                _lvPackageList.ClearSelection();
                _selectedPackageEditor = null;
                RefreshPackageList();
            }
        }

        private readonly ExcelBinaryManager _excelBinaryManager = new();
        private readonly AddressablesManager _addressablesManager = new();

        private int _packageNewestId;
        private readonly List<PackageEditorData> _packageItems = new();
        private PackageEditorData _selectedPackage;
        private UnityEditor.Editor _selectedPackageEditor;

        [MenuItem("Tools/Game/Package List Editor")]
        public static void ShowPackageListEditorWindow()
        {
            var window = GetWindow<PackageListEditorWindow>();
            window.titleContent = new GUIContent("Package List Editor");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            // 引用UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Scripts/Package/Editor/PackageListEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Package/Editor/PackageListEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            // 获取控件
            _ofPackageBinary = root.Q<ObjectField>("OfPackageBinary");
            _ofWeaponAppearanceBinary = root.Q<ObjectField>("OfWeaponAppearanceBinary");
            _ofGearAppearanceBinary = root.Q<ObjectField>("OfGearAppearanceBinary");
            _tbImport = root.Q<ToolbarButton>("TbImport");
            _ofPackageExcel = root.Q<ObjectField>("OfPackageExcel");
            _tfPackageSheet = root.Q<TextField>("TfPackageSheet");
            _tfPackageSheet.value = "PackageInfo";
            _tbExport = root.Q<ToolbarButton>("TbExport");
            _btnAddPackage = root.Q<Button>("BtnAddPackage");
            _lvPackageList = root.Q<ListView>("LvPackageList");
            _lvPackageList.itemsSource = _packageItems;
            _btnCopyPackageToNew = root.Q<Button>("BtnCopyPackageToNew");
            _btnDeletePackage = root.Q<Button>("BtnDeletePackage");
            _packageDetailContainer = root.Q<IMGUIContainer>("PackageDetailContainer");

            // 导入和导出操作
            _tbImport.clicked += HandleImportClicked;
            _tbExport.clicked += HandleExportClicked;

            // 监听添加按钮
            _btnAddPackage.clicked += HandleAddPackageClicked;

            // 初始化列表
            _lvPackageList.makeItem = CreatePackageListItem;
            _lvPackageList.bindItem = BindPackageListItem;
            _lvPackageList.unbindItem = UnbindPackageListItem;
            _lvPackageList.selectionChanged += OnPackageSelected;

            // 添加物品窗口绘制回调
            _packageDetailContainer.onGUIHandler = DrawPackageEditor;

            // 监听物品窗口的复制和删除按钮
            _btnCopyPackageToNew.clicked += HandleCopyPackageClicked;
            _btnDeletePackage.clicked += HandleDeletePackageClicked;

            // 创建视图默认是未导入数据
            Imported = false;
        }

        private void OnDestroy()
        {
            _addressablesManager?.ClearAllAssets();
            _tbImport.clicked -= HandleImportClicked;
            _tbExport.clicked -= HandleExportClicked;
            _btnAddPackage.clicked -= HandleAddPackageClicked;
            _lvPackageList.makeItem = null;
            _lvPackageList.bindItem = null;
            _lvPackageList.unbindItem = null;
            _lvPackageList.selectionChanged -= OnPackageSelected;
            _packageDetailContainer.onGUIHandler = null;
            _btnCopyPackageToNew.clicked -= HandleCopyPackageClicked;
            _btnDeletePackage.clicked -= HandleDeletePackageClicked;
        }

        private void HandleImportClicked()
        {
            if (Imported)
            {
                if (!EditorUtility.DisplayDialog("提示", "当前已导入数据，是否重置导入数据？", "是"))
                {
                    return;
                }
            }

            _packageItems.Clear();

            if (_ofWeaponAppearanceBinary.value is not DefaultAsset weaponAppearanceBinary ||
                _ofGearAppearanceBinary.value is not DefaultAsset gearAppearanceBinary)
            {
                _packageNewestId = -1;
                Imported = false;
            }
            else
            {
                PackageInfoContainer packageInfoContainer = null;

                if (_ofPackageBinary.value is not DefaultAsset packageBinary)
                {
                    _packageNewestId = -1;
                }
                else
                {
                    var packageBinaryPath = AssetDatabase.GetAssetPath(packageBinary);
                    _excelBinaryManager.LoadContainer<PackageInfoContainer, PackageInfoData>(packageBinaryPath, true);
                    packageInfoContainer = _excelBinaryManager.GetContainer<PackageInfoContainer>();
                    _packageNewestId = packageInfoContainer.Data.Keys.Max();
                }

                var weaponAppearanceBinaryPath = AssetDatabase.GetAssetPath(weaponAppearanceBinary);
                _excelBinaryManager
                    .LoadContainer<HumanoidAppearanceWeaponInfoContainer, HumanoidAppearanceWeaponInfoData>(
                        weaponAppearanceBinaryPath, true);
                var weaponInfoContainer = _excelBinaryManager.GetContainer<HumanoidAppearanceWeaponInfoContainer>();
                var gearAppearanceBinaryPath = AssetDatabase.GetAssetPath(gearAppearanceBinary);
                _excelBinaryManager.LoadContainer<HumanoidAppearanceGearInfoContainer, HumanoidAppearanceGearInfoData>(
                    gearAppearanceBinaryPath, true);
                var gearInfoContainer = _excelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();

                if (packageInfoContainer != null)
                {
                    _packageItems.AddRange(
                        packageInfoContainer.Data.Values.Select(data =>
                        {
                            var packageEditorData = data.ToPackageEditorData(weaponInfoContainer, gearInfoContainer);
                            packageEditorData.LoadSprite = LoadSprite;
                            packageEditorData.CheckThumbnail();
                            return packageEditorData;
                        }));
                }

                Imported = true;
            }
        }

        private void HandleExportClicked()
        {
            if (_ofPackageExcel.value is not DefaultAsset packageExcel)
            {
                throw new Exception("The export excel is not configured");
            }

            var list = _packageItems.ConvertAll(x => x.ToPackageInfoData()).ToList();
            list.Sort((a, b) => a.Id <= b.Id ? -1 : 1);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(AssetDatabase.GetAssetPath(packageExcel)));

            // 查找或创建sheet
            var workbookWorksheet = package.Workbook.Worksheets[_tfPackageSheet.value] ??
                                    package.Workbook.Worksheets.Add(_tfPackageSheet.value);

            // 写入数据
            var startRow = 5;
            for (var i = 0; i < list.Count; i++)
            {
                var row = startRow + i;
                var packageInfoData = list[i];
                var index = 1;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Id;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Type;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Name;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Introduction;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Price;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Thumbnail;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.QuantitativeRestriction;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.GroupMaximum;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.WeaponType;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.WeaponAppearanceId;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.GearPart;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.GearAppearanceId;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.MaxHp;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.MaxMp;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.DefenceDamageMultiplier;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.DefenceBreakResumeSpeed;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Stamina;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Strength;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Magic;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Reaction;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Luck;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.WeaponSkills;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.ItemAppearancePrefab;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Hp;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.Mp;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.BuffId;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.BuffStack;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.BuffDuration;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.ItemSkills;
                workbookWorksheet.Cells[row, index++].Value = packageInfoData.MaterialAppearancePrefab;
            }

            // 保存数据
            package.Save();

            EditorUtility.DisplayDialog("导出结果", "成功！！！", "好的");
        }

        private void HandleAddPackageClicked()
        {
            if (!Imported)
            {
                return;
            }

            var packageItemConfig = ScriptableObject.CreateInstance<PackageEditorData>();
            packageItemConfig.WeaponInfoContainer =
                _excelBinaryManager.GetContainer<HumanoidAppearanceWeaponInfoContainer>();
            packageItemConfig.GearInfoContainer =
                _excelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();
            packageItemConfig.LoadSprite = LoadSprite;
            packageItemConfig.id = ++_packageNewestId;
            _packageItems.Add(packageItemConfig);
            RefreshPackageList();
            // 添加后查看列表是否有对应的物品，是则选中该物品
            for (var i = 0; i < _lvPackageList.itemsSource.Count; i++)
            {
                var data = _lvPackageList.itemsSource[i] as PackageEditorData;
                if (data == packageItemConfig)
                {
                    _lvPackageList.selectedIndex = i;
                }
            }
        }

        private VisualElement CreatePackageListItem()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Scripts/Package/Editor/PackageItemView.uxml");
            return visualTreeAsset.CloneTree();
        }

        private void BindPackageListItem(VisualElement parent, int index)
        {
            var data = _lvPackageList.itemsSource[index] as PackageEditorData;
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = () =>
            {
                var width = container.contentRect.width;
                var height = container.contentRect.height;
                var name = data.name;
                var id = $"Id {data.id}/{data.type.GetString()}";
                var icon = data.thumbnail;
                var nameTextStyle = new GUIStyle
                {
                    fontSize = 20,
                    normal =
                    {
                        textColor = Color.white,
                    }
                };
                var idTextStyle = new GUIStyle
                {
                    fontSize = 15,
                    normal =
                    {
                        textColor = Color.gray,
                    }
                };
                var iconSize = 50f;
                var nameSize = nameTextStyle.CalcSize(new GUIContent(name));
                var idSize = nameTextStyle.CalcSize(new GUIContent(id));

                var iconX = 10f;
                if (icon)
                {
                    EditorGUI.DrawPreviewTexture(new Rect(iconX, (height - iconSize) / 2, iconSize, iconSize),
                        AssetPreview.GetAssetPreview(icon));
                }

                var textX = iconX + iconSize + 10f;
                EditorGUI.LabelField(new Rect(textX, height / 2 - nameSize.y, nameSize.x, nameSize.y), name,
                    nameTextStyle);
                EditorGUI.LabelField(new Rect(textX, height / 2, idSize.x, idSize.y), id, idTextStyle);
            };
        }

        private void UnbindPackageListItem(VisualElement parent, int index)
        {
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = null;
        }

        private void OnPackageSelected(IEnumerable<object> objects)
        {
            // 判断是否有选中的物品
            if (!objects.Any())
            {
                // 如果没有就清除选择
                _selectedPackageEditor = null;
                _btnCopyPackageToNew.visible = false;
                _btnDeletePackage.visible = false;
            }
            else
            {
                // 否则就遍历选择物品
                foreach (var obj in objects)
                {
                    _selectedPackage = obj as PackageEditorData;
                    if (_selectedPackage != null)
                    {
                        _selectedPackageEditor =
                            UnityEditor.Editor.CreateEditor(_selectedPackage);
                        _btnCopyPackageToNew.visible = true;
                        _btnDeletePackage.visible = true;
                    }
                    else
                    {
                        _selectedPackageEditor = null;
                        _btnCopyPackageToNew.visible = false;
                        _btnDeletePackage.visible = false;
                    }
                }
            }
        }

        private void HandleCopyPackageClicked()
        {
            if (!Imported || !_selectedPackage)
            {
                return;
            }

            var newPackage = Instantiate(_selectedPackage);
            newPackage.WeaponInfoContainer = _excelBinaryManager.GetContainer<HumanoidAppearanceWeaponInfoContainer>();
            newPackage.GearInfoContainer = _excelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();
            newPackage.LoadSprite = LoadSprite;
            newPackage.id = ++_packageNewestId;
            _packageItems.Add(newPackage);
            RefreshPackageList();
            // 添加后查看列表是否有对应的物品，是则选中该物品
            for (var i = 0; i < _lvPackageList.itemsSource.Count; i++)
            {
                var data = _lvPackageList.itemsSource[i] as PackageEditorData;
                if (data == newPackage)
                {
                    _lvPackageList.selectedIndex = i;
                }
            }
        }

        private void HandleDeletePackageClicked()
        {
            if (!Imported || !_selectedPackage)
            {
                return;
            }

            _packageItems.Remove(_selectedPackage);
            RefreshPackageList();
        }

        private void DrawPackageEditor()
        {
            if (!_selectedPackageEditor)
            {
                return;
            }

            if (_selectedPackageEditor.target)
            {
                _selectedPackageEditor.OnInspectorGUI();
            }
        }

        private void RefreshPackageList()
        {
            _lvPackageList.RefreshItems();

            // 去判断最新列表数据是否存在选中数据，是则不需要更改选中，否则置空
            foreach (var item in _lvPackageList.itemsSource)
            {
                var data = item as PackageEditorData;
                if (data == _selectedPackage)
                {
                    return;
                }
            }

            _lvPackageList.ClearSelection();
        }

        private void LoadSprite(PackageEditorData data, string atlas, string name)
        {
            _addressablesManager.LoadAssetAsync<SpriteAtlas>(atlas, handle =>
            {
                var sprite = handle.GetSprite(name);
                data.thumbnail = sprite;
            });
        }
    }
}