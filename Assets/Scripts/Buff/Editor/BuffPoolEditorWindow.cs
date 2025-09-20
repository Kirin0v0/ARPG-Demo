using System;
using System.Collections.Generic;
using System.Linq;
using Buff.Config;
using Framework.Common.Debug;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Buff.Editor
{
    public class BuffPoolEditorWindow : EditorWindow
    {
        private ObjectField _ofBuffPool;
        private ToolbarButton _tbApply;
        private ToolbarButton _tbRevert;
        private Button _btnAddBuff;
        private ListView _lvBuffList;
        private Button _btnCopyBuffToNew;
        private Button _btnDeleteBuff;
        private IMGUIContainer _buffConfigContainer;

        private BuffPool _buffPool;
        private BuffConfig _selectedBuffConfig;
        private UnityEditor.Editor _selectedBuffEditor;

        [MenuItem("Tools/Game/Buff Pool Editor")]
        public static void ShowBuffPoolEditorWindow()
        {
            var window = GetWindow<BuffPoolEditorWindow>();
            window.titleContent = new GUIContent("Buff Pool Editor");
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
            if (Selection.activeObject is BuffPool buffPool && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowBuffPoolEditorWindow();
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
                    "Assets/Scripts/Buff/Editor/BuffPoolEditorWindow.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Scripts/Buff/Editor/BuffPoolEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            // 获取控件
            _ofBuffPool = root.Q<ObjectField>("OfBuffPool");
            _tbApply = root.Q<ToolbarButton>("TbApply");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _btnAddBuff = root.Q<Button>("BtnAddBuff");
            _lvBuffList = root.Q<ListView>("LvBuffList");
            _btnCopyBuffToNew = root.Q<Button>("BtnCopyBuffToNew");
            _btnDeleteBuff = root.Q<Button>("BtnDeleteBuff");
            _buffConfigContainer = root.Q<IMGUIContainer>("BuffConfigContainer");

            // 监听Buff池对象变化
            _ofBuffPool.RegisterValueChangedCallback(HandleBuffPoolChanged);

            // 撤销操作和保存操作
            _tbApply.clicked += HandleApplyClicked;
            _tbRevert.clicked += HandleRevertClicked;

            // 监听Buff添加按钮
            _btnAddBuff.clicked += HandleAddBuffClicked;

            // 初始化Buff列表
            _lvBuffList.makeItem = CreateBuffListItem;
            _lvBuffList.bindItem = BindBuffListItem;
            _lvBuffList.unbindItem = UnbindBuffListItem;
            _lvBuffList.selectionChanged += OnBuffSelected;

            // 添加Buff窗口绘制回调
            _buffConfigContainer.onGUIHandler = DrawBuffEditor;

            // 监听Buff窗口的复制和删除按钮
            _btnCopyBuffToNew.clicked += HandleCopyBuffClicked;
            _btnDeleteBuff.clicked += HandleDeleteBuffClicked;

            // 判断创建视图时是否选择Buff池，是则直接设置为对象
            if (Selection.activeObject is BuffPool buffPool &&
                AssetDatabase.CanOpenAssetInEditor(buffPool.GetInstanceID()))
            {
                _ofBuffPool.value = buffPool;
                _lvBuffList.ClearSelection();
                _selectedBuffConfig = null;
                _selectedBuffEditor = null;
            }
            else
            {
                _ofBuffPool.value = null;
                _lvBuffList.ClearSelection();
                _selectedBuffConfig = null;
                _selectedBuffEditor = null;
            }
        }

        private void OnDestroy()
        {
            _ofBuffPool.UnregisterValueChangedCallback(HandleBuffPoolChanged);
            _tbApply.clicked -= HandleApplyClicked;
            _tbRevert.clicked -= HandleRevertClicked;
            _btnAddBuff.clicked -= HandleAddBuffClicked;
            _lvBuffList.makeItem = null;
            _lvBuffList.bindItem = null;
            _lvBuffList.unbindItem = null;
            _lvBuffList.selectionChanged -= OnBuffSelected;
            _buffConfigContainer.onGUIHandler = null;
            _btnCopyBuffToNew.clicked -= HandleCopyBuffClicked;
            _btnDeleteBuff.clicked -= HandleDeleteBuffClicked;
        }

        private void HandleBuffPoolChanged(ChangeEvent<Object> value)
        {
            _buffPool = _ofBuffPool.value as BuffPool;
            RefreshBuffList();
        }

        private void HandleApplyClicked()
        {
            if (_ofBuffPool.value && _ofBuffPool.value is BuffPool buffPool)
            {
                Undo.ClearAll();
                new SerializedObject(buffPool).ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                RefreshBuffList();
            }
        }

        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            RefreshBuffList();
        }

        private void HandleAddBuffClicked()
        {
            if (!_buffPool) return;
            var newBuffConfig = _buffPool.AddBuff();
            RefreshBuffList();
            // 添加后查看列表是否有对应的Buff，是则选中该buff
            for (var i = 0; i < _lvBuffList.itemsSource.Count; i++)
            {
                var buffConfig = _lvBuffList.itemsSource[i] as BuffConfig;
                if (buffConfig == newBuffConfig)
                {
                    _lvBuffList.selectedIndex = i;
                }
            }
        }

        private VisualElement CreateBuffListItem()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Scripts/Buff/Editor/BuffItemView.uxml");
            return visualTreeAsset.CloneTree();
        }

        private void BindBuffListItem(VisualElement parent, int index)
        {
            var data = _lvBuffList.itemsSource[index] as BuffConfig;
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = () =>
            {
                var width = container.contentRect.width;
                var height = container.contentRect.height;
                var name = data.name;
                var id = "Id " + data.id;
                var icon = data.icon;
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

        private void UnbindBuffListItem(VisualElement parent, int index)
        {
            var container = parent.Q<IMGUIContainer>("ItemContainer");
            container.onGUIHandler = null;
        }

        private void OnBuffSelected(IEnumerable<object> objects)
        {
            // 判断是否有选中的Buff
            if (!objects.Any())
            {
                // 如果没有就清除选择
                _selectedBuffEditor = null;
                _btnCopyBuffToNew.visible = false;
                _btnDeleteBuff.visible = false;
            }
            else
            {
                // 否则就遍历选择Buff
                foreach (var obj in objects)
                {
                    var buffConfig = obj as BuffConfig;
                    _selectedBuffConfig = buffConfig;
                    if (_selectedBuffConfig)
                    {
                        _selectedBuffEditor = UnityEditor.Editor.CreateEditor(_selectedBuffConfig);
                        _btnCopyBuffToNew.visible = true;
                        _btnDeleteBuff.visible = true;
                    }
                    else
                    {
                        _selectedBuffEditor = null;
                        _btnCopyBuffToNew.visible = false;
                        _btnDeleteBuff.visible = false;
                    }
                }
            }
        }

        private void HandleCopyBuffClicked()
        {
            if (!_buffPool || !_selectedBuffConfig)
            {
                return;
            }

            var newBuffConfig = _buffPool.CopyBuff(_selectedBuffConfig);
            RefreshBuffList();
            // 添加后查看列表是否有对应的Buff，是则选中该buff
            for (var i = 0; i < _lvBuffList.itemsSource.Count; i++)
            {
                var buffConfig = _lvBuffList.itemsSource[i] as BuffConfig;
                if (buffConfig == newBuffConfig)
                {
                    _lvBuffList.selectedIndex = i;
                }
            }
        }

        private void HandleDeleteBuffClicked()
        {
            if (!_buffPool || !_selectedBuffConfig)
            {
                return;
            }

            _buffPool.DeleteBuff(_selectedBuffConfig);
            RefreshBuffList();
        }

        private void DrawBuffEditor()
        {
            if (!_selectedBuffEditor)
            {
                return;
            }

            if (_selectedBuffEditor.target)
            {
                _selectedBuffEditor.OnInspectorGUI();
            }
        }

        private void RefreshBuffList()
        {
            if (_buffPool)
            {
                _lvBuffList.itemsSource = _buffPool.BuffConfigs;
            }
            else
            {
                _lvBuffList.itemsSource = Array.Empty<BuffConfig>();
            }

            // 去判断最新列表数据是否存在选中数据，是则不需要更改选中，否则置空
            foreach (var item in _lvBuffList.itemsSource)
            {
                var buffConfig = item as BuffConfig;
                if (buffConfig == _selectedBuffConfig)
                {
                    return;
                }
            }

            _lvBuffList.ClearSelection();
        }
    }
}