using System;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.UI
{
    public interface IActionChannelUI
    {
        // UI属性
        VisualElement Root { get; }
        VisualElement Parent { get; }
        ActionChannelEditorUIData Data { get; }

        // UI类生命周期函数
        void CreateView(VisualElement parent);
        void BindView(ActionChannelEditorUIData data);
        void DestroyView();

        // UI类辅助函数
        void SetDropdownMenu(Func<GenericDropdownMenu> showDropdownMenu);
        void SetClickChannelCallback(System.Action callback);
    }
}