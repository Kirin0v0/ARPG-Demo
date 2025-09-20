using System;
using System.Collections.Generic;
using Framework.Common.Blackboard;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.Blackboard.Editor
{
    [CustomEditor(typeof(Blackboard))]
    public class BlackboardInspector : UnityEditor.Editor
    {
        private Blackboard _blackboard;

        private TextField _tfParameterName;
        private ToolbarButton _tbAddParameter;
        private ToolbarButton _tbRemoveParameter;

        private ListView _listView;
        private VisualTreeAsset _listViewItem;
        private readonly Dictionary<IntegerField, EventCallback<ChangeEvent<int>>> _ifEvents = new();
        private readonly Dictionary<FloatField, EventCallback<ChangeEvent<float>>> _ffEvents = new();
        private readonly Dictionary<Toggle, EventCallback<ChangeEvent<bool>>> _togEvents = new();
        private readonly Dictionary<TextField, EventCallback<ChangeEvent<string>>> _tfEvents = new();

        public override VisualElement CreateInspectorGUI()
        {
            _blackboard = serializedObject.targetObject as Blackboard;

            var root = new VisualElement();
            // 引用UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Framework/Common/Blackboard/Editor/BlackboardInspector.uxml");
            visualTree.CloneTree(root);
            // 添加USS
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Framework/Common/Blackboard/Editor/BlackboardInspector.uss");
            root.styleSheets.Add(styleSheet);

            // 获取列表子项UXML
            _listViewItem =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Framework/Common/Blackboard/Editor/BlackboardParameterItem.uxml");

            var objectField = root.Q<ObjectField>("ObjectField");
            objectField.SetEnabled(false);
            objectField.objectType = typeof(Blackboard);
            objectField.value = MonoScript.FromScriptableObject(_blackboard);

            _tfParameterName = root.Q<TextField>("TfParameterName");
            _tbAddParameter = root.Q<ToolbarButton>("TbAddParameter");
            _tbRemoveParameter = root.Q<ToolbarButton>("TbRemoveParameter");
            _listView = root.Q<ListView>("ListView");

            _tbAddParameter.clicked += HandleAddParameterClicked;
            _tbRemoveParameter.clicked += HandleRemoveParameterClicked;

            InitListView();

            // 监听ScriptableObject数据变化
            root.TrackSerializedObjectValue(serializedObject, CheckParameterValueChanged);

            return root;
        }

        private void CheckParameterValueChanged(SerializedObject obj)
        {
            _listView.RefreshItems();
        }

        private void OnDestroy()
        {
            if (_tbAddParameter != null)
            {
                _tbAddParameter.clicked -= HandleAddParameterClicked;
            }

            if (_tbRemoveParameter != null)
            {
                _tbRemoveParameter.clicked -= HandleRemoveParameterClicked;
            }

            _ifEvents.Clear();
            _ffEvents.Clear();
            _togEvents.Clear();
        }

        private void HandleAddParameterClicked()
        {
            var dropdownMenu = new GenericMenu();
            dropdownMenu.AddItem(new GUIContent("Int"), false, () =>
            {
                serializedObject.Update();
                _blackboard.AddParameter(new BlackboardVariable
                {
                    key = _tfParameterName.value,
                    type = BlackboardVariableType.Int,
                    intValue = 0,
                });
                serializedObject.ApplyModifiedProperties();
            });
            dropdownMenu.AddItem(new GUIContent("Float"), false, () =>
            {
                serializedObject.Update();
                _blackboard.AddParameter(new BlackboardVariable
                {
                    key = _tfParameterName.value,
                    type = BlackboardVariableType.Float,
                    floatValue = 0f,
                });
                serializedObject.ApplyModifiedProperties();
            });
            dropdownMenu.AddItem(new GUIContent("Bool"), false, () =>
            {
                serializedObject.Update();
                _blackboard.AddParameter(new BlackboardVariable
                {
                    key = _tfParameterName.value,
                    type = BlackboardVariableType.Bool,
                    boolValue = false,
                });
                serializedObject.ApplyModifiedProperties();
            });
            dropdownMenu.AddItem(new GUIContent("String"), false, () =>
            {
                serializedObject.Update();
                _blackboard.AddParameter(new BlackboardVariable
                {
                    key = _tfParameterName.value,
                    type = BlackboardVariableType.String,
                    stringValue = "",
                });
                serializedObject.ApplyModifiedProperties();
            });
            dropdownMenu.DropDown(
                new Rect(_tbAddParameter.worldTransform.GetPosition().x, _tbAddParameter.worldTransform.GetPosition().y,
                    0,
                    _tbAddParameter.contentRect.height)
            );
        }

        private void HandleRemoveParameterClicked()
        {
            if (_listView.selectedItem is BlackboardVariable blackboardVariable)
            {
                serializedObject.Update();
                _blackboard.RemoveParameter(blackboardVariable.key);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void InitListView()
        {
            _listView.itemsSource = _blackboard.parameters;
            _listView.makeItem += _listViewItem.CloneTree;
            _listView.unbindItem += (element, i) =>
            {
                var labelParameterName = element.Q<Label>("LabelParameterName");
                var ifIntValue = element.Q<IntegerField>("IfIntValue");
                var ffFloatValue = element.Q<FloatField>("FfFloatValue");
                var togBooleanValue = element.Q<Toggle>("TogBooleanValue");
                var tfStringValue = element.Q<TextField>("TfStringValue");

                labelParameterName.text = "";
                ifIntValue.style.display = DisplayStyle.None;
                ffFloatValue.style.display = DisplayStyle.None;
                togBooleanValue.style.display = DisplayStyle.None;
                tfStringValue.style.display = DisplayStyle.None;

                if (_ifEvents.TryGetValue(ifIntValue, out var intCallback))
                {
                    ifIntValue.UnregisterCallback(intCallback);
                    _ifEvents.Remove(ifIntValue);
                }

                if (_ffEvents.TryGetValue(ffFloatValue, out var floatCallback))
                {
                    ffFloatValue.UnregisterCallback(floatCallback);
                    _ffEvents.Remove(ffFloatValue);
                }

                if (_togEvents.TryGetValue(togBooleanValue, out var togCallback))
                {
                    togBooleanValue.UnregisterCallback(togCallback);
                    _togEvents.Remove(togBooleanValue);
                }

                if (_tfEvents.TryGetValue(tfStringValue, out var tfCallback))
                {
                    tfStringValue.UnregisterCallback(tfCallback);
                    _tfEvents.Remove(tfStringValue);
                }
            };
            _listView.bindItem += (element, i) =>
            {
                var labelParameterName = element.Q<Label>("LabelParameterName");
                var ifIntValue = element.Q<IntegerField>("IfIntValue");
                var ffFloatValue = element.Q<FloatField>("FfFloatValue");
                var togBooleanValue = element.Q<Toggle>("TogBooleanValue");
                var tfStringValue = element.Q<TextField>("TfStringValue");
                var parameterKey = _blackboard.parameters[i];

                labelParameterName.text = parameterKey.key;
                ifIntValue.style.display = DisplayStyle.None;
                ffFloatValue.style.display = DisplayStyle.None;
                togBooleanValue.style.display = DisplayStyle.None;
                tfStringValue.style.display = DisplayStyle.None;

                switch (parameterKey.type)
                {
                    case BlackboardVariableType.Int:
                        ifIntValue.style.display = DisplayStyle.Flex;
                        ifIntValue.value = _blackboard.GetIntParameter(parameterKey.key);
                        EventCallback<ChangeEvent<int>> intCallback = evt =>
                        {
                            serializedObject.Update();
                            _blackboard.SetIntParameter(parameterKey.key, evt.newValue);
                            serializedObject.ApplyModifiedProperties();
                        };
                        ifIntValue.RegisterValueChangedCallback(intCallback);
                        _ifEvents.Add(ifIntValue, intCallback);
                        break;
                    case BlackboardVariableType.Float:
                        ffFloatValue.style.display = DisplayStyle.Flex;
                        ffFloatValue.value = _blackboard.GetFloatParameter(parameterKey.key);
                        EventCallback<ChangeEvent<float>> floatCallback = evt =>
                        {
                            serializedObject.Update();
                            _blackboard.SetFloatParameter(parameterKey.key, evt.newValue);
                            serializedObject.ApplyModifiedProperties();
                        };
                        ffFloatValue.RegisterValueChangedCallback(floatCallback);
                        _ffEvents.Add(ffFloatValue, floatCallback);
                        break;
                    case BlackboardVariableType.Bool:
                        togBooleanValue.style.display = DisplayStyle.Flex;
                        togBooleanValue.value = _blackboard.GetBoolParameter(parameterKey.key);
                        EventCallback<ChangeEvent<bool>> booleanCallback = evt =>
                        {
                            serializedObject.Update();
                            _blackboard.SetBoolParameter(parameterKey.key, evt.newValue);
                            serializedObject.ApplyModifiedProperties();
                        };
                        togBooleanValue.RegisterValueChangedCallback(booleanCallback);
                        _togEvents.Add(togBooleanValue, booleanCallback);
                        break;
                    case BlackboardVariableType.String:
                        tfStringValue.style.display = DisplayStyle.Flex;
                        tfStringValue.value = _blackboard.GetStringParameter(parameterKey.key);
                        EventCallback<ChangeEvent<string>> stringCallback = evt =>
                        {
                            serializedObject.Update();
                            _blackboard.SetStringParameter(parameterKey.key, evt.newValue);
                            serializedObject.ApplyModifiedProperties();
                        };
                        tfStringValue.RegisterValueChangedCallback(stringCallback);
                        _tfEvents.Add(tfStringValue, stringCallback);
                        break;
                }
            };
        }
    }
}