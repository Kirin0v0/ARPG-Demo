using UnityEngine.Events;
using UnityEngine.UI;

namespace Framework.Common.UI
{
    public class ListenStateButton : Button
    {
        // 状态回调
        public UnityAction OnSelectionStateNormal;
        public UnityAction OnSelectionStateHighlighted;
        public UnityAction OnSelectionStatePressed;
        public UnityAction OnSelectionStateSelected;
        public UnityAction OnSelectionStateDisabled;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            switch (state)
            {
                case SelectionState.Highlighted:
                    OnSelectionStateHighlighted?.Invoke();
                    break;
                case SelectionState.Pressed:
                    OnSelectionStatePressed?.Invoke();
                    break;
                case SelectionState.Selected:
                    OnSelectionStateSelected?.Invoke();
                    break;
                case SelectionState.Disabled:
                    OnSelectionStateDisabled?.Invoke();
                    break;
                case SelectionState.Normal:
                default:
                    OnSelectionStateNormal?.Invoke();
                    break;
            }
        }
    }
}