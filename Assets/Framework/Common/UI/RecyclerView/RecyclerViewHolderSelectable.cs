using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView
{
    /// <summary>
    /// 服务于RecyclerViewHolder的导航选择行为，具体用法步骤如下：
    /// 1.为RecyclerViewHolder绑定RecyclerViewHolderSelectable
    /// 2.在具体业务设置各种回调
    /// </summary>
    public class RecyclerViewHolderSelectable : ListenStateButton
    {
        // 导航回调
        public UnityAction OnNavigationSelect;
        public UnityAction OnNavigationDeselect;
        public UnityAction<MoveDirection> OnNavigationMove;
        
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            OnNavigationSelect?.Invoke();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            OnNavigationDeselect?.Invoke();
        }

        public override void OnMove(AxisEventData eventData)
        {
            // 这里直接覆盖导航的切换逻辑，由子类自身实现导航切换功能
            OnMove(eventData.moveDir);
        }

        protected virtual void OnMove(MoveDirection moveDirection)
        {
            OnNavigationMove?.Invoke(moveDirection);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            // 由于RecyclerViewHolder采用复用机制，所以不使用原生导航切换功能，仅使用原生导航的选中和取消选中回调
            navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = null,
                selectOnDown = null,
                selectOnLeft = null,
                selectOnRight = null,
                wrapAround = false,
            };
        }
#endif
    }
}