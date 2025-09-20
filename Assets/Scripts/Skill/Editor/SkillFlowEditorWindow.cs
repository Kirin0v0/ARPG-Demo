using System;
using System.IO;
using System.Linq;
using AoE;
using Bullet;
using Character;
using Framework.Common.Debug;
using Framework.Common.Timeline;
using Sirenix.Utilities;
using Skill.Editor.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Skill.Editor
{
    public class SkillFlowEditorWindow : EditorWindow
    {
        private SkillFlowGraphView _graphView;
        private SkillFlowInspectorView _inspectorView;
        private TextField _tfFileDirectory;
        private TextField _tfSkillId;
        private ObjectField _ofSkillFlow;
        private ToolbarButton _tbSelect;
        private ToolbarButton _tbCreateNew;
        private ToolbarButton _tbRevert;
        private ToolbarButton _tbApply;

        [MenuItem("Tools/Game/Skill Flow Editor")]
        public static void ShowSkillFlowEditorWindow()
        {
            var window = GetWindow<SkillFlowEditorWindow>();
            window.titleContent = new GUIContent("Skill Flow Editor");
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
            if (Selection.activeObject is SkillFlow skillFlow && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowSkillFlowEditorWindow();
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
                    "Assets/Scripts/Skill/Editor/SkillFlowEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Skill/Editor/SkillFlowEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            _graphView = root.Q<SkillFlowGraphView>("SkillFlowGraphView");
            _inspectorView = root.Q<SkillFlowInspectorView>("SkillFlowInspectorView");
            _tfFileDirectory = root.Q<TextField>("TfFileDirectory");
            _tfSkillId = root.Q<TextField>("TfSkillId");
            _ofSkillFlow = root.Q<ObjectField>("OfSkillFlow");
            _tbSelect = root.Q<ToolbarButton>("TbSelect");
            _tbCreateNew = root.Q<ToolbarButton>("TbCreateNew");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _tbApply = root.Q<ToolbarButton>("TbApply");

            // 选择文件路径以及创建技能流文件
            _tbSelect.clicked += HandleSelectClicked;
            _tbCreateNew.clicked += HandleCreateClicked;

            // 设置节点选择回调
            _graphView.OnNodeSelected = _inspectorView.HandleNodeSelected;
            _graphView.OnNodeUnselected = _inspectorView.HandleNodeUnselected;

            // 监听运行/编辑模式
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            // 监听技能流对象变化
            _ofSkillFlow.RegisterValueChangedCallback(HandleSkillFlowChanged);

            // 撤销操作和保存操作
            _tbRevert.clicked += HandleRevertClicked;
            _tbApply.clicked += HandleApplyClicked;

            // 获取技能流
            TryToGetFlow();
            TryToUpdateView();

            // 默认设置文件路径
            _tfFileDirectory.value = Application.dataPath + "/Prefabs/Skill";
        }

        private void Update()
        {
            if (_ofSkillFlow.value && _ofSkillFlow.value is SkillFlow)
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
            if (Event.current != null && _graphView != null)
            {
                _graphView.MousePosition = Event.current.mousePosition;
            }
        }

        private void OnDestroy()
        {
            _graphView.OnNodeSelected = null;
            _graphView.OnNodeUnselected = null;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            _ofSkillFlow.UnregisterValueChangedCallback(HandleSkillFlowChanged);
            _tbSelect.clicked -= HandleSelectClicked;
            _tbCreateNew.clicked -= HandleCreateClicked;
            _tbRevert.clicked -= HandleRevertClicked;
            _tbApply.clicked -= HandleApplyClicked;
        }

        /// <summary>
        /// 项目资源选中改变节点回调
        /// </summary>
        private void OnSelectionChange()
        {
            // 获取技能流
            TryToGetFlow();
        }

        private void TryToGetFlow()
        {
            // 当项目选中技能流文件且能够打开技能流时获取技能流
            {
                if (Selection.activeObject is SkillFlow skillFlow &&
                    AssetDatabase.CanOpenAssetInEditor(skillFlow.GetInstanceID()))
                {
                    _ofSkillFlow.value = skillFlow;
                    return;
                }
            }
        }

        private void TryToUpdateView()
        {
            var flow = _ofSkillFlow.value as SkillFlow;
            _graphView?.UpdateView(flow);
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // 获取技能流
                    TryToGetFlow();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // 获取技能流
                    TryToGetFlow();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void HandleSelectClicked()
        {
            _tfFileDirectory.value =
                EditorUtility.OpenFolderPanel("Select folder", _tfFileDirectory.value, "");
        }

        private void HandleCreateClicked()
        {
            // 判断文件路径是否有效
            if (String.IsNullOrEmpty(_tfFileDirectory.value) || !Directory.Exists(_tfFileDirectory.value) ||
                !_tfFileDirectory.value.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("警告", "创建路径无效", "确认");
                return;
            }

            // 判断技能Id是否有效
            if (String.IsNullOrEmpty(_tfSkillId.value))
            {
                EditorUtility.DisplayDialog("警告", "技能Id无效", "确认");
                return;
            }

            // 判断是否已存在对应文件
            var fileName = "Id " + _tfSkillId.value + " Skill";
            if (File.Exists(_tfFileDirectory.value + "/" + fileName + ".asset"))
            {
                EditorUtility.DisplayDialog("警告", "已存在该技能流文件", "确认");
                return;
            }

            // 创建技能流文件
            var relativeFolderPath = "Assets" + _tfFileDirectory.value.Replace(Application.dataPath, "");
            var filePath = relativeFolderPath + "/" + fileName + ".asset";
            var skillFlow = ScriptableObject.CreateInstance<SkillFlow>();
            skillFlow.name = fileName;
            AssetDatabase.CreateAsset(skillFlow, filePath);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = skillFlow;

            // 创建根节点
            var skillFlowRootNode = skillFlow.CreateRootNode();
            skillFlowRootNode.id = _tfSkillId.value;
            skillFlow.rootNode = skillFlowRootNode;
        }

        private void HandleSkillFlowChanged(ChangeEvent<Object> evt)
        {
            // 更新路径以及技能Id
            var flow = _ofSkillFlow.value as SkillFlow;
            if (flow)
            {
                var paths = AssetDatabase.GetAssetPath(flow).Split("/");
                var suffixPath = Path.Combine(paths.Take(paths.Length - 1).ToArray());
                _tfFileDirectory.value = Path.Combine(Directory.GetCurrentDirectory(), suffixPath).Replace("\\", "/");
                _tfSkillId.value = flow.Id;
            }

            // 更新UI
            TryToUpdateView();
        }

        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void HandleApplyClicked()
        {
            if (_ofSkillFlow.value && _ofSkillFlow.value is SkillFlow flow)
            {
                Undo.ClearAll();
                new SerializedObject(flow).ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}