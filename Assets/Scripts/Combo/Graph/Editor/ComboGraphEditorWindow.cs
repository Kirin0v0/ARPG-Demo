using System;
using System.Collections.Generic;
using Combo.Graph.Editor.GUI;
using Combo.Graph.Editor.UI;
using Combo.Graph.Unit;
using Framework.Common.Blackboard.Editor.UI;
using Framework.Common.Debug;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Combo.Graph.Editor
{
    public class ComboGraphEditorWindow : EditorWindow
    {
        private ComboGraphContext _context;

        private IMGUIContainer _graphContainer;
        private ComboGraphInspectorView _inspectorView;
        private BlackboardView _blackboardView;
        private ObjectField _ofComboGraph;
        private ToolbarButton _tbRevert;
        private ToolbarButton _tbApply;

        private readonly List<ComboGraphLayer> _graphLayers = new();

        [MenuItem("Tools/Game/Combo Graph Editor")]
        public static void ShowComboGraphEditorWindow()
        {
            ComboGraphEditorWindow window = GetWindow<ComboGraphEditorWindow>();
            window.titleContent = new GUIContent("Combo Graph Editor");
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
            if (Selection.activeObject is ComboGraph behaviourTree && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowComboGraphEditorWindow();
                return true;
            }

            return false;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            // 引用UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Scripts/Combo/Graph/Editor/ComboGraphEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Combo/Graph/Editor/ComboGraphEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            _context = new ComboGraphContext(this);
            _graphContainer = root.Q<IMGUIContainer>("ComboGraphContainer");
            _inspectorView = root.Q<ComboGraphInspectorView>("ComboGraphInspectorView");
            _blackboardView = root.Q<BlackboardView>("ComboGraphBlackboardView");
            _ofComboGraph = root.Q<ObjectField>("OfComboGraph");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _tbApply = root.Q<ToolbarButton>("TbApply");

            // 初始化图层
            _graphLayers.Add(new ComboGraphBackgroundLayer(_context));
            _graphLayers.Add(new ComboGraphTransitionLayer(_context));
            _graphLayers.Add(new ComboGraphNodeLayer(_context));
            _graphContainer.onGUIHandler += HandleGraphGUI;

            // 设置选中回调
            _context.OnGraphNodeSelected += _inspectorView.HandleNodeSelected;
            _context.OnGraphTransitionSelected += _inspectorView.HandleTransitionSelected;
            _context.OnGraphNoneSelected += _inspectorView.HandleNoneSelected;

            // 监听运行/编辑模式
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            // 监听连招图文件变化
            _ofComboGraph.RegisterValueChangedCallback(HandleComboGraphChanged);

            // 撤销操作和保存操作
            _tbRevert.clicked += HandleRevertClicked;
            _tbApply.clicked += HandleApplyClicked;

            // 获取连招图
            TryToSetGraph();
            TryToGetGraph();
        }

        private void Update()
        {
            _graphLayers.ForEach(layer => layer.Update());
            if (_context.ComboGraph)
            {
                _tbRevert.SetEnabled(true);
                _tbApply.SetEnabled(true);
            }
            else
            {
                _tbRevert.SetEnabled(false);
                _tbApply.SetEnabled(false);
            }
        }

        private void OnDestroy()
        {
            _graphLayers.Clear();
            _context.ComboGraph = null;

            _context.OnGraphNodeSelected -= _inspectorView.HandleNodeSelected;
            _context.OnGraphTransitionSelected -= _inspectorView.HandleTransitionSelected;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            _ofComboGraph.UnregisterValueChangedCallback(HandleComboGraphChanged);
            _tbRevert.clicked -= HandleRevertClicked;
            _tbApply.clicked -= HandleApplyClicked;
        }

        private void HandleGraphGUI()
        {
            // 正序渲染
            _graphLayers.ForEach(layer => layer.DrawGUI(_graphContainer.contentRect));
            // 倒序消费事件
            for (var i = _graphLayers.Count - 1; i >= 0; i--)
            {
                _graphLayers[i].ConsumeEvent();
            }
        }

        /// <summary>
        /// 项目资源选中改变节点回调
        /// </summary>
        private void OnSelectionChange()
        {
            // 获取连招图
            TryToSetGraph();
        }

        private void TryToSetGraph()
        {
            // 当项目选中连招图文件且能够打开连招图时获取图
            if (Selection.activeObject is ComboGraph comboGraph &&
                AssetDatabase.CanOpenAssetInEditor(comboGraph.GetInstanceID()))
            {
                _ofComboGraph.value = comboGraph;
                return;
            }

            // 当项目选中游戏对象且存在连招图运行组件且图不为空时获取连招图
            if (Selection.activeGameObject)
            {
                var executor = Selection.activeGameObject.GetComponent<ComboGraphExecutor>();
                if (executor && executor.Graph)
                {
                    _ofComboGraph.value = executor.Graph;
                }

                return;
            }
        }

        private void TryToGetGraph()
        {
            var comboGraph = _ofComboGraph.value as ComboGraph;
            _context.ComboGraph = comboGraph;
            if (comboGraph && !comboGraph.blackboard)
            {
                comboGraph.CreateBlackboard();
            }

            _blackboardView?.UpdateView(comboGraph?.blackboard);
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // 获取连招图并更新UI
                    TryToSetGraph();
                    TryToGetGraph();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // 获取连招图并更新UI
                    TryToSetGraph();
                    TryToGetGraph();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void HandleComboGraphChanged(ChangeEvent<Object> evt)
        {
            // 更新UI
            TryToGetGraph();
        }


        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void HandleApplyClicked()
        {
            if (_context.ComboGraph)
            {
                Undo.ClearAll();
                new SerializedObject(_context.ComboGraph).ApplyModifiedPropertiesWithoutUndo();
                // 额外将连招名称设置到节点上
                _context.ComboGraph.nodes.ForEach(node =>
                {
                    if (node is ComboGraphPlayNode playNode)
                    {
                        playNode.name = playNode.nodeName;
                    }
                });
                _context.ComboGraph.transitions.ForEach(transition =>
                {
                    transition.name = transition.from.name + "-->" + transition.to.name;
                });
                EditorUtility.SetDirty(_context.ComboGraph);
                AssetDatabase.SaveAssets();
            }
        }
    }
}