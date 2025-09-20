using System;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Package.Data;
using Trade.Config;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Trade.Editor
{
    public class TradeConfigEditorWindow : EditorWindow
    {
        private ObjectField _ofPackageBinary;
        private ObjectField _ofTradeConfig;
        private IMGUIContainer _tradeGeneratorContainer;
        private IMGUIContainer _tradeConfigContainer;

        private TradeConfigGenerator _tradeConfigGenerator;

        private TradeConfig _tradeConfig;

        private UnityEditor.Editor _tradeGeneratorEditor;
        private UnityEditor.Editor _tradeConfigEditor;

        private readonly ExcelBinaryManager _excelBinaryManager = new();

        [MenuItem("Tools/Game/Trade Config Editor")]
        public static void ShowTradeConfigEditorWindow()
        {
            var window = GetWindow<TradeConfigEditorWindow>();
            window.titleContent = new GUIContent("Trade Config Editor");
        }

        /// <summary>
        /// 双击资源时节点函数回调
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="line"></param>
        /// <returns>返回true则代表处理了该事件</returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is TradeConfig tradeConfig && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowTradeConfigEditorWindow();
                return true;
            }

            return false;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            // 引用UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Scripts/Trade/Editor/TradeConfigEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Trade/Editor/TradeConfigEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            // 获取控件
            _ofPackageBinary = root.Q<ObjectField>("OfPackageBinary");
            _ofTradeConfig = root.Q<ObjectField>("OfTradeConfig");
            _tradeGeneratorContainer = root.Q<IMGUIContainer>("TradeEditorContainer");
            _tradeConfigContainer = root.Q<IMGUIContainer>("TradeConfigContainer");

            // 监听交易文件变化
            _ofTradeConfig.RegisterValueChangedCallback(HandleTradeConfigChanged);

            // 添加任务窗口绘制回调
            _tradeGeneratorContainer.onGUIHandler = DrawTradeGeneratorEditor;
            _tradeConfigContainer.onGUIHandler = DrawTradeConfigEditor;

            // 直接创建交易生成编辑器
            _tradeConfigGenerator = new(GetPackageInfoContainer);
            _tradeGeneratorEditor = UnityEditor.Editor.CreateEditor(_tradeConfigGenerator);

            // 如果此时选中的资源是交易文件，则自动设置为交易文件
            if (Selection.activeObject is TradeConfig tradeConfig)
            {
                _ofTradeConfig.value = tradeConfig;
            }
            else
            {
                _ofTradeConfig.value = null;
            }
            
            _tradeConfigEditor = UnityEditor.Editor.CreateEditor(_ofTradeConfig.value);
        }

        private void Update()
        {
            // 如果此时选中的资源是交易文件，则自动设置为交易文件
            if (Selection.activeObject is TradeConfig tradeConfig)
            {
                _ofTradeConfig.value = tradeConfig;
            }
        }

        private void OnDestroy()
        {
            _ofTradeConfig.UnregisterValueChangedCallback(HandleTradeConfigChanged);
            _tradeGeneratorContainer.onGUIHandler = null;
            _tradeConfigContainer.onGUIHandler = null;
        }

        private void HandleTradeConfigChanged(ChangeEvent<Object> evt)
        {
            _tradeConfig = _ofTradeConfig.value as TradeConfig;
            _tradeConfigGenerator.TradeConfig = _tradeConfig;
            _tradeConfigEditor = UnityEditor.Editor.CreateEditor(_tradeConfig);
        }

        private void DrawTradeGeneratorEditor()
        {
            if (!_tradeGeneratorEditor)
            {
                return;
            }

            if (_tradeGeneratorEditor.target)
            {
                _tradeGeneratorEditor.OnInspectorGUI();
            }
        }

        private void DrawTradeConfigEditor()
        {
            if (!_tradeConfigEditor)
            {
                return;
            }

            if (_tradeConfigEditor.target)
            {
                _tradeConfigEditor.OnInspectorGUI();
            }
        }

        private PackageInfoContainer GetPackageInfoContainer()
        {
            if (_ofPackageBinary.value is not DefaultAsset packageBinary)
            {
                return null;
            }

            var packageBinaryPath = AssetDatabase.GetAssetPath(packageBinary);
            _excelBinaryManager.LoadContainer<PackageInfoContainer, PackageInfoData>(packageBinaryPath, true);
            return _excelBinaryManager.GetContainer<PackageInfoContainer>();
        }
    }
}