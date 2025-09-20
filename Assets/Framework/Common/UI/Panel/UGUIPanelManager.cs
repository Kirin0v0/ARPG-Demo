using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Core.Extension;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Framework.Common.UI.Panel
{
    /// <summary>
    /// 层级枚举
    /// </summary>
    public enum UGUIPanelLayer
    {
        // 最底层
        Bottom = 0,

        // 中层
        Middle = 1,

        // 高层
        Top = 2,

        // 系统层
        System = 3,
    }

    /// <summary>
    /// 基于UGUI系统封装的面板管理器，需要提前设置预设体和面板物体，支持展示/隐藏/获取面板
    /// </summary>
    public class UGUIPanelManager
    {
        public delegate void LoadPanelAsset(string panelName, Type panelType, bool isAsync,
            UnityAction<GameObject> callback);

        public delegate void UnloadPanelAsset(string panelName, Type panelType, UnityAction callback);

        /// <summary>
        /// 面板加载基类，主要用于里式替换原则
        /// </summary>
        private abstract class BasePanelAssetLoad
        {
        }

        /// <summary>
        /// 面板加载后操作基类，主要用于里式替换原则
        /// </summary>
        private abstract class BasePanelAssetOperationAfterLoad
        {
        }

        /// <summary>
        /// 面板加载中类，用于异步等待加载完毕，由于多模块可能对同一面板进行展示/隐藏操作，这里规定内部数据记录实际加载完毕后的行为
        /// </summary>
        private class PanelAssetLoading : BasePanelAssetLoad
        {
            public UnityAction LoadCompleteEvent; // 面板加载完成事件
            public BasePanelAssetOperationAfterLoad OperationAfterLoad; // 面板加载后操作
        }

        /// <summary>
        /// 面板加载后展示类，记录展示操作数据
        /// </summary>
        private class PanelAssetToShowAfterLoad<T> : BasePanelAssetOperationAfterLoad where T : BaseUGUIPanel
        {
            public UnityAction<T> BeforeShowEvent; // 面板显示前事件
            public UnityAction<T> AfterShowEvent; // 面板显示后事件
            public UGUIPanelLayer PanelLayer; // 展示层级
            public bool IsFade; // 是否渐隐
            public object Payload; // 展示携带的参数
        }

        /// <summary>
        /// 面板加载后隐藏类，记录展示操作数据
        /// </summary>
        private class PanelAssetToHideAfterLoad : BasePanelAssetOperationAfterLoad
        {
            public UnityAction AfterHideEvent; // 面板隐藏后事件
        }

        /// <summary>
        /// 面板加载后卸载类，记录展示操作数据
        /// </summary>
        private class PanelAssetToUnloadAfterLoad : BasePanelAssetOperationAfterLoad
        {
        }

        /// <summary>
        /// 面板加载完成类，存放面板资源
        /// </summary>
        private class BasePanelAssetLoadComplete : BasePanelAssetLoad
        {
            public readonly BaseUGUIPanel Panel; // 面板对象

            public BasePanelAssetLoadComplete(BaseUGUIPanel panel)
            {
                Panel = panel;
            }
        }

        /// <summary>
        /// 面板加载完成类，存放面板资源
        /// </summary>
        private class PanelAssetLoadComplete<T> : BasePanelAssetLoadComplete where T : BaseUGUIPanel
        {
            public new T Panel => base.Panel as T;

            public PanelAssetLoadComplete(T panel) : base(panel)
            {
            }
        }

        // UI组件
        private readonly Canvas _uiCanvas;

        // 资源加载和卸载委托
        private readonly LoadPanelAsset _loadPanelAsset;
        private readonly UnloadPanelAsset _unloadPanelAsset;

        // 层级父对象，bottom < middle < top < system
        private readonly Transform _recycleLayer;
        private readonly Transform _bottomLayer;
        private readonly Transform _middleLayer;
        private readonly Transform _topLayer;
        private readonly Transform _systemLayer;

        // 存储加载面板的次数，这里不包含展示面板中的加载
        private readonly Dictionary<Type, int> _panelLoadCounts = new();

        // 存储展示面板的加载字典，包含加载中以及加载完毕
        private readonly Dictionary<Type, BasePanelAssetLoad> _showingPanelLoads = new();

        // 存储当前正在展示的面板，隐藏的面板则不在其中
        private readonly Dictionary<BaseUGUIPanel, UGUIPanelLayer> _showingPanels = new();

        public UGUIPanelManager(Canvas uiCanvas, LoadPanelAsset loadPanelAsset, UnloadPanelAsset unloadPanelAsset)
        {
            _uiCanvas = uiCanvas;
            _loadPanelAsset = loadPanelAsset;
            _unloadPanelAsset = unloadPanelAsset;

            // 创建层级对象
            _recycleLayer = CreateLayer("Recycle");
            _bottomLayer = CreateLayer("Bottom");
            _middleLayer = CreateLayer("Middle");
            _topLayer = CreateLayer("Top");
            _systemLayer = CreateLayer("System");
        }

        /// <summary>
        /// 获取对应层级的对象
        /// </summary>
        /// <param name="panelLayer">层级枚举值</param>
        /// <returns></returns>
        public Transform GetLayer(UGUIPanelLayer panelLayer)
        {
            switch (panelLayer)
            {
                case UGUIPanelLayer.Bottom:
                    return _bottomLayer;
                case UGUIPanelLayer.Middle:
                    return _middleLayer;
                case UGUIPanelLayer.Top:
                    return _topLayer;
                case UGUIPanelLayer.System:
                    return _systemLayer;
            }

            return _middleLayer;
        }

        // 管理器销毁函数，用于兜底保证面板资源全部释放
        public void Destroy()
        {
            // 对面板加载/卸载的卸载逻辑
            foreach (var valuePair in _panelLoadCounts)
            {
                if (valuePair.Value > 0)
                {
                    for (int i = 0; i < valuePair.Value; i++)
                    {
                        var panelType = valuePair.Key;
                        var panelName = panelType.Name;
                        // 卸载面板资源
                        _unloadPanelAsset.Invoke(
                            panelName,
                            panelType,
                            null
                        );
                    }
                }
            }

            _panelLoadCounts.Clear();

            // 对面板展示/隐藏的卸载逻辑
            foreach (var valuePair in _showingPanelLoads)
            {
                switch (valuePair.Value)
                {
                    case PanelAssetLoading panelAssetLoading:
                    {
                        panelAssetLoading.OperationAfterLoad = new PanelAssetToUnloadAfterLoad();
                    }
                        break;
                    case BasePanelAssetLoadComplete panelAssetLoadComplete:
                    {
                        var panelName = valuePair.Key.Name;
                        if (!panelAssetLoadComplete.Panel.IsGameObjectDestroyed())
                        {
                            panelAssetLoadComplete.Panel.Hide(false, null);
                            // 解绑层级
                            UnbindPanelFromLayer(panelAssetLoadComplete.Panel);
                            // 销毁面板
                            GameObject.Destroy(panelAssetLoadComplete.Panel.gameObject);
                        }

                        // 卸载面板资源
                        _unloadPanelAsset.Invoke(panelName, valuePair.Key, null);
                    }
                        break;
                }
            }

            _showingPanelLoads.Clear();
            _showingPanels.Clear();
        }

        /// <summary>
        /// 独立加载面板，面板资源预设体名必须和面板类名一致，注意，加载出来的面板是资源对象，需要在外部克隆实例化
        /// 这里不和展示面板的加载逻辑共用，而是独立重新加载，即使存在已展示的面板，也不会复用，而是重新执行加载面板资源逻辑
        /// </summary>
        /// <typeparam name="T">面板的类型</typeparam>
        /// <param name="loadCompleteCallback">加载完毕回调</param>
        /// <param name="isAsync">是否采用异步加载</param>
        public void LoadAlone<T>(UnityAction<T> loadCompleteCallback = null, bool isAsync = true)
            where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 加载面板资源
            _loadPanelAsset.Invoke(
                panelName,
                typeof(T),
                isAsync,
                (res) => { loadCompleteCallback?.Invoke(res.GetComponent<T>()); }
            );
            // 记录加载次数
            _panelLoadCounts[typeof(T)] = _panelLoadCounts.TryGetValue(typeof(T), out var count) ? count + 1 : 1;
        }

        /// <summary>
        /// 独立卸载面板，面板资源预设体名必须和面板类名一致
        /// 这里不和隐藏面板的卸载逻辑共用，而是独立重新卸载，使用时与Load函数配对使用
        /// </summary>
        /// <typeparam name="T">面板的类型</typeparam>
        public void UnloadAlone<T>(UnityAction unloadCompleteCallback = null) where T : BaseUGUIPanel
        {
            var panelType = typeof(T);
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = panelType.Name;
            // 去除未加载就直接卸载的场景
            if (!_panelLoadCounts.TryGetValue(panelType, out var count) || count <= 0)
            {
                unloadCompleteCallback?.Invoke();
                return;
            }

            // 卸载面板资源
            _unloadPanelAsset.Invoke(
                panelName,
                panelType,
                unloadCompleteCallback
            );
            // 记录加载次数
            _panelLoadCounts[panelType] = count - 1;
        }

        /// <summary>
        /// 加载面板资源命令，面板资源预设体名必须和面板类名一致
        /// 这里是服务面板的显隐的资源，而不是独立加载和卸载的逻辑
        /// </summary>
        /// <param name="isAsync">是否采用异步加载</param>
        /// <param name="callback">加载后回调</param>
        /// <typeparam name="T"></typeparam>
        public void ToLoad<T>(UnityAction callback = null, bool isAsync = true) where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 如果存在面板加载，就执行分支逻辑
            if (_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad))
            {
                switch (panelAssetLoad)
                {
                    // 正在加载则追加加载后回调
                    case PanelAssetLoading panelAssetLoading:
                    {
                        panelAssetLoading.LoadCompleteEvent += callback;
                    }
                        break;
                    // 已加载完毕则直接执行回调
                    case PanelAssetLoadComplete<T> panelAssetLoadComplete:
                    {
                        callback?.Invoke();
                    }
                        break;
                }

                return;
            }

            // 面板加载不存在则添加为加载中状态并设置为加载后隐藏操作
            var loading = new PanelAssetLoading
            {
                OperationAfterLoad = new PanelAssetToHideAfterLoad
                {
                    AfterHideEvent = callback
                }
            };
            _showingPanelLoads.Add(typeof(T), loading);
            // 加载面板资源，并在回调中执行加载后操作
            _loadPanelAsset.Invoke(
                panelName,
                typeof(T),
                isAsync,
                ToDoAfterLoadPanel<T>
            );
        }

        /// <summary>
        /// 卸载面板资源命令，面板资源预设体名必须和面板类名一致
        /// 这里是服务面板的显隐的资源，而不是独立加载和卸载的逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ToUnload<T>() where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 如果存在面板加载，则根据其状态进行操作
            if (_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad))
            {
                switch (panelAssetLoad)
                {
                    // 正在加载则设置为加载后卸载面板
                    case PanelAssetLoading panelAssetLoading:
                    {
                        panelAssetLoading.OperationAfterLoad = new PanelAssetToUnloadAfterLoad();
                    }
                        break;
                    // 已加载完毕则直接卸载
                    case PanelAssetLoadComplete<T> panelAssetLoadComplete:
                    {
                        panelAssetLoadComplete.Panel.Hide(false, null);
                        // 解绑层级
                        UnbindPanelFromLayer(panelAssetLoadComplete.Panel);
                        // 销毁面板并卸载资源
                        GameObject.Destroy(panelAssetLoadComplete.Panel.gameObject);
                        _showingPanelLoads.Remove(typeof(T));
                        _unloadPanelAsset.Invoke(panelName, typeof(T), null);
                    }
                        break;
                }
            }
        }

        /// <summary>
        /// 显示面板，面板资源预设体名必须和面板类名一致
        /// </summary>
        /// <typeparam name="T">面板的类型</typeparam>
        /// <param name="panelLayer">面板显示的层级</param>
        /// <param name="beforeShowCallback">显示前回调</param>
        /// <param name="afterShowCallback">显示后回调</param>
        /// <param name="isAsync">是否采用异步加载</param>
        /// <param name="isFade">是否渐变展示</param>
        /// <param name="payload">传递参数</param>
        public void Show<T>(
            UGUIPanelLayer panelLayer = UGUIPanelLayer.Middle,
            UnityAction<T> beforeShowCallback = null,
            UnityAction<T> afterShowCallback = null,
            bool isAsync = true,
            bool isFade = false,
            object payload = null
        ) where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 如果存在面板加载，则根据其状态进行操作
            if (_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad))
            {
                switch (panelAssetLoad)
                {
                    // 正在加载则设置为加载后展示面板
                    case PanelAssetLoading panelAssetLoading:
                    {
                        panelAssetLoading.OperationAfterLoad = new PanelAssetToShowAfterLoad<T>()
                        {
                            BeforeShowEvent = beforeShowCallback,
                            AfterShowEvent = afterShowCallback,
                            PanelLayer = panelLayer,
                            IsFade = isFade,
                            Payload = payload
                        };
                    }
                        break;
                    // 已加载完毕则直接复用
                    case PanelAssetLoadComplete<T> panelAssetLoadComplete:
                    {
                        // 如果失活则重新激活
                        if (!panelAssetLoadComplete.Panel.gameObject.activeSelf)
                        {
                            panelAssetLoadComplete.Panel.gameObject.SetActive(true);
                        }

                        // 执行展示前回调
                        beforeShowCallback?.Invoke(panelAssetLoadComplete.Panel);
                        // 重新绑定层级并显示
                        BindPanelToLayer(panelLayer, panelAssetLoadComplete.Panel);
                        panelAssetLoadComplete.Panel.Show(isFade, payload,
                            (() => { afterShowCallback?.Invoke(panelAssetLoadComplete.Panel); }));
                    }
                        break;
                }

                return;
            }

            // 面板加载不存在则添加为加载中状态并设置为加载后展示操作
            var loading = new PanelAssetLoading
            {
                OperationAfterLoad = new PanelAssetToShowAfterLoad<T>()
                {
                    BeforeShowEvent = beforeShowCallback,
                    AfterShowEvent = afterShowCallback,
                    PanelLayer = panelLayer,
                    IsFade = isFade,
                    Payload = payload
                }
            };
            _showingPanelLoads.Add(typeof(T), loading);
            // 加载面板资源，并在回调中执行加载后操作
            _loadPanelAsset.Invoke(
                panelName,
                typeof(T),
                isAsync,
                ToDoAfterLoadPanel<T>
            );
        }

        /// <summary>
        /// 隐藏面板，并根据是否卸载执行回收面板或卸载面板的逻辑
        /// </summary>
        /// <param name="callBack">隐藏回调</param>
        /// <param name="isFade">是否渐变隐藏</param>
        /// <param name="unloadAfterHide">是否在隐藏后卸载</param>
        /// <typeparam name="T"></typeparam>
        public void Hide<T>(UnityAction callBack = null, bool isFade = false, bool unloadAfterHide = false)
            where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 如果存在面板加载，则根据其状态进行操作
            if (_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad))
            {
                switch (panelAssetLoad)
                {
                    // 正在加载则设置为加载后隐藏或卸载面板
                    case PanelAssetLoading panelAssetLoading:
                    {
                        panelAssetLoading.OperationAfterLoad = unloadAfterHide
                            ? new PanelAssetToUnloadAfterLoad()
                            : new PanelAssetToHideAfterLoad();
                        callBack?.Invoke();
                    }
                        break;
                    // 已加载完毕则直接隐藏或卸载
                    case PanelAssetLoadComplete<T> panelAssetLoadComplete:
                    {
                        // 隐藏面板，待隐藏结束后执行后续逻辑
                        panelAssetLoadComplete.Panel.Hide(isFade, () =>
                        {
                            // 解绑层级
                            UnbindPanelFromLayer(panelAssetLoadComplete.Panel);
                            // 如果需要卸载则执行卸载逻辑，否则仅失活
                            if (unloadAfterHide)
                            {
                                GameObject.Destroy(panelAssetLoadComplete.Panel.gameObject);
                                _showingPanelLoads.Remove(typeof(T));
                                _unloadPanelAsset.Invoke(panelName, typeof(T), null);
                            }
                            else
                            {
                                panelAssetLoadComplete.Panel.gameObject.SetActive(false);
                            }

                            callBack?.Invoke();
                        });
                    }
                        break;
                }
            }
            else
            {
                callBack?.Invoke();
            }
        }

        /// <summary>
        /// 获取正在展示的面板
        /// </summary>
        /// <param name="panel">面板对象</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetShowingPanel<T>(out T panel) where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            if (_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad) &&
                panelAssetLoad is PanelAssetLoadComplete<T> panelAssetLoadComplete)
            {
                panel = panelAssetLoadComplete.Panel;
                return true;
            }

            panel = null;
            return false;
        }

        /// <summary>
        /// 判断面板是否处于焦点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsFocus<T>() where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            //  如果存在面板则获取面板焦点状态
            if (_showingPanelLoads.ContainsKey(typeof(T)) &&
                _showingPanelLoads[typeof(T)] is PanelAssetLoadComplete<T> panelAssetLoadComplete)
            {
                return panelAssetLoadComplete.Panel.Focus;
            }

            return false;
        }

        /// <summary>
        /// 显示指定层级
        /// </summary>
        /// <param name="layer"></param>
        public void VisibleLayer(UGUIPanelLayer layer)
        {
            GetLayer(layer).gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏指定层级
        /// </summary>
        /// <param name="layer"></param>
        public void InvisibleLayer(UGUIPanelLayer layer)
        {
            GetLayer(layer).gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示指定层级以下的层级
        /// </summary>
        /// <param name="layer"></param>
        public void VisibleUntilSpecifiedLayer(UGUIPanelLayer layer)
        {
            for (var i = (int)UGUIPanelLayer.Bottom; i <= (int)UGUIPanelLayer.System; i++)
            {
                if (i == (int)layer)
                {
                    break;
                }

                GetLayer((UGUIPanelLayer)i).gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏指定层级以下的层级
        /// </summary>
        /// <param name="layer"></param>
        public void InvisibleUntilSpecifiedLayer(UGUIPanelLayer layer)
        {
            for (var i = (int)UGUIPanelLayer.Bottom; i <= (int)UGUIPanelLayer.System; i++)
            {
                if (i == (int)layer)
                {
                    break;
                }

                GetLayer((UGUIPanelLayer)i).gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 创建Layer层级
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Transform CreateLayer(string name)
        {
            // 创建Layer对象在Canvas下
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_uiCanvas.transform);
            // 这里是设置参数，全屏且不缩放
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            return rectTransform;
        }

        /// <summary>
        /// 加载面板后的操作逻辑
        /// </summary>
        /// <param name="panelAsset"></param>
        /// <typeparam name="T"></typeparam>
        private void ToDoAfterLoadPanel<T>(GameObject panelAsset) where T : BaseUGUIPanel
        {
            // 获取面板名，预设体名必须和面板类名一致 
            var panelName = typeof(T).Name;
            // 如果面板不是正在加载则直接返回，即不执行任何操作
            if (!_showingPanelLoads.TryGetValue(typeof(T), out var panelAssetLoad) ||
                panelAssetLoad is not PanelAssetLoading panelAssetLoading)
            {
                return;
            }

            // 执行加载完成回调
            panelAssetLoading.LoadCompleteEvent?.Invoke();
            // 获取加载后的操作，根据其类型执行不同操作
            switch (panelAssetLoading.OperationAfterLoad)
            {
                // 显示到指定层级中
                case PanelAssetToShowAfterLoad<T> toShowAfterLoad:
                {
                    // 创建面板预设体并且保持原本的缩放大小
                    var panelObj = GameObject.Instantiate(panelAsset, _recycleLayer, false);
                    // 获取对应UI组件
                    var panel = panelObj.GetComponent<T>();
                    // 绑定层级
                    BindPanelToLayer(toShowAfterLoad.PanelLayer, panel);
                    // 展示UI
                    panelObj.SetActive(true);
                    // 执行展示前回调
                    toShowAfterLoad.BeforeShowEvent?.Invoke(panel);
                    panel.Show(toShowAfterLoad.IsFade, toShowAfterLoad.Payload, (() =>
                    {
                        panel.transform.SetAsLastSibling();
                        // 最后才执行展示后回调，防止回调内部执行Hide函数不能及时查询到加载完毕
                        toShowAfterLoad.AfterShowEvent?.Invoke(panel);
                    }));

                    // 记录为加载资源完毕
                    _showingPanelLoads[typeof(T)] = new PanelAssetLoadComplete<T>(panel);
                }
                    break;
                // 隐藏并回收到回收层级中
                case PanelAssetToHideAfterLoad toHideAfterLoad:
                {
                    // 创建面板预设体并且保持原本的缩放大小
                    var panelObj = GameObject.Instantiate(panelAsset, _recycleLayer, false);
                    // 获取对应UI组件
                    var panel = panelObj.GetComponent<T>();
                    // 解绑层级并不展示UI
                    UnbindPanelFromLayer(panel);
                    panelObj.SetActive(false);
                    // 执行隐藏回调
                    toHideAfterLoad.AfterHideEvent?.Invoke();
                    // 记录为加载资源完毕
                    _showingPanelLoads[typeof(T)] = new PanelAssetLoadComplete<T>(panel);
                }
                    break;
                // 删除加载记录并卸载资源
                case PanelAssetToUnloadAfterLoad toUnloadAfterLoad:
                {
                    _showingPanelLoads.Remove(typeof(T));
                    _unloadPanelAsset.Invoke(panelName, typeof(T), null);
                }
                    break;
            }
        }

        private void BindPanelToLayer(UGUIPanelLayer layer, BaseUGUIPanel panel)
        {
            if (_showingPanels.TryGetValue(panel, out var originPanel))
            {
                if (originPanel != layer)
                {
                    panel.transform.SetParent(GetLayer(layer));
                    _showingPanels[panel] = layer;
                }
            }
            else
            {
                panel.transform.SetParent(GetLayer(layer));
                _showingPanels.Add(panel, layer);
            }

            // 每次绑定后都去检查层级表盘是否聚焦
            CheckPanelLayerFocus();
        }

        private void UnbindPanelFromLayer(BaseUGUIPanel panel)
        {
            if (_showingPanels.Remove(panel))
            {
                panel.transform.SetParent(_recycleLayer);
            }

            // 每次解绑后都去检查层级表盘是否聚焦
            CheckPanelLayerFocus();
        }

        private void CheckPanelLayerFocus()
        {
            if (_showingPanels.Count == 0)
            {
                return;
            }

            var focusLayer = _showingPanels.Values.Max();
            foreach (var pair in _showingPanels)
            {
                pair.Key.Focus = pair.Value == focusLayer;
            }
        }
    }
}