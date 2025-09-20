using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Editor.UI;
using Framework.Common.Blackboard.Editor.UI;
using Framework.Common.Debug;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Framework.Common.BehaviourTree.Editor
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        private BehaviourTreeGraphView _graphView;
        private BehaviourTreeInspectorView _inspectorView;
        private BlackboardView _blackboardView;
        private ObjectField _ofBehaviourTree;
        private DropdownField _dfOperationPath;
        private ToolbarButton _tbRevert;
        private ToolbarButton _tbApply;

        private readonly Stack<BehaviourTree> _operationPath = new();

        [MenuItem("Tools/Behaviour Tree Editor")]
        public static void ShowBehaviourTreeEditorWindow()
        {
            var window = GetWindow<BehaviourTreeEditorWindow>();
            window.titleContent = new GUIContent("Behaviour Tree Editor");
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
            if (Selection.activeObject is BehaviourTree behaviourTree && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowBehaviourTreeEditorWindow();
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
                    "Assets/Framework/Common/BehaviourTree/Editor/BehaviourTreeEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Framework/Common/BehaviourTree/Editor/BehaviourTreeEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            _graphView = root.Q<BehaviourTreeGraphView>("BehaviourTreeGraphView");
            _inspectorView = root.Q<BehaviourTreeInspectorView>("BehaviourTreeInspectorView");
            _blackboardView = root.Q<BlackboardView>("BehaviourTreeBlackboardView");
            _ofBehaviourTree = root.Q<ObjectField>("OfBehaviourTree");
            _dfOperationPath = root.Q<DropdownField>("DfOperationPath");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _tbApply = root.Q<ToolbarButton>("TbApply");
            _operationPath.Clear();

            // 设置节点选择回调
            _graphView.OnNodeSelected = _inspectorView.HandleNodeSelected;
            _graphView.OnNodeUnselected = _inspectorView.HandleNodeUnselected;

            // 监听运行/编辑模式
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            // 监听行为树对象变化
            _ofBehaviourTree.RegisterValueChangedCallback(HandleBehaviourTreeChanged);

            // 监听路径选择
            _dfOperationPath.RegisterValueChangedCallback(HandleOperationPathChanged);

            // 撤销操作和保存操作
            _tbRevert.clicked += HandleRevertClicked;
            _tbApply.clicked += HandleApplyClicked;

            // 获取行为树
            TryToGetTree();
            TryToUpdateView();
        }

        private void Update()
        {
            _graphView?.UpdateNodeStates();
            if (_ofBehaviourTree.value && _ofBehaviourTree.value is BehaviourTree)
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

        private void OnGUI()
        {
            if (_graphView != null && Event.current != null)
            {
                _graphView.MousePosition = Event.current.mousePosition;
            }
        }

        private void OnDestroy()
        {
            _graphView.OnNodeSelected = null;
            _graphView.OnNodeUnselected = null;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            _ofBehaviourTree.UnregisterValueChangedCallback(HandleBehaviourTreeChanged);
            _tbRevert.clicked -= HandleRevertClicked;
            _tbApply.clicked -= HandleApplyClicked;
        }

        /// <summary>
        /// 项目资源选中改变节点回调
        /// </summary>
        private void OnSelectionChange()
        {
            // 获取行为树
            TryToGetTree();
        }

        private void TryToGetTree()
        {
            // 当项目选中行为树文件且能够打开行为树时获取行为树
            if (Selection.activeObject is BehaviourTree behaviourTree)
            {
                _ofBehaviourTree.value = behaviourTree;
                return;
            }

            // 当项目选中游戏对象且存在行为树运行组件且树不为空时获取行为树
            if (Selection.activeGameObject)
            {
                var behaviourTreeExecutor = Selection.activeGameObject.GetComponent<BehaviourTreeExecutor>();
                if (behaviourTreeExecutor && behaviourTreeExecutor.RuntimeTree)
                {
                    _ofBehaviourTree.value = behaviourTreeExecutor.RuntimeTree;
                }
            }
        }

        private void TryToUpdateView()
        {
            var tree = _ofBehaviourTree.value as BehaviourTree;
            _graphView?.UpdateView(tree);
            _blackboardView?.UpdateView(tree?.blackboard);
            UpdateOperationPath();
        }

        private void UpdateOperationPath()
        {
            var behaviourTree = _ofBehaviourTree.value as BehaviourTree;
            if (behaviourTree == null)
            {
                // 清空路径
                _operationPath.Clear();
                _dfOperationPath.value = null;
                _dfOperationPath.index = -1;
            }
            else
            {
                if (_operationPath.Contains(behaviourTree)) // 如果当前路径包含该树，认为是回退操作
                {
                    while (_operationPath.TryPeek(out var tree))
                    {
                        if (tree == behaviourTree)
                        {
                            break;
                        }

                        _operationPath.Pop();
                    }

                    _dfOperationPath.choices = _operationPath.Select(tree => tree.name).Reverse().ToList();
                    _dfOperationPath.index = _operationPath.Count - 1;
                }
                else // 否则认为是压入操作
                {
                    // 在此之前判断路径是否存在操作且树是否为路径最后一个操作的子树，是则追加，否则清空再添加
                    if (_operationPath.Count == 0 || _operationPath.Peek() != behaviourTree.Parent)
                    {
                        _operationPath.Clear();
                    }

                    _operationPath.Push(behaviourTree);
                    _dfOperationPath.choices = _operationPath.Select(tree => tree.name).Reverse().ToList();
                    _dfOperationPath.index = _operationPath.Count - 1;
                }
            }
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // 获取行为树
                    TryToGetTree();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // 获取行为树
                    TryToGetTree();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void HandleBehaviourTreeChanged(ChangeEvent<Object> evt)
        {
            // 更新树UI
            TryToUpdateView();
        }

        private void HandleOperationPathChanged(ChangeEvent<string> evt)
        {
            if (_dfOperationPath.index != -1)
            {
                while (_operationPath.TryPeek(out var tree))
                {
                    if (tree.name == _dfOperationPath.value)
                    {
                        break;
                    }

                    _operationPath.Pop();
                }

                Selection.activeObject = _operationPath.Count == 0 ? null : _operationPath.Peek();
            }
        }

        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void HandleApplyClicked()
        {
            if (_ofBehaviourTree.value && _ofBehaviourTree.value is BehaviourTree tree)
            {
                Undo.ClearAll();
                new SerializedObject(tree).ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}