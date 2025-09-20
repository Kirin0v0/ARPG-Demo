using System;
using System.Collections.Generic;
using Framework.Common.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI.Panel
{
    /// <summary>
    /// 基于UGUI封装的Panel基类，内部会自动读取当前控件（控件名不能为默认控件名）
    /// </summary>
    public abstract class BaseUGUIPanel : MonoBehaviour
    {
        private enum PanelOperation
        {
            None,
            ToShow,
            ToHide,
            FadeShow,
            FadeHide,
        }

        // 控件默认名字，如果控件名为默认控件，我们则不会缓存该控件，仅认为该控件是显示控件，不会在Panel类中使用
        private static readonly List<string> DefaultNameList = new List<string>()
        {
            "Image",
            "Text (TMP)",
            "RawImage",
            "Background",
            "Checkmark",
            "Label",
            "Text (Legacy)",
            "Arrow",
            "Placeholder",
            "Fill",
            "Handle",
            "Viewport",
            "Scrollbar Horizontal",
            "Scrollbar Vertical"
        };

        // 存储控件名<-->控件对象的字典，注意控件名称不能冲突
        private readonly Dictionary<string, Component> _widgets = new();

        // 透明度渐变速度
        [SerializeField] private float fadeSpeed = 10f;

        protected CanvasGroup CanvasGroup;

        // 当前是否处于焦点状态，这个由PanelManager管理，如果Panel不是被PanelManager调起的话则需要自己处理焦点状态
        public bool Focus { set; get; } = true;

        // 本次参数
        private object _payload;

        // 本次操作
        private PanelOperation _operation = PanelOperation.None;

        // 是否正在展示
        private bool _showing = false;

        public bool Showing
        {
            get => _showing;
            private set
            {
                if (_showing == value)
                {
                    return;
                }

                _showing = value;
                if (_showing)
                {
                    OnShow(_payload);
                }
                else
                {
                    OnHide();
                }
            }
        }

        // 表盘展示委托
        private event UnityAction AfterShowEvent;

        // 表盘隐藏委托
        private event UnityAction AfterHideEvent;

        protected virtual void Awake()
        {
            // 设置Canvas Group
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            ReloadWidgets();
            OnInit();
        }

        protected virtual void Update()
        {
            HandleOperation();
            
            if (Showing)
            {
                OnShowingUpdate(Focus);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Showing)
            {
                Hide(false);
            }

            OnDispose();
        }

        /// <summary>
        /// 展示表盘
        /// </summary>
        /// <param name="isFade"></param>
        /// <param name="payload"></param>
        /// <param name="afterShowAction"></param>
        public virtual void Show(bool isFade = false, object payload = null, UnityAction afterShowAction = null)
        {
            // 如果设置为展示去重则会判断当前是否正在展示或者即将展示，是则忽略调用
            if (ShowingDebounce() &&
                (Showing || _operation == PanelOperation.ToShow || _operation == PanelOperation.FadeShow))
            {
                return;
            }

            _payload = payload;
            AfterShowEvent = afterShowAction;
            AfterHideEvent = null;

            // 没有渐隐就在本次调用直接处理操作
            if (isFade)
            {
                _operation = PanelOperation.FadeShow;
            }
            else
            {
                _operation = PanelOperation.ToShow;
                HandleOperation();
            }
        }

        /// <summary>
        /// 隐藏表盘
        /// </summary>
        /// <param name="isFade"></param>
        /// <param name="afterHideAction"></param>
        public virtual void Hide(bool isFade = false, UnityAction afterHideAction = null)
        {
            // 如果正在隐藏或处于隐藏操作中则忽略调用
            if (!Showing || _operation == PanelOperation.ToHide || _operation == PanelOperation.FadeHide)
            {
                return;
            }

            _payload = null;
            AfterHideEvent = afterHideAction;
            AfterShowEvent = null;

            // 没有渐隐就在本次调用直接处理操作
            if (isFade)
            {
                _operation = PanelOperation.FadeHide;
            }
            else
            {
                _operation = PanelOperation.ToHide;
                HandleOperation();
            }
        }

        /// <summary>
        /// 获取指定名字以及指定类型的控件
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="name">控件名字</param>
        /// <returns></returns>
        public T GetWidget<T>(string name) where T : Component
        {
            if (_widgets.TryGetValue(name, out var widget))
            {
                T control = widget as T;
                if (control == null)
                    UnityEngine.Debug.LogError($"不存在对应名字{name}类型为{typeof(T)}的组件");
                return control;
            }

            UnityEngine.Debug.LogError($"不存在对应名字{name}的组件");
            return null;
        }

        /// <summary>
        /// 重新加载当前页面控件，避免动态添加控件而获取不到最新控件
        /// </summary>
        public void ReloadWidgets()
        {
            // 为了避免某一个对象上存在两种控件的情况，我们应该优先查找重要的组件
            FindChildrenWidget<RecyclerView.RecyclerView>();
            FindChildrenWidget<Button>();
            FindChildrenWidget<Toggle>();
            FindChildrenWidget<ToggleGroup>();
            FindChildrenWidget<Slider>();
            FindChildrenWidget<TMP_InputField>();
            FindChildrenWidget<InputField>();
            FindChildrenWidget<ScrollRect>();
            FindChildrenWidget<TMP_Dropdown>();
            FindChildrenWidget<Dropdown>();
            // 即使对象上挂在了多个组件，只要优先找到了重要组件，之后也可以通过重要组件得到身上其他挂载的内容
            FindChildrenWidget<Text>();
            FindChildrenWidget<TextMeshProUGUI>();
            FindChildrenWidget<TextMeshPro>();
            FindChildrenWidget<RawImage>();
            FindChildrenWidget<Image>();
            FindChildrenWidget<HorizontalLayoutGroup>();
            FindChildrenWidget<VerticalLayoutGroup>();
            FindChildrenWidget<LayoutElement>();
            FindChildrenWidget<RectTransform>();
        }

        protected virtual bool ShowingDebounce() => true;

        /// <summary>
        /// 初始化界面
        /// </summary>
        protected abstract void OnInit();

        /// <summary>
        /// 页面隐藏=>页面展示时调用函数
        /// </summary>
        /// <param name="payload"></param>
        protected abstract void OnShow(object payload);

        /// <summary>
        /// 页面展示中的渲染帧调用函数
        /// </summary>
        protected abstract void OnShowingUpdate(bool focus);

        /// <summary>
        /// 页面展示=>页面隐藏时调用函数
        /// </summary>
        protected abstract void OnHide();

        protected virtual void OnDispose()
        {
        }

        private void HandleOperation()
        {
            switch (_operation)
            {
                case PanelOperation.ToShow:
                {
                    CanvasGroup.alpha = 1f;
                    CanvasGroup.interactable = true;
                    CanvasGroup.blocksRaycasts = true;
                    Showing = true;
                    AfterShowEvent?.Invoke();
                    AfterShowEvent = null;
                    _operation = PanelOperation.None;
                }
                    break;
                case PanelOperation.ToHide:
                {
                    CanvasGroup.alpha = 0f;
                    CanvasGroup.interactable = false;
                    CanvasGroup.blocksRaycasts = false;
                    Showing = false;
                    AfterHideEvent?.Invoke();
                    AfterHideEvent = null;
                    _operation = PanelOperation.None;
                }
                    break;
                case PanelOperation.FadeShow:
                {
                    CanvasGroup.alpha += fadeSpeed * Time.deltaTime;
                    if (CanvasGroup.alpha >= 1f)
                    {
                        _operation = PanelOperation.ToShow;
                        CanvasGroup.alpha = 1f;
                        HandleOperation();
                    }
                }
                    break;
                case PanelOperation.FadeHide:
                {
                    CanvasGroup.alpha -= fadeSpeed * Time.deltaTime;
                    if (CanvasGroup.alpha <= 0f)
                    {
                        _operation = PanelOperation.ToHide;
                        CanvasGroup.alpha = 0f;
                        HandleOperation();
                    }
                }
                    break;
            }
        }

        /// <summary>
        /// 查询子对象中的泛型类型控件
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        private void FindChildrenWidget<T>() where T : Component
        {
            T[] controls = this.GetComponentsInChildren<T>(true);
            for (int i = 0; i < controls.Length; i++)
            {
                // 获取当前控件的名字
                var name = controls[i].gameObject.name;
                // 如果当前控件为可交互控件则记录到字典中
                if (!_widgets.ContainsKey(name) && !DefaultNameList.Contains(name))
                {
                    _widgets.Add(name, controls[i]);
                }
            }
        }
    }
}