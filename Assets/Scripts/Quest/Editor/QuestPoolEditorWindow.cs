using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Util;
using Quest.Config;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Quest.Editor
{
    public class QuestPoolEditorWindow : EditorWindow
    {
        private ObjectField _ofQuestPool;
        private ToolbarButton _tbApply;
        private ToolbarButton _tbRevert;
        private Button _btnAddQuest;
        private ListView _lvQuestList;
        private Button _btnDeleteQuest;
        private IMGUIContainer _questConfigContainer;

        private QuestPool _questPool;
        private QuestConfig _selectedQuestConfig;
        private UnityEditor.Editor _selectedQuestEditor;

        [MenuItem("Tools/Game/Quest Pool Editor")]
        public static void ShowQuestPoolEditorWindow()
        {
            var window = GetWindow<QuestPoolEditorWindow>();
            window.titleContent = new GUIContent("Quest Pool Editor");
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
            if (Selection.activeObject is QuestPool questPool && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowQuestPoolEditorWindow();
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
                    "Assets/Scripts/Quest/Editor/QuestPoolEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Quest/Editor/QuestPoolEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            // 获取控件
            _ofQuestPool = root.Q<ObjectField>("OfQuestPool");
            _tbApply = root.Q<ToolbarButton>("TbApply");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _btnAddQuest = root.Q<Button>("BtnAddQuest");
            _lvQuestList = root.Q<ListView>("LvQuestList");
            _btnDeleteQuest = root.Q<Button>("BtnDeleteQuest");
            _questConfigContainer = root.Q<IMGUIContainer>("QuestConfigContainer");

            // 监听任务池对象变化
            _ofQuestPool.RegisterValueChangedCallback(HandleQuestPoolChanged);

            // 撤销操作和保存操作
            _tbApply.clicked += HandleApplyClicked;
            _tbRevert.clicked += HandleRevertClicked;

            // 监听任务添加按钮
            _btnAddQuest.clicked += HandleAddQuestClicked;

            // 初始化任务列表
            _lvQuestList.makeItem = CreateQuestListItem;
            _lvQuestList.bindItem = BindQuestListItem;
            _lvQuestList.unbindItem = UnbindQuestListItem;
            _lvQuestList.selectionChanged += OnQuestSelected;

            // 添加任务窗口绘制回调
            _questConfigContainer.onGUIHandler = DrawQuestEditor;

            // 监听任务窗口的删除按钮
            _btnDeleteQuest.clicked += HandleDeleteQuestClicked;
            
            // 判断创建视图时是否选择任务池，是则直接设置为对象
            if (Selection.activeObject is QuestPool questPool &&
                AssetDatabase.CanOpenAssetInEditor(questPool.GetInstanceID()))
            {
                _ofQuestPool.value = questPool;
                _lvQuestList.ClearSelection();
                _selectedQuestConfig = null;
                _selectedQuestEditor = null;
            }
            else
            {
                _ofQuestPool.value = null;
                _lvQuestList.ClearSelection();
                _selectedQuestConfig = null;
                _selectedQuestEditor = null;
            }
        }

        private void OnDestroy()
        {
            _ofQuestPool.UnregisterValueChangedCallback(HandleQuestPoolChanged);
            _tbApply.clicked -= HandleApplyClicked;
            _tbRevert.clicked -= HandleRevertClicked;
            _btnAddQuest.clicked -= HandleAddQuestClicked;
            _lvQuestList.makeItem = null;
            _lvQuestList.bindItem = null;
            _lvQuestList.unbindItem = null;
            _lvQuestList.selectionChanged -= OnQuestSelected;
            _questConfigContainer.onGUIHandler = null;
            _btnDeleteQuest.clicked -= HandleDeleteQuestClicked;
        }

        private void HandleQuestPoolChanged(ChangeEvent<Object> value)
        {
            _questPool = _ofQuestPool.value as QuestPool;
            RefreshQuestList();
        }

        private void HandleApplyClicked()
        {
            if (_ofQuestPool.value && _ofQuestPool.value is QuestPool questPool)
            {
                Undo.ClearAll();
                new SerializedObject(questPool).ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                RefreshQuestList();
            }
        }

        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            RefreshQuestList();
        }

        private void HandleAddQuestClicked()
        {
            if (!_questPool) return;
            var newQuestConfig = _questPool.AddQuest();
            RefreshQuestList();
            // 添加后查看列表是否有对应的任务，是则选中该任务
            for (var i = 0; i < _lvQuestList.itemsSource.Count; i++)
            {
                var questConfig = _lvQuestList.itemsSource[i] as QuestConfig;
                if (questConfig == newQuestConfig)
                {
                    _lvQuestList.selectedIndex = i;
                }
            }
        }

        private VisualElement CreateQuestListItem()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Scripts/Quest/Editor/QuestItemView.uxml");
            return visualTreeAsset.CloneTree();
        }

        private void BindQuestListItem(VisualElement parent, int index)
        {
            var data = _lvQuestList.itemsSource[index] as QuestConfig;
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = () =>
            {
                var width = container.contentRect.width;
                var height = container.contentRect.height;
                var id = data.id;
                var title = data.title;
                var description = TextUtil.GetLineText(data.description, 0);
                var idTextStyle = new GUIStyle
                {
                    fontSize = 20,
                    normal =
                    {
                        textColor = Color.gray,
                    }
                };
                var titleTextStyle = new GUIStyle
                {
                    fontSize = 15,
                    normal =
                    {
                        textColor = Color.white,
                    }
                };
                var descriptionTextStyle = new GUIStyle
                {
                    fontSize = 15,
                    normal =
                    {
                        textColor = Color.gray,
                    }
                };
                var idSize = titleTextStyle.CalcSize(new GUIContent(id));
                var titleSize = titleTextStyle.CalcSize(new GUIContent(title));
                var descriptionSize = titleTextStyle.CalcSize(new GUIContent(description));
                var startX = 10f;
                EditorGUI.LabelField(new Rect(startX, (height - idSize.y) / 2 - 3f, idSize.x, idSize.y), id,
                    idTextStyle);
                startX += idSize.x >= 40f ? idSize.x : 40f;
                EditorGUI.LabelField(new Rect(startX, height / 2 - titleSize.y, titleSize.x, titleSize.y), title,
                    titleTextStyle);
                EditorGUI.LabelField(new Rect(startX, height / 2, descriptionSize.x, descriptionSize.y), description,
                    descriptionTextStyle);
            };
        }

        private void UnbindQuestListItem(VisualElement parent, int index)
        {
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = null;
        }

        private void OnQuestSelected(IEnumerable<object> objects)
        {
            // 判断是否有选中的任务
            if (!objects.Any())
            {
                // 如果没有就清除选择
                _selectedQuestEditor = null;
                _btnDeleteQuest.visible = false;
            }
            else
            {
                // 否则就遍历选择任务
                foreach (var obj in objects)
                {
                    var questConfig = obj as QuestConfig;
                    _selectedQuestConfig = questConfig;
                    if (_selectedQuestConfig)
                    {
                        _selectedQuestEditor = UnityEditor.Editor.CreateEditor(_selectedQuestConfig);
                        _btnDeleteQuest.visible = true;
                    }
                    else
                    {
                        _selectedQuestEditor = null;
                        _btnDeleteQuest.visible = false;
                    }
                }
            }
        }

        private void HandleDeleteQuestClicked()
        {
            if (!_questPool || !_selectedQuestConfig)
            {
                return;
            }

            _questPool.DeleteQuest(_selectedQuestConfig);
            RefreshQuestList();
        }

        private void DrawQuestEditor()
        {
            if (!_selectedQuestEditor)
            {
                return;
            }

            if (_selectedQuestEditor.target)
            {
                _selectedQuestEditor.OnInspectorGUI();
            }
        }

        private void RefreshQuestList()
        {
            if (_questPool)
            {
                _lvQuestList.itemsSource = _questPool.QuestConfigs;
            }
            else
            {
                _lvQuestList.itemsSource = Array.Empty<QuestConfig>();
            }

            // 去判断最新列表数据是否存在选中数据，是则不需要更改选中，否则置空
            foreach (var item in _lvQuestList.itemsSource)
            {
                var questConfig = item as QuestConfig;
                if (questConfig == _selectedQuestConfig)
                {
                    return;
                }
            }

            _lvQuestList.ClearSelection();
        }
    }
}