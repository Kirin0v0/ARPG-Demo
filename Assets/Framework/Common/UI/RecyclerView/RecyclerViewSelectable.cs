using System;
using System.Linq;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView
{
    public delegate GameObject RecyclerViewSelectableTransfer(GameObject from);

    /// <summary>
    /// 服务于RecyclerView的导航选择行为，具体用法步骤如下：
    /// 1.外部UI元素导航绑定RecyclerView
    /// 2.为RecyclerView绑定RecyclerViewSelectable并添加导航信息，与外部绑定一致
    /// 3.在具体业务设置RecyclerViewSelectableTransfer代理（默认从上方和左方导航过来的切换到第一个ViewHolder，从下方和右方导航过来的切换到最后一个ViewHolder）
    /// </summary>
    [RequireComponent(typeof(RecyclerView))]
    public class RecyclerViewSelectable : Selectable
    {
        public RecyclerViewSelectableTransfer FromUpSelectableTransfer;
        public RecyclerViewSelectableTransfer FromLeftSelectableTransfer;
        public RecyclerViewSelectableTransfer FromRightSelectableTransfer;
        public RecyclerViewSelectableTransfer FromDownSelectableTransfer;
        public RecyclerViewSelectableTransfer FromUnknownSelectableTransfer;
        public RecyclerViewSelectableTransfer GlobalSelectableTransfer; // 该委托是全局委托，优先级比上述的方向委托高

        [SerializeField] private bool passSelectedEventWhenEmpty = true;

        private RecyclerView _recyclerView;
        private GameObject _selectedGameObject;

        protected override void Awake()
        {
            base.Awake();
            _recyclerView = GetComponent<RecyclerView>();
            // 默认设置导航代理
            FromUpSelectableTransfer ??= SelectFirstViewHolder;
            FromLeftSelectableTransfer ??= SelectFirstViewHolder;
            FromRightSelectableTransfer ??= SelectLastViewHolder;
            FromDownSelectableTransfer ??= SelectLastViewHolder;
            FromUnknownSelectableTransfer ??= SelectFirstViewHolder;
        }

        private void LateUpdate()
        {
            // 如果当前选中该组件，则不记录选中GameObject
            if (EventSystem.current?.currentSelectedGameObject == gameObject)
            {
                SelectSelf();
                return;
            }

            _selectedGameObject = EventSystem.current?.currentSelectedGameObject;
        }

        public void SelectSelf()
        {
            if (GlobalSelectableTransfer != null)
            {
                if (FromUnknownSelectableTransfer != null)
                {
                    var selectable = FromUnknownSelectableTransfer(_selectedGameObject);
                    EventSystem.current?.SetSelectedGameObject(selectable != null
                        ? selectable
                        : _selectedGameObject);
                }
                else
                {
                    EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                }
            }
            
            // 通过转换代理获取当前指向GameObject，如果没有就判断是否传递选中事件，是则传递至同方向的绑定控件上，否则恢复到先前的GameObject
            var fromDirection = CalculateFromDirection(_selectedGameObject);
            switch (fromDirection)
            {
                case MoveDirection.Up:
                {
                    if (FromUpSelectableTransfer != null)
                    {
                        var selectable = FromUpSelectableTransfer(_selectedGameObject);
                        if (selectable != null)
                        {
                            EventSystem.current?.SetSelectedGameObject(selectable);
                        }
                        else
                        {
                            var selected = (passSelectedEventWhenEmpty
                                ? navigation.selectOnDown?.gameObject
                                : _selectedGameObject) ?? _selectedGameObject;
                            EventSystem.current?.SetSelectedGameObject(selected);
                        }
                    }
                    else
                    {
                        EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                    }
                }
                    break;
                case MoveDirection.Left:
                {
                    if (FromLeftSelectableTransfer != null)
                    {
                        var selectable = FromLeftSelectableTransfer(_selectedGameObject);
                        if (selectable != null)
                        {
                            EventSystem.current?.SetSelectedGameObject(selectable);
                        }
                        else
                        {
                            var selected = (passSelectedEventWhenEmpty
                                ? navigation.selectOnRight?.gameObject
                                : _selectedGameObject) ?? _selectedGameObject;
                            EventSystem.current?.SetSelectedGameObject(selected);
                        }
                    }
                    else
                    {
                        EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (FromDownSelectableTransfer != null)
                    {
                        var selectable = FromDownSelectableTransfer(_selectedGameObject);
                        if (selectable != null)
                        {
                            EventSystem.current?.SetSelectedGameObject(selectable);
                        }
                        else
                        {
                            var selected = (passSelectedEventWhenEmpty
                                ? navigation.selectOnUp?.gameObject
                                : _selectedGameObject) ?? _selectedGameObject;
                            EventSystem.current?.SetSelectedGameObject(selected);
                        }
                    }
                    else
                    {
                        EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                    }
                }
                    break;
                case MoveDirection.Right:
                {
                    if (FromRightSelectableTransfer != null)
                    {
                        var selectable = FromRightSelectableTransfer(_selectedGameObject);
                        if (selectable != null)
                        {
                            EventSystem.current?.SetSelectedGameObject(selectable);
                        }
                        else
                        {
                            var selected = (passSelectedEventWhenEmpty
                                ? navigation.selectOnLeft?.gameObject
                                : _selectedGameObject) ?? _selectedGameObject;
                            EventSystem.current?.SetSelectedGameObject(selected);
                        }
                    }
                    else
                    {
                        EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                    }
                }
                    break;
                default:
                {
                    if (FromUnknownSelectableTransfer != null)
                    {
                        var selectable = FromUnknownSelectableTransfer(_selectedGameObject);
                        EventSystem.current?.SetSelectedGameObject(selectable != null
                            ? selectable
                            : _selectedGameObject);
                    }
                    else
                    {
                        EventSystem.current?.SetSelectedGameObject(_selectedGameObject);
                    }
                }
                    break;
            }
        }

        private GameObject SelectFirstViewHolder(GameObject from)
        {
            return _recyclerView.RecyclerQuery.GetVisibleViewHolders().OrderBy(viewHolder => viewHolder.Position)
                .FirstOrDefault()?.gameObject;
        }

        private GameObject SelectLastViewHolder(GameObject from)
        {
            return _recyclerView.RecyclerQuery.GetVisibleViewHolders()
                .OrderByDescending(viewHolder => viewHolder.Position)
                .FirstOrDefault()?.gameObject;
        }

        private MoveDirection CalculateFromDirection(GameObject previousSelectedGameObject)
        {
            if (!previousSelectedGameObject)
            {
                return MoveDirection.None;
            }

            if (previousSelectedGameObject == navigation.selectOnUp?.gameObject)
            {
                return MoveDirection.Up;
            }

            if (previousSelectedGameObject == navigation.selectOnLeft?.gameObject)
            {
                return MoveDirection.Left;
            }

            if (previousSelectedGameObject == navigation.selectOnRight?.gameObject)
            {
                return MoveDirection.Right;
            }

            if (previousSelectedGameObject == navigation.selectOnDown?.gameObject)
            {
                return MoveDirection.Down;
            }

            return MoveDirection.None;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            interactable = false;
            transition = Transition.None;
            if (navigation.mode != Navigation.Mode.Explicit)
            {
                navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit
                };
            }
        }
#endif
    }
}