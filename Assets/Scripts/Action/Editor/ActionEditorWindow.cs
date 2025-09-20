using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Action.Editor.Channel;
using Action.Editor.Channel.Features;
using Action.Editor.Track;
using Action.Editor.Track.Features;
using Character;
using Framework.Common.Audio;
using Framework.Common.Debug;
using JetBrains.Annotations;
using NUnit.Framework;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Action.Editor
{
    public enum ActionEditorFrameRate : int
    {
        FPS24 = 24,
        FPS30 = 30,
        FPS60 = 60,
    }

    public class ActionEditorWindow : EditorWindow
    {
        public static ActionEditorWindow Instance;

        // 轨道事件
        public event System.Action OnTracksRefreshed;

        #region 预设值

        // 编辑器场景
        private const string EditorScenePath = "Assets/Scripts/Action/Editor/ActionEditorScene.unity";

        // 帧时间刻度像素步长
        public const float StandardFrameTimescaleUnitStepPixel = 6f;

        // 帧时间刻度缩放系数
        private const float MinFrameTimescaleScaleFactor = 1f;
        private const float MaxFrameTimescaleScaleFactor = 15f;

        // 帧时间刻度标展示优先级
        private const int FrameTimescalePriority1Step = 10;
        private const float FrameTimescalePriority1LinePercentage = 0.4f;
        private const int FrameTimescalePriority2Step = 5;
        private const float FrameTimescalePriority2LinePercentage = 0.3f;
        private const int FrameTimescalePriority3Step = 1;
        private const float FrameTimescalePriority3LinePercentage = 0.2f;

        // 帧时间刻度数字标识步长
        private const float StandardFrameTimescaleTickStep = 10;

        // 帧时间选择线拖动像素范围
        private const float FrameSelectedLineDragPixelRange = StandardFrameTimescaleUnitStepPixel;

        // 通道和轨道颜色
        public static readonly Color PrimaryChannelColor = new Color(88 / 256f, 88 / 256f, 88 / 256f);
        public static readonly Color SecondaryChannelColor = new Color(42 / 256f, 42 / 256f, 42 / 256f);
        public static readonly Color ProcessTrackNormalColor = new Color(241 / 256f, 196 / 256f, 15 / 256f);
        public static readonly Color ProcessTrackSelectedColor = new Color(247 / 256f, 220 / 256f, 111 / 256f);
        public static readonly Color AnimationTrackNormalColor = new Color(52 / 256f, 152 / 256f, 219 / 256f);
        public static readonly Color AnimationTrackSelectedColor = new Color(133 / 256f, 193 / 256f, 233 / 256f);
        public static readonly Color AudioTrackNormalColor = new Color(46 / 256f, 204 / 256f, 113 / 256f);
        public static readonly Color AudioTrackSelectedColor = new Color(130 / 256f, 224 / 256f, 170 / 256f);
        public static readonly Color EffectTrackNormalColor = new Color(116 / 256f, 0 / 256f, 184 / 256f);
        public static readonly Color EffectTrackSelectedColor = new Color(200 / 256f, 182 / 256f, 255 / 256f);
        public static readonly Color CollideDetectionTrackNormalColor = new Color(239 / 256f, 35 / 256f, 60 / 256f);
        public static readonly Color CollideDetectionTrackSelectedColor = new Color(255 / 256f, 112 / 256f, 166 / 256f);
        public static readonly Color EventTrackNormalColor = new Color(241 / 256f, 196 / 256f, 15 / 256f);
        public static readonly Color EventTrackSelectedColor = new Color(247 / 256f, 220 / 256f, 111 / 256f);

        #endregion

        #region 头部菜单控件

        private Button _btnGotoEditorScene;
        private Button _btnReturnLastScene;
        private ObjectField _ofModelPrefab;
        private Button _btnLoadModel;
        private ObjectField _ofFileConfig;
        private TextField _tfActionName;
        private Button _btnSaveConfig;

        private string _lastScenePath;
        private GameObject _modelPrefab;
        private GameObject _modelPrefabInstance;
        public GameObject PreviewInstance => _modelPrefabInstance;

        private ActionClip _actionClip;
        private bool _configInitial;

        #endregion

        #region 帧时间轴控件

        private Button _btnPreviousFrame;
        private Button _btnPlayOrPause;
        private Button _btnNextFrame;
        private TextField _tfCurrentFrame;
        private TextField _tfTotalFrames;
        private EnumField _efFrameRate;
        private IMGUIContainer _barFrameTimescale;
        private VisualElement _lineFrameSelected;
        private VisualElement _lineFrameSelectedColor;

        private bool _playing;
        private int _startFrameWhenPlaying;
        private DateTime _startTimeWhenPlaying;
        private DateTime _lastPlayingTime;

        private IActionClipPlay _actionClipPlay;

        private IActionClipPlay ActionClipPlay
        {
            get
            {
                // 在初始化动作文件时不可使用动作播放器
                if (_configInitial)
                {
                    return null;
                }

                if (_actionClipPlay == null)
                {
                    if (_modelPrefabInstance && _actionClip)
                    {
                        _actionClipPlay = new ActionClipEditorPlayer(
                            _modelPrefabInstance.GetComponent<AnimancerComponent>(),
                            _modelPrefabInstance.transform);
                    }
                }

                if (_actionClipPlay != null && !Playing && _actionClip)
                {
                    var actionClip = Instantiate(_actionClip);
                    SetNewestActionData(actionClip);
                    _actionClipPlay.ChangeActionClip(actionClip);
                }

                return _actionClipPlay;
            }
        }

        public bool Playing
        {
            get => _playing;
            set
            {
                if (value)
                {
                    _btnPlayOrPause.text = "\u25a0";
                    _btnPlayOrPause.style.backgroundColor = new Color(256 / 256f, 110 / 256f, 0 / 256f);
                    // 如果当前帧是最后一帧就跳转到0帧
                    if (SelectedFrame == TotalFrames)
                    {
                        SelectedFrame = 0;
                    }

                    // 记录播放开始帧
                    _startTimeWhenPlaying = DateTime.Now;
                    _lastPlayingTime = DateTime.Now;
                    _startFrameWhenPlaying = SelectedFrame;
                    ActionClipPlay?.StartAt(_startFrameWhenPlaying);
                }
                else
                {
                    _btnPlayOrPause.text = "\u25b6";
                    _btnPlayOrPause.style.backgroundColor = new Color(88 / 256f, 88 / 256f, 88 / 256f);
                    ActionClipPlay?.Stop();
                }

                _playing = value;
            }
        }

        public int FrameRate { get; set; }

        public float FrameTimeUnit => 1f / FrameRate;

        private float _frameTimescaleScaleFactor;
        public float FrameTimescaleScaleFactor => _frameTimescaleScaleFactor;

        private int _selectedFrame;

        public int SelectedFrame
        {
            get => _selectedFrame;
            private set
            {
                if (_selectedFrame == value)
                {
                    return;
                }

                _selectedFrame = value;
                if (_selectedFrame > TotalFrames)
                {
                    TotalFrames = _selectedFrame;
                }

                _tfCurrentFrame.value = _selectedFrame.ToString();
                // 时间刻度重绘
                _barFrameTimescale.MarkDirtyLayout();
                if (!Playing)
                {
                    // 播放动作内容
                    ActionClipPlay?.PlayAt(value);
                }
            }
        }

        private int _totalFrames;

        public int TotalFrames
        {
            get => _totalFrames;
            private set
            {
                if (_totalFrames == value)
                {
                    return;
                }

                _totalFrames = value;
                if (_totalFrames < SelectedFrame)
                {
                    SelectedFrame = _totalFrames;
                }
                else
                {
                    // 执行轨道刷新事件
                    OnTracksRefreshed?.Invoke();
                }

                _tfTotalFrames.value = _totalFrames.ToString();
                // 重新设置轨道列表宽度
                _listTrack.style.width =
                    StandardFrameTimescaleUnitStepPixel * _frameTimescaleScaleFactor * _totalFrames;
            }
        }

        private bool _dragSelectedLine;
        public bool DragSelectedLine => _dragSelectedLine;

        public float FrameTimescalePixelOffset => Mathf.Abs(_elementTrackListContainer.transform.position.x);

        #endregion

        #region 内容列表控件

        private ScrollView _svChannelList;
        private VisualElement _listChannel;
        private ScrollView _svTrackList;
        private VisualElement _elementTrackListContainer;
        private VisualElement _listTrack;

        private ActionProcessChannelGroup<ActionProcessTrack> _processChannelGroup;
        private ActionProcessChannel<ActionProcessTrack> _processAnticipationChannel;
        private ActionProcessChannel<ActionProcessTrack> _processJudgmentChannel;
        private ActionProcessChannel<ActionProcessTrack> _processRecoveryChannel;

        private ActionAnimationChannelGroup<ActionAnimationTrack> _animationChannelGroup;

        private ActionAudioChannelGroup<ActionAudioTrack> _audioChannelGroup;

        private ActionEffectChannelGroup<ActionEffectTrack> _effectChannelGroup;

        private ActionCollideDetectionChannelGroup<ActionCollideDetectionTrack> _collideDetectionChannelGroup;

        private ActionEventChannelGroup<ActionEventTrack> _eventChannelGroup;

        #endregion

        private DateTime _timerScheduleTime;
        private float _timerTriggerTime;
        private UnityAction _onTimerTriggered;

        [MenuItem("Tools/Game/Action Editor")]
        public static void ShowActionEditorWindow()
        {
            var window = GetWindow<ActionEditorWindow>();
            window.titleContent = new GUIContent("Action Editor");
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
            if (Selection.activeObject is ActionClip actionConfig && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                ShowActionEditorWindow();
                return true;
            }

            return false;
        }

        private void CreateGUI()
        {
            if (Instance)
            {
                DestroyImmediate(Instance);
            }

            Instance = this;

            #region 绑定控件

            var root = rootVisualElement;
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Action/Editor/ActionEditorWindow.uxml");
            visualTree.CloneTree(root);

            _btnGotoEditorScene = rootVisualElement.Q<Button>("BtnGotoEditorScene");
            _btnReturnLastScene = rootVisualElement.Q<Button>("BtnReturnLastScene");
            _ofModelPrefab = rootVisualElement.Q<ObjectField>("OfModelPrefab");
            _btnLoadModel = rootVisualElement.Q<Button>("BtnLoadModel");
            _ofFileConfig = rootVisualElement.Q<ObjectField>("OfFileConfig");
            _tfActionName = rootVisualElement.Q<TextField>("TfActionName");
            _btnSaveConfig = rootVisualElement.Q<Button>("BtnSaveConfig");

            _btnPreviousFrame = rootVisualElement.Q<Button>("BtnPreviousFrame");
            _btnPlayOrPause = rootVisualElement.Q<Button>("BtnPlayOrPause");
            _btnNextFrame = rootVisualElement.Q<Button>("BtnNextFrame");
            _tfCurrentFrame = rootVisualElement.Q<TextField>("TfCurrentFrame");
            _tfTotalFrames = rootVisualElement.Q<TextField>("TfTotalFrames");
            _efFrameRate = rootVisualElement.Q<EnumField>("EfFrameRate");
            _barFrameTimescale = rootVisualElement.Q<IMGUIContainer>("BarFrameTimescale");
            _lineFrameSelected = rootVisualElement.Q<VisualElement>("LineFrameSelected");
            _lineFrameSelectedColor = rootVisualElement.Q<VisualElement>("LineFrameSelectedColor");

            _svChannelList = rootVisualElement.Q<ScrollView>("SvChannelList");
            _listChannel = _svChannelList.Q<VisualElement>("ListChannel");
            _svTrackList = rootVisualElement.Q<ScrollView>("SvTrackList");
            _elementTrackListContainer = _svTrackList.Q<VisualElement>("unity-content-container");
            _listTrack = _svTrackList.Q<VisualElement>("ListTrack");

            #endregion

            _actionClip = null;

            InitTopMenu();
            InitFrameBar();
            InitContentList();
            CreateConfigUI();

            TryToGetActionConfig();

            // 更新场景GUI
            SceneView.duringSceneGui += UpdateSceneGUI;

            // 监听运行/编辑模式
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void Update()
        {
            if (_onTimerTriggered != null &&
                (float)DateTime.Now.Subtract(_timerScheduleTime).TotalSeconds >= _timerTriggerTime)
            {
                _onTimerTriggered?.Invoke();
                _onTimerTriggered = null;
            }

            if (Playing)
            {
                var timeOffset = (float)DateTime.Now.Subtract(_lastPlayingTime).TotalSeconds;
                ActionClipPlay?.Tick(timeOffset);
                SelectedFrame = ActionClipPlay?.CurrentTick ?? SelectedFrame;
                _lastPlayingTime = DateTime.Now;
                if (SelectedFrame == TotalFrames)
                {
                    Playing = false;
                }
            }
        }

        private void OnSelectionChange()
        {
            TryToGetActionConfig();
        }

        private void OnDestroy()
        {
            if (Instance)
            {
                if (_modelPrefabInstance)
                {
                    DestroyImmediate(_modelPrefabInstance);
                }

                DestroyTopMenu();
                DestroyFrameBar();
                DestroyContentList();
                DestroyConfigUI();
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            SceneView.duringSceneGui -= UpdateSceneGUI;
            _actionClip = null;
            Instance = null;
        }

        public int CalculateTimescalePositionNearFrame(float timescalePosition, bool withPixelOffset = true)
        {
            var timescalePixelOffset = (withPixelOffset ? FrameTimescalePixelOffset : 0f) + timescalePosition;
            var frameTimescaleUnitStepPixel = StandardFrameTimescaleUnitStepPixel * _frameTimescaleScaleFactor;
            var passedFrames =
                Mathf.FloorToInt(timescalePixelOffset / frameTimescaleUnitStepPixel);
            var firstFramePixelDistance =
                (passedFrames + 1) * frameTimescaleUnitStepPixel - timescalePixelOffset;

            if (firstFramePixelDistance <= frameTimescaleUnitStepPixel / 2)
            {
                return passedFrames + 1;
            }

            return passedFrames;
        }

        public void UpdateTrackPreview(ActionTrackEditorData trackData)
        {
            // 如果选中帧处于轨道范围内就更新预览
            if (trackData.IsActive(_selectedFrame))
            {
                // 播放动作内容
                ActionClipPlay?.PlayAt(_selectedFrame);
            }
        }

        private void UpdateSceneGUI(SceneView sceneView)
        {
            _collideDetectionChannelGroup?.ChildChannels.ForEach(channel =>
            {
                channel.Tracks.ForEach(track =>
                {
                    if (track is ActionCollideDetectionTrack collideDetectionTrack &&
                        track.Active(SelectedFrame * FrameTimeUnit))
                    {
                        collideDetectionTrack.UpdateSceneView();
                    }
                });
            });
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (activeScene.path == EditorScenePath)
                    {
                        // 停止编辑动作播放器
                        ActionClipPlay?.Stop();
                    }
                }
                    break;
            }
        }

        private void TryToGetActionConfig()
        {
            if (Selection.activeObject is ActionClip actionConfig)
            {
                if (_ofFileConfig.value != actionConfig)
                {
                    _ofFileConfig.value = actionConfig;
                }
            }
        }

        private void InitTopMenu()
        {
            _btnGotoEditorScene.clicked += ClickGotoEditorSceneButton;
            _btnReturnLastScene.clicked += ClickReturnLastSceneButton;
            _btnLoadModel.clicked += ClickLoadModelButton;
            _btnSaveConfig.clicked += ClickSaveConfigButton;
            _ofModelPrefab.RegisterValueChangedCallback(HandleModelPrefabObjectFieldValueChanged);
            _ofFileConfig.RegisterValueChangedCallback(HandleConfigFileObjectFieldValueChanged);

            _ofModelPrefab.value = _modelPrefab;
        }

        private void DestroyTopMenu()
        {
            _btnGotoEditorScene.clicked -= ClickGotoEditorSceneButton;
            _btnReturnLastScene.clicked -= ClickReturnLastSceneButton;
            _btnLoadModel.clicked -= ClickLoadModelButton;
            _btnSaveConfig.clicked -= ClickSaveConfigButton;
            _ofModelPrefab.UnregisterValueChangedCallback(HandleModelPrefabObjectFieldValueChanged);
            _ofFileConfig.UnregisterValueChangedCallback(HandleConfigFileObjectFieldValueChanged);
        }

        private void InitFrameBar()
        {
            // 初始化数据
            Playing = false;
            _frameTimescaleScaleFactor = MinFrameTimescaleScaleFactor;
            SelectedFrame = 0;
            TotalFrames = 100;
            FrameRate = (int)(ActionEditorFrameRate)_efFrameRate.value;
            _dragSelectedLine = false;

            _btnPreviousFrame.clicked += ClickPreviousFrameButton;
            _btnPlayOrPause.clicked += ClickPlayOrPauseButton;
            _btnNextFrame.clicked += ClickNextFrameButton;
            _tfCurrentFrame.RegisterValueChangedCallback(HandleCurrentFrameTextFieldValueChanged);
            _tfTotalFrames.RegisterValueChangedCallback(HandleTotalFramesTextFieldValueChanged);
            _efFrameRate.RegisterValueChangedCallback(HandleFrameRateEnumFieldValueChanged);

            _barFrameTimescale.onGUIHandler = DrawTimescaleBar;
            _barFrameTimescale.RegisterCallback<WheelEvent>(HandleTimescaleWheelEvent);
            _barFrameTimescale.RegisterCallback<MouseDownEvent>(HandleTimescaleMouseDownEvent);
            _barFrameTimescale.RegisterCallback<MouseMoveEvent>(HandleTimescaleMouseMoveEvent);
            _barFrameTimescale.RegisterCallback<MouseUpEvent>(HandleTimescaleMouseUpEvent);
        }

        private void DestroyFrameBar()
        {
            _btnPreviousFrame.clicked -= ClickPreviousFrameButton;
            _btnPlayOrPause.clicked -= ClickPlayOrPauseButton;
            _btnNextFrame.clicked -= ClickNextFrameButton;
            _tfCurrentFrame.UnregisterValueChangedCallback(HandleCurrentFrameTextFieldValueChanged);
            _tfTotalFrames.UnregisterValueChangedCallback(HandleTotalFramesTextFieldValueChanged);
            _efFrameRate.UnregisterValueChangedCallback(HandleFrameRateEnumFieldValueChanged);

            _barFrameTimescale.onGUIHandler = null;
            _barFrameTimescale.UnregisterCallback<WheelEvent>(HandleTimescaleWheelEvent);
            _barFrameTimescale.UnregisterCallback<MouseDownEvent>(HandleTimescaleMouseDownEvent);
            _barFrameTimescale.UnregisterCallback<MouseMoveEvent>(HandleTimescaleMouseMoveEvent);
            _barFrameTimescale.UnregisterCallback<MouseUpEvent>(HandleTimescaleMouseUpEvent);
            _barFrameTimescale.UnregisterCallback<MouseOutEvent>(HandleTimescaleMouseOutEvent);
        }

        private void InitContentList()
        {
            _svChannelList.verticalScroller.valueChanged += HandleChannelListVerticalScroll;
            _svTrackList.verticalScroller.valueChanged += HandleTrackListVerticalScroll;
        }

        private void DestroyContentList()
        {
            _svChannelList.verticalScroller.valueChanged -= HandleChannelListVerticalScroll;
            _svTrackList.verticalScroller.valueChanged -= HandleTrackListVerticalScroll;
        }

        private void ClickGotoEditorSceneButton()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == EditorScenePath)
            {
                return;
            }

            if (_modelPrefabInstance)
            {
                DestroyImmediate(_modelPrefabInstance);
            }

            ScheduleTimer(0.1f, () =>
            {
                _lastScenePath = activeScene.path;
                EditorSceneManager.OpenScene(EditorScenePath);
            });
        }

        private void ClickReturnLastSceneButton()
        {
            if (String.IsNullOrEmpty(_lastScenePath))
            {
                return;
            }

            if (_modelPrefabInstance)
            {
                DestroyImmediate(_modelPrefabInstance);
            }


            ScheduleTimer(0.1f, () =>
            {
                EditorSceneManager.OpenScene(_lastScenePath);
                _lastScenePath = "";
            });
        }

        private void ScheduleTimer(float time, UnityAction action)
        {
            _timerScheduleTime = DateTime.Now;
            _timerTriggerTime = time;
            _onTimerTriggered = action;
        }

        private void ClickLoadModelButton()
        {
            if (!_modelPrefab)
            {
                return;
            }

            if (SceneManager.GetActiveScene().path != EditorScenePath)
            {
                return;
            }

            if (_modelPrefabInstance)
            {
                DestroyImmediate(_modelPrefabInstance);
            }

            _modelPrefabInstance = GameObject.Instantiate(_modelPrefab, Vector3.zero, Quaternion.identity);
            InitModelInstance(_modelPrefabInstance);

            // 重新初始化播放器
            if (_modelPrefabInstance && _actionClip)
            {
                _actionClipPlay = new ActionClipEditorPlayer(_modelPrefabInstance.GetComponent<AnimancerComponent>(),
                    _modelPrefabInstance.transform);
            }
            else
            {
                _actionClipPlay = null;
            }

            void InitModelInstance(GameObject instance)
            {
                var animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                var animancerComponent = instance.GetComponent<AnimancerComponent>();
                if (animancerComponent == null)
                {
                    animancerComponent = instance.AddComponent<AnimancerComponent>();
                }

                animancerComponent.Animator = animator;
                animator.applyRootMotion = true;
            }
        }

        private void HandleModelPrefabObjectFieldValueChanged(ChangeEvent<Object> evt)
        {
            if (evt.newValue is GameObject gameObject)
            {
                _modelPrefab = gameObject;
            }
        }

        private void HandleConfigFileObjectFieldValueChanged(ChangeEvent<Object> value)
        {
            if (value.newValue is ActionClip actionClip && actionClip)
            {
                if (_actionClip == actionClip)
                {
                    return;
                }

                SelectedFrame = 0;
                _actionClip = actionClip;
                if (Selection.activeObject is ActionChannelInspectorSO ||
                    Selection.activeObject is ActionTrackInspectorSO)
                {
                    Selection.activeObject = null;
                }

                CreateConfigUI();
            }
            else
            {
                SelectedFrame = 0;
                _actionClip = null;
                if (Selection.activeObject is ActionChannelInspectorSO ||
                    Selection.activeObject is ActionTrackInspectorSO)
                {
                    Selection.activeObject = null;
                }

                DestroyConfigUI();
            }
        }

        private void ClickPreviousFrameButton()
        {
            SelectedFrame = Mathf.Max(SelectedFrame - 1, 0);
        }

        private void ClickPlayOrPauseButton()
        {
            Playing = !Playing;
        }

        private void ClickNextFrameButton()
        {
            SelectedFrame++;
        }

        private void HandleCurrentFrameTextFieldValueChanged(ChangeEvent<string> value)
        {
            int newValue;
            if (String.IsNullOrEmpty(value.newValue))
            {
                newValue = 0;
            }
            else
            {
                newValue = Mathf.Max(int.Parse(value.newValue), 0);
            }

            SelectedFrame = newValue;
        }

        private void HandleTotalFramesTextFieldValueChanged(ChangeEvent<string> value)
        {
            int newValue;
            if (String.IsNullOrEmpty(value.newValue))
            {
                newValue = 0;
            }
            else
            {
                newValue = Mathf.Max(int.Parse(value.newValue), 0);
            }

            TotalFrames = newValue;
        }

        private void HandleFrameRateEnumFieldValueChanged(ChangeEvent<Enum> value)
        {
            var oldFrameRate = FrameRate;
            FrameRate = (int)(ActionEditorFrameRate)value.newValue;
            // 设置总帧数
            TotalFrames = _actionClip != null
                ? Mathf.RoundToInt(_actionClip.duration * FrameRate)
                : Mathf.RoundToInt(TotalFrames * 1f * FrameRate / oldFrameRate);
            // 同步各通道下轨道的时间
            SynchronizeChannelTime(_processChannelGroup.ChildChannels);
            SynchronizeChannelTime(_animationChannelGroup.ChildChannels);
            SynchronizeChannelTime(_audioChannelGroup.ChildChannels);
            SynchronizeChannelTime(_effectChannelGroup.ChildChannels);
            SynchronizeChannelTime(_collideDetectionChannelGroup.ChildChannels);
            SynchronizeChannelTime(_eventChannelGroup.ChildChannels);

            return;

            void SynchronizeChannelTime(List<IActionChannel> channels)
            {
                foreach (var channel in channels)
                {
                    foreach (var track in channel.Tracks)
                    {
                        track.Data.UpdateFrameRate(FrameRate);
                        track.Bind(track.Data);
                    }
                }
            }
        }

        private void DrawTimescaleBar()
        {
            Handles.BeginGUI();
            var timescaleContentRect = _barFrameTimescale.contentRect;
            var frameTimescaleUnitStepPixel = StandardFrameTimescaleUnitStepPixel * _frameTimescaleScaleFactor;

            // 计算当前偏移后的第一帧是多少帧，从控件开始距离第一帧的距离是多少
            var passedFrames = Mathf.FloorToInt(FrameTimescalePixelOffset / frameTimescaleUnitStepPixel);
            var firstFramePixelDistance =
                (passedFrames + 1) * frameTimescaleUnitStepPixel - FrameTimescalePixelOffset;

            // 这里额外处理，为了让刚好出现的第一帧能够显示出来
            int frame;
            if (Mathf.Approximately(firstFramePixelDistance, frameTimescaleUnitStepPixel))
            {
                frame = passedFrames;
                firstFramePixelDistance = 0f;
            }
            else
            {
                frame = passedFrames + 1;
            }

            // 计算帧记号步长
            var frameTimescaleTickStep = MathF.Ceiling(StandardFrameTimescaleTickStep / _frameTimescaleScaleFactor);

            bool findSelectedFrame = false;

            for (float timescalePixel = firstFramePixelDistance;
                 timescalePixel < timescaleContentRect.width;
                 timescalePixel += frameTimescaleUnitStepPixel)
            {
                // 展示记号帧数
                if (frame % frameTimescaleTickStep == 0)
                {
                    Handles.color = Color.white;
                    var frameStr = frame.ToString();
                    GUI.Label(new Rect(timescalePixel - frameStr.Length * 4.5f, timescaleContentRect.y - 3, 35, 20),
                        frameStr);
                }

                // 展示帧刻度线
                if (frame % FrameTimescalePriority1Step == 0)
                {
                    Handles.color = Color.white;
                    Handles.DrawLine(
                        new Vector2(timescalePixel,
                            Mathf.Clamp01(1 - FrameTimescalePriority1LinePercentage) * timescaleContentRect.height),
                        new Vector2(timescalePixel, timescaleContentRect.height)
                    );
                }

                else if (frame % FrameTimescalePriority2Step == 0)
                {
                    Handles.color = Color.white;
                    Handles.DrawLine(
                        new Vector2(timescalePixel,
                            Mathf.Clamp01(1 - FrameTimescalePriority2LinePercentage) * timescaleContentRect.height),
                        new Vector2(timescalePixel, timescaleContentRect.height)
                    );
                }
                else if (frame % FrameTimescalePriority3Step == 0)
                {
                    Handles.color = Color.grey;
                    Handles.DrawLine(
                        new Vector2(timescalePixel,
                            Mathf.Clamp01(1 - FrameTimescalePriority3LinePercentage) * timescaleContentRect.height),
                        new Vector2(timescalePixel, timescaleContentRect.height)
                    );
                }

                // 如果找到选中帧，就展示选中线以及选中帧数
                if (frame == SelectedFrame)
                {
                    findSelectedFrame = true;

                    if (_dragSelectedLine)
                    {
                        _lineFrameSelectedColor.style.backgroundColor = Color.grey;
                    }
                    else
                    {
                        _lineFrameSelectedColor.style.backgroundColor = Color.white;
                    }

                    var originPosition = _lineFrameSelected.transform.position;
                    _lineFrameSelected.visible = true;
                    _lineFrameSelected.transform.position =
                        new Vector3(timescalePixel, originPosition.y, originPosition.z);
                }

                frame++;
            }

            // 如果没找到选中帧，就隐藏选中线
            if (!findSelectedFrame)
            {
                _lineFrameSelected.visible = false;
            }

            Handles.EndGUI();

            // 执行轨道刷新事件
            OnTracksRefreshed?.Invoke();
        }

        private void HandleTimescaleWheelEvent(WheelEvent wheelEvent)
        {
            // 调整当前时间帧缩放系数，delta.y为正数代表滚轮向下即放大，delta.y为负数代表滚轮向上即缩小
            _frameTimescaleScaleFactor = Mathf.Clamp(_frameTimescaleScaleFactor - wheelEvent.delta.y / 5,
                MinFrameTimescaleScaleFactor, MaxFrameTimescaleScaleFactor);
            // 时间刻度重绘
            _barFrameTimescale.MarkDirtyLayout();
            // 重新设置轨道列表宽度
            _listTrack.style.width = StandardFrameTimescaleUnitStepPixel * _frameTimescaleScaleFactor * _totalFrames;
        }

        private void HandleTimescaleMouseDownEvent(MouseDownEvent mouseDownEvent)
        {
            // 如果鼠标落在拖动范围外，则重新设置选择线对应的帧数
            if (Mathf.Abs(_lineFrameSelected.transform.position.x -
                          (mouseDownEvent.mousePosition.x - _barFrameTimescale.worldTransform.GetPosition().x)) >
                FrameSelectedLineDragPixelRange)
            {
                // 设置选中帧数
                SelectedFrame = CalculateTimescalePositionNearFrame(
                    mouseDownEvent.mousePosition.x - _barFrameTimescale.worldTransform.GetPosition().x
                );
            }

            _dragSelectedLine = true;
        }

        private void HandleTimescaleMouseMoveEvent(MouseMoveEvent mouseMoveEvent)
        {
            if (!_dragSelectedLine)
            {
                return;
            }

            // 设置选中帧数
            SelectedFrame = CalculateTimescalePositionNearFrame(
                mouseMoveEvent.mousePosition.x - _barFrameTimescale.worldTransform.GetPosition().x
            );
        }

        private void HandleTimescaleMouseUpEvent(MouseUpEvent mouseUpEvent)
        {
            if (!_dragSelectedLine)
            {
                return;
            }

            // 设置选中帧数
            SelectedFrame = CalculateTimescalePositionNearFrame(
                mouseUpEvent.mousePosition.x - _barFrameTimescale.worldTransform.GetPosition().x
            );

            _dragSelectedLine = false;
        }

        private void HandleTimescaleMouseOutEvent(MouseOutEvent mouseOutEvent)
        {
            if (!_dragSelectedLine)
            {
                return;
            }

            // 设置选中帧数
            SelectedFrame = CalculateTimescalePositionNearFrame(
                mouseOutEvent.mousePosition.x - _barFrameTimescale.worldTransform.GetPosition().x
            );

            _dragSelectedLine = false;
        }

        private void HandleChannelListVerticalScroll(float value)
        {
            _svTrackList.scrollOffset = new Vector2(_svTrackList.scrollOffset.x, value);
        }

        private void HandleTrackListVerticalScroll(float value)
        {
            _svChannelList.scrollOffset = new Vector2(_svTrackList.scrollOffset.x, value);
        }

        private void CreateConfigUI()
        {
            _configInitial = true;

            DestroyConfigUI();
            if (!_actionClip)
            {
                _configInitial = false;
                return;
            }

            _tfActionName.value = _actionClip.name;
            if ((ActionEditorFrameRate)_actionClip.frameRate != 0)
            {
                _efFrameRate.value = (ActionEditorFrameRate)_actionClip.frameRate;
            }

            TotalFrames = _actionClip.totalTicks;

            CreateProcessChannel();
            CreateAnimationChannel();
            CreateAudioChannel();
            CreateEffectChannel();
            CreateCollideDetectionChannel();
            CreateEventChannel();

            _configInitial = false;

            // 这里重新播放动作
            ActionClipPlay?.PlayAt(SelectedFrame);

            return;

            void CreateProcessChannel()
            {
                _processChannelGroup ??= new ActionProcessChannelGroup<ActionProcessTrack>();
                _processChannelGroup.Init(null, _listChannel, _listTrack);

                var processChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "流程通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = false,
                    TrackNormalColor = ProcessTrackNormalColor,
                    TrackSelectedColor = ProcessTrackSelectedColor,
                };
                _processChannelGroup.Bind(processChannelData);

                _processAnticipationChannel ??= new ActionProcessChannel<ActionProcessTrack>();
                _processChannelGroup.AddChildChannel(_processAnticipationChannel);

                var processAnticipationChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "前摇通道",
                    Color = SecondaryChannelColor,
                    ShowMoreButton = false,
                    TrackNormalColor = ProcessTrackNormalColor,
                    TrackSelectedColor = ProcessTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame,
                };
                _processChannelGroup.UpdateChildChannel(_processAnticipationChannel, processAnticipationChannelData);
                var processAnticipationTracks = new List<ActionTrackEditorData>();
                processAnticipationTracks.Add(new ActionTrackPointEditorData
                {
                    Name = "前摇",
                    Time = _actionClip.process.anticipationTime,
                    Tick = _actionClip.process.anticipationTick,
                    RestrictionStrategy = ActionTrackRestrictionStrategy.RestrictInTotalFrames,
                });
                _processAnticipationChannel.InitTracks(processAnticipationTracks);

                _processJudgmentChannel ??= new ActionProcessChannel<ActionProcessTrack>();
                _processChannelGroup.AddChildChannel(_processJudgmentChannel);

                var processJudgmentChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "判定通道",
                    Color = SecondaryChannelColor,
                    ShowMoreButton = false,
                    TrackNormalColor = ProcessTrackNormalColor,
                    TrackSelectedColor = ProcessTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame,
                };
                _processChannelGroup.UpdateChildChannel(_processJudgmentChannel, processJudgmentChannelData);
                var processJudgmentTracks = new List<ActionTrackEditorData>();
                processJudgmentTracks.Add(new ActionTrackPointEditorData
                {
                    Name = "判定",
                    Time = _actionClip.process.judgmentTime,
                    Tick = _actionClip.process.judgmentTick,
                    RestrictionStrategy = ActionTrackRestrictionStrategy.RestrictInTotalFrames,
                });
                _processJudgmentChannel.InitTracks(processJudgmentTracks);

                _processRecoveryChannel ??= new ActionProcessChannel<ActionProcessTrack>();
                _processChannelGroup.AddChildChannel(_processRecoveryChannel);

                var processRecoveryChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "后摇通道",
                    Color = SecondaryChannelColor,
                    ShowMoreButton = false,
                    TrackNormalColor = ProcessTrackNormalColor,
                    TrackSelectedColor = ProcessTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame,
                };
                _processChannelGroup.UpdateChildChannel(_processRecoveryChannel, processRecoveryChannelData);
                var processRecoveryTracks = new List<ActionTrackEditorData>();
                processRecoveryTracks.Add(new ActionTrackPointEditorData
                {
                    Name = "后摇",
                    Time = _actionClip.process.recoveryTime,
                    Tick = _actionClip.process.recoveryTick,
                    RestrictionStrategy = ActionTrackRestrictionStrategy.RestrictInTotalFrames,
                });
                _processRecoveryChannel.InitTracks(processRecoveryTracks);
            }

            void CreateAnimationChannel()
            {
                #region 创建动画通道组

                _animationChannelGroup ??= new ActionAnimationChannelGroup<ActionAnimationTrack>();
                _animationChannelGroup.Init(null, _listChannel, _listTrack);

                var animationChannelData = new ActionAnimationChannelGroupEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "动画通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = true,
                    TrackNormalColor = AnimationTrackNormalColor,
                    TrackSelectedColor = AnimationTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.None,
                    TransitionLibraryAsset = _actionClip.animation.transitionLibrary,
                };
                _animationChannelGroup.Bind(animationChannelData);

                #endregion

                #region 创建动画子通道

                var animationChannelGroups =
                    _actionClip.animation.animationClips.GroupBy(animationClip => animationClip.channelId);
                foreach (var channelGroup in animationChannelGroups)
                {
                    // 查找动画通道
                    var channelId = channelGroup.Key;
                    var channelIndex =
                        _animationChannelGroup.ChildChannels.FindIndex(channel => channel.Data.Id == channelId);
                    IActionChannel animationChannel;
                    if (channelIndex == -1)
                    {
                        // 如果没有该通道就新建通道
                        animationChannel = new ActionAnimationChannel<ActionAnimationTrack>();
                        _animationChannelGroup.AddChildChannel(animationChannel);

                        // 更新子通道
                        var index = _actionClip.channels.FindIndex(channel => String.Equals(channel.id, channelId));
                        var channelName = index < 0 ? "动画子通道" : _actionClip.channels[index].name;
                        var channelData = CreateAnimationChannelData(channelId, channelName);
                        _animationChannelGroup.UpdateChildChannel(animationChannel, channelData);
                    }
                    else
                    {
                        animationChannel = _animationChannelGroup.ChildChannels[channelIndex];
                    }

                    // 初始化轨道
                    var animationTrackDataList = channelGroup.Select(data =>
                        (ActionTrackEditorData)new ActionAnimationTrackEditorData
                        {
                            Name = data.transition.name,
                            StartTime = data.startTime,
                            Duration = data.duration,
                            StartTick = data.startTick,
                            DurationTicks = data.durationTicks,
                            Transition = data.transition,
                            Speed = data.speed,
                            RestrictionStrategy = ActionTrackRestrictionStrategy.None,
                        }).ToList();
                    animationChannel.InitTracks(animationTrackDataList);
                }

                #endregion
            }

            void CreateAudioChannel()
            {
                #region 创建音频通道组

                _audioChannelGroup ??= new ActionAudioChannelGroup<ActionAudioTrack>();
                _audioChannelGroup.Init(null, _listChannel, _listTrack);

                var audioChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "音频通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = true,
                    TrackNormalColor = AudioTrackNormalColor,
                    TrackSelectedColor = AnimationTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.None,
                };
                _audioChannelGroup.Bind(audioChannelData);

                #endregion

                #region 创建音频子通道

                var audioClipChannelGroups =
                    _actionClip.audio.audioClips.GroupBy(audioClip => audioClip.channelId);
                foreach (var channelGroup in audioClipChannelGroups)
                {
                    // 查找音频通道
                    var channelId = channelGroup.Key;
                    var channelIndex =
                        _audioChannelGroup.ChildChannels.FindIndex(channel => channel.Data.Id == channelId);
                    IActionChannel audioChannel;
                    if (channelIndex == -1)
                    {
                        // 如果没有该通道就新建通道
                        audioChannel = new ActionAudioChannel<ActionAudioTrack>();
                        _audioChannelGroup.AddChildChannel(audioChannel);

                        // 更新子通道
                        var index = _actionClip.channels.FindIndex(channel => String.Equals(channel.id, channelId));
                        var channelName = index < 0 ? "音频子通道" : _actionClip.channels[index].name;
                        var channelData = CreateAudioChannelData(channelId, channelName);
                        _audioChannelGroup.UpdateChildChannel(audioChannel, channelData);
                    }
                    else
                    {
                        audioChannel = _audioChannelGroup.ChildChannels[channelIndex];
                    }

                    // 初始化轨道
                    var audioTrackDataList = channelGroup.Select(data =>
                        (ActionTrackEditorData)new ActionAudioTrackEditorData
                        {
                            Name = data.Name,
                            StartTime = data.startTime,
                            Duration = data.duration,
                            StartTick = data.startTick,
                            DurationTicks = data.durationTicks,
                            Type = data.type,
                            AudioClip = data.specifiedAudioClip,
                            AudioClipRandomizer = data.randomAudioClip,
                            Volume = data.volume,
                            RestrictionStrategy = ActionTrackRestrictionStrategy.None,
                        }).ToList();
                    audioChannel.InitTracks(audioTrackDataList);
                }

                #endregion
            }

            void CreateEffectChannel()
            {
                #region 创建特效通道组

                _effectChannelGroup ??= new ActionEffectChannelGroup<ActionEffectTrack>();
                _effectChannelGroup.Init(null, _listChannel, _listTrack);

                var effectChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "特效通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = true,
                    TrackNormalColor = EffectTrackNormalColor,
                    TrackSelectedColor = EffectTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.None,
                };
                _effectChannelGroup.Bind(effectChannelData);

                #endregion

                #region 创建特效子通道

                var effectChannelGroups =
                    _actionClip.effect.effectClips.GroupBy(clip => clip.channelId);
                foreach (var channelGroup in effectChannelGroups)
                {
                    // 查找特效通道
                    var channelId = channelGroup.Key;
                    var channelIndex =
                        _effectChannelGroup.ChildChannels.FindIndex(channel => channel.Data.Id == channelId);
                    IActionChannel effectChannel;
                    if (channelIndex == -1)
                    {
                        // 如果没有该通道就新建通道
                        effectChannel = new ActionEffectChannel<ActionEffectTrack>();
                        _effectChannelGroup.AddChildChannel(effectChannel);

                        // 更新子通道
                        var index = _actionClip.channels.FindIndex(channel => String.Equals(channel.id, channelId));
                        var channelName = index < 0 ? "特效子通道" : _actionClip.channels[index].name;
                        var channelData = CreateEffectChannelData(channelId, channelName);
                        _effectChannelGroup.UpdateChildChannel(effectChannel, channelData);
                    }
                    else
                    {
                        effectChannel = _effectChannelGroup.ChildChannels[channelIndex];
                    }

                    // 初始化轨道
                    var effectTrackDataList = channelGroup.Select(data =>
                        (ActionTrackEditorData)new ActionEffectTrackEditorData
                        {
                            Name = data.prefab.name,
                            Prefab = data.prefab,
                            StartTime = data.startTime,
                            Duration = data.duration,
                            StartTick = data.startTick,
                            DurationTicks = data.durationTicks,
                            Type = data.type,
                            StartLifetime = data.startLifetime,
                            SimulationSpeed = data.simulationSpeed,
                            LocalPosition = data.localPosition,
                            LocalRotation = data.localRotation,
                            LocalScale = data.localScale,
                            RestrictionStrategy = ActionTrackRestrictionStrategy.None,
                        }
                    ).ToList();
                    effectChannel.InitTracks(effectTrackDataList);
                }

                #endregion
            }

            void CreateCollideDetectionChannel()
            {
                #region 创建碰撞检测通道组

                _collideDetectionChannelGroup ??= new ActionCollideDetectionChannelGroup<ActionCollideDetectionTrack>();
                _collideDetectionChannelGroup.Init(null, _listChannel, _listTrack);

                var collideDetectionChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "碰撞检测通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = true,
                    TrackNormalColor = CollideDetectionTrackNormalColor,
                    TrackSelectedColor = CollideDetectionTrackSelectedColor,
                    TrackSupportAbilities = ActionChannelTrackSupportAbility.None,
                };
                _collideDetectionChannelGroup.Bind(collideDetectionChannelData);

                #endregion

                #region 创建碰撞检测子通道

                var collideDetectionChannelGroups =
                    _actionClip.collideDetection.collideDetectionClips.GroupBy(clip => clip.channelId);
                foreach (var channelGroup in collideDetectionChannelGroups)
                {
                    // 查找碰撞检测通道
                    var channelId = channelGroup.Key;
                    var channelIndex =
                        _collideDetectionChannelGroup.ChildChannels.FindIndex(channel => channel.Data.Id == channelId);
                    IActionChannel collideDetectionChannel;
                    if (channelIndex == -1)
                    {
                        // 如果没有该通道就新建通道
                        collideDetectionChannel = new ActionCollideDetectionChannel<ActionCollideDetectionTrack>();
                        _collideDetectionChannelGroup.AddChildChannel(collideDetectionChannel);

                        // 更新子通道
                        var index = _actionClip.channels.FindIndex(channel => String.Equals(channel.id, channelId));
                        var channelName = index < 0 ? "碰撞检测子通道" : _actionClip.channels[index].name;
                        var channelData = CreateCollideDetectionChannelData(channelId, channelName);
                        _collideDetectionChannelGroup.UpdateChildChannel(collideDetectionChannel, channelData);
                    }
                    else
                    {
                        collideDetectionChannel = _collideDetectionChannelGroup.ChildChannels[channelIndex];
                    }

                    // 初始化轨道
                    var collideDetectionTrackDataList = channelGroup.Select(data =>
                        (ActionTrackEditorData)new ActionCollideDetectionTrackEditorData
                        {
                            Name = ActionCollideDetectionTrackEditorData.GetName(data.type),
                            GroupId = data.groupId,
                            Type = data.type,
                            Data = (ActionCollideDetectionBaseData)data.data.Clone(),
                            StartTime = data.startTime,
                            Duration = data.duration,
                            StartTick = data.startTick,
                            DurationTicks = data.durationTicks,
                            RestrictionStrategy = ActionTrackRestrictionStrategy.None,
                        }
                    ).ToList();
                    collideDetectionChannel.InitTracks(collideDetectionTrackDataList);
                }

                #endregion
            }

            void CreateEventChannel()
            {
                #region 创建事件通道组

                _eventChannelGroup ??= new ActionEventChannelGroup<ActionEventTrack>();
                _eventChannelGroup.Init(null, _listChannel, _listTrack);

                var eventChannelData = new ActionChannelEditorData
                {
                    Id = GUID.Generate().ToString(),
                    Name = "事件通道",
                    Color = PrimaryChannelColor,
                    ShowMoreButton = true,
                    TrackNormalColor = EventTrackNormalColor,
                    TrackSelectedColor = EventTrackSelectedColor,
                };
                _eventChannelGroup.Bind(eventChannelData);

                #endregion

                #region 创建事件子通道

                var eventChannelGroups =
                    _actionClip.events.eventClips.GroupBy(clip => clip.channelId);
                foreach (var channelGroup in eventChannelGroups)
                {
                    // 查找事件通道
                    var channelId = channelGroup.Key;
                    var channelIndex =
                        _eventChannelGroup.ChildChannels.FindIndex(channel => channel.Data.Id == channelId);
                    IActionChannel eventChannel;
                    if (channelIndex == -1)
                    {
                        // 如果没有该通道就新建通道
                        eventChannel = new ActionEventChannel<ActionEventTrack>();
                        _eventChannelGroup.AddChildChannel(eventChannel);

                        // 更新子通道
                        var index = _actionClip.channels.FindIndex(channel => String.Equals(channel.id, channelId));
                        var channelName = index < 0 ? "事件子通道" : _actionClip.channels[index].name;
                        var channelData = CreateEventChannelData(channelId, channelName);
                        _eventChannelGroup.UpdateChildChannel(eventChannel, channelData);
                    }
                    else
                    {
                        eventChannel = _eventChannelGroup.ChildChannels[channelIndex];
                    }

                    // 初始化轨道
                    var eventTrackDataList = channelGroup.Select(data =>
                        (ActionTrackEditorData)new ActionEventTrackEditorData
                        {
                            Name = data.name,
                            Time = data.time,
                            Tick = data.tick,
                            Parameter = data.parameter,
                            BoolPayload = data.boolPayload,
                            IntPayload = data.intPayload,
                            FloatPayload = data.floatPayload,
                            StringPayload = data.stringPayload,
                            ObjectPayload = data.objectPayload,
                            RestrictionStrategy = ActionTrackRestrictionStrategy.None,
                        }
                    ).ToList();
                    eventChannel.InitTracks(eventTrackDataList);
                }

                #endregion
            }
        }

        private void DestroyConfigUI()
        {
            _tfActionName.value = "";

            // 这里停止动作
            ActionClipPlay?.Stop();
            DestroyProcessChannel();
            DestroyAnimationChannel();
            DestroyAudioChannel();
            DestroyEffectChannel();
            DestroyCollideDetectionChannel();
            DestroyEventChannel();

            return;

            void DestroyProcessChannel()
            {
                _processChannelGroup?.Destroy();
                _processChannelGroup = null;
            }

            void DestroyAnimationChannel()
            {
                _animationChannelGroup?.Destroy();
                _animationChannelGroup = null;
            }

            void DestroyAudioChannel()
            {
                _audioChannelGroup?.Destroy();
                _audioChannelGroup = null;
            }

            void DestroyEffectChannel()
            {
                _effectChannelGroup?.Destroy();
                _effectChannelGroup = null;
            }

            void DestroyCollideDetectionChannel()
            {
                _collideDetectionChannelGroup?.Destroy();
                _collideDetectionChannelGroup = null;
            }

            void DestroyEventChannel()
            {
                _eventChannelGroup?.Destroy();
                _eventChannelGroup = null;
            }
        }

        private void ClickSaveConfigButton()
        {
            if (!_actionClip)
            {
                return;
            }

            SetNewestActionData(_actionClip);
            EditorUtility.SetDirty(_actionClip);
            AssetDatabase.SaveAssets();
        }

        private void SetNewestActionData(ActionClip actionClip)
        {
            actionClip.name = _tfActionName.value;
            actionClip.frameRate = FrameRate;
            actionClip.duration = TotalFrames * FrameTimeUnit;
            actionClip.totalTicks = TotalFrames;
            actionClip.channels = new List<ActionChannelData>();

            SetProcessData();
            SetAnimationData();
            SetAudioData();
            SetEffectData();
            SetCollideDetectionData();
            SetEventData();

            return;

            void SetProcessData()
            {
                if (_processAnticipationChannel != null && _processAnticipationChannel.Tracks.Length > 0)
                {
                    var data = (ActionTrackPointEditorData)_processAnticipationChannel.Tracks[0].Data;
                    actionClip.process.anticipationTime = data.Time;
                    actionClip.process.anticipationTick = data.Tick;
                }
                else
                {
                    actionClip.process.anticipationTime = 0f;
                    actionClip.process.anticipationTick = 0;
                }

                if (_processJudgmentChannel != null && _processJudgmentChannel.Tracks.Length > 0)
                {
                    var data = (ActionTrackPointEditorData)_processJudgmentChannel.Tracks[0].Data;
                    actionClip.process.judgmentTime = data.Time;
                    actionClip.process.judgmentTick = data.Tick;
                }
                else
                {
                    actionClip.process.judgmentTime = 0f;
                    actionClip.process.judgmentTick = 0;
                }

                if (_processRecoveryChannel != null && _processRecoveryChannel.Tracks.Length > 0)
                {
                    var data = (ActionTrackPointEditorData)_processRecoveryChannel.Tracks[0].Data;
                    actionClip.process.recoveryTime = data.Time;
                    actionClip.process.recoveryTick = data.Tick;
                }
                else
                {
                    actionClip.process.recoveryTime = 0f;
                    actionClip.process.recoveryTick = 0;
                }
            }

            void SetAnimationData()
            {
                if (_animationChannelGroup != null)
                {
                    actionClip.animation.transitionLibrary =
                        (_animationChannelGroup.Data as ActionAnimationChannelGroupEditorData)!.TransitionLibraryAsset;
                    var animationClipList = new List<ActionAnimationClipData>();
                    foreach (var childChannel in _animationChannelGroup.ChildChannels)
                    {
                        foreach (var childChannelTrack in childChannel.Tracks)
                        {
                            if (childChannelTrack.Data is not ActionAnimationTrackEditorData animationTrackData)
                            {
                                continue;
                            }

                            animationClipList.Add(new ActionAnimationClipData
                            {
                                transition = animationTrackData.Transition,
                                startTime = animationTrackData.StartTime,
                                duration = animationTrackData.Duration,
                                startTick = animationTrackData.StartTick,
                                durationTicks = animationTrackData.DurationTicks,
                                speed = animationTrackData.Speed,
                                channelId = childChannel.Data.Id,
                            });
                        }

                        actionClip.channels.Add(new ActionChannelData
                        {
                            id = childChannel.Data.Id,
                            name = childChannel.Data.Name,
                        });
                    }

                    actionClip.animation.animationClips = animationClipList;
                }
            }

            void SetAudioData()
            {
                if (_audioChannelGroup != null)
                {
                    var audioClipList = new List<ActionAudioClipData>();
                    foreach (var childChannel in _audioChannelGroup.ChildChannels)
                    {
                        foreach (var childChannelTrack in childChannel.Tracks)
                        {
                            if (childChannelTrack.Data is not ActionAudioTrackEditorData actionAudioTrackEditorData)
                            {
                                continue;
                            }

                            audioClipList.Add(new ActionAudioClipData
                            {
                                type = actionAudioTrackEditorData.Type,
                                specifiedAudioClip = actionAudioTrackEditorData.AudioClip,
                                randomAudioClip = actionAudioTrackEditorData.AudioClipRandomizer,
                                startTime = actionAudioTrackEditorData.StartTime,
                                duration = actionAudioTrackEditorData.Duration,
                                startTick = actionAudioTrackEditorData.StartTick,
                                durationTicks = actionAudioTrackEditorData.DurationTicks,
                                volume = actionAudioTrackEditorData.Volume,
                                channelId = childChannel.Data.Id,
                            });
                        }

                        actionClip.channels.Add(new ActionChannelData
                        {
                            id = childChannel.Data.Id,
                            name = childChannel.Data.Name,
                        });
                    }

                    actionClip.audio.audioClips = audioClipList;
                }
            }

            void SetEffectData()
            {
                if (_effectChannelGroup != null)
                {
                    var effectClipList = new List<ActionEffectClipData>();
                    foreach (var childChannel in _effectChannelGroup.ChildChannels)
                    {
                        foreach (var childChannelTrack in childChannel.Tracks)
                        {
                            if (childChannelTrack.Data is not ActionEffectTrackEditorData effectTrackData)
                            {
                                continue;
                            }

                            effectClipList.Add(new ActionEffectClipData
                            {
                                prefab = effectTrackData.Prefab,
                                startTime = effectTrackData.StartTime,
                                duration = effectTrackData.Duration,
                                startTick = effectTrackData.StartTick,
                                durationTicks = effectTrackData.DurationTicks,
                                type = effectTrackData.Type,
                                startLifetime = effectTrackData.StartLifetime,
                                simulationSpeed = effectTrackData.SimulationSpeed,
                                localPosition = effectTrackData.LocalPosition,
                                localRotation = effectTrackData.LocalRotation,
                                localScale = effectTrackData.LocalScale,
                                channelId = childChannel.Data.Id,
                            });
                        }

                        actionClip.channels.Add(new ActionChannelData
                        {
                            id = childChannel.Data.Id,
                            name = childChannel.Data.Name,
                        });
                    }

                    actionClip.effect.effectClips = effectClipList;
                }
            }

            void SetCollideDetectionData()
            {
                if (_collideDetectionChannelGroup != null)
                {
                    var collideDetectionClipList = new List<ActionCollideDetectionClipData>();
                    foreach (var childChannel in _collideDetectionChannelGroup.ChildChannels)
                    {
                        foreach (var childChannelTrack in childChannel.Tracks)
                        {
                            if (childChannelTrack.Data is not ActionCollideDetectionTrackEditorData
                                collideDetectionTrackData)
                            {
                                continue;
                            }

                            collideDetectionClipList.Add(new ActionCollideDetectionClipData
                            {
                                type = collideDetectionTrackData.Type,
                                data = (ActionCollideDetectionBaseData)collideDetectionTrackData.Data.Clone(),
                                startTime = collideDetectionTrackData.StartTime,
                                duration = collideDetectionTrackData.Duration,
                                startTick = collideDetectionTrackData.StartTick,
                                durationTicks = collideDetectionTrackData.DurationTicks,
                                groupId = collideDetectionTrackData.GroupId,
                                channelId = childChannel.Data.Id,
                            });
                        }

                        actionClip.channels.Add(new ActionChannelData
                        {
                            id = childChannel.Data.Id,
                            name = childChannel.Data.Name,
                        });
                    }

                    actionClip.collideDetection.collideDetectionClips = collideDetectionClipList;
                }
            }

            void SetEventData()
            {
                if (_eventChannelGroup != null)
                {
                    var eventClipList = new List<ActionEventClipData>();
                    foreach (var childChannel in _eventChannelGroup.ChildChannels)
                    {
                        foreach (var childChannelTrack in childChannel.Tracks)
                        {
                            if (childChannelTrack.Data is not ActionEventTrackEditorData eventTrackData)
                            {
                                continue;
                            }

                            eventClipList.Add(new ActionEventClipData
                            {
                                name = eventTrackData.Name,
                                time = eventTrackData.Time,
                                tick = eventTrackData.Tick,
                                boolPayload = eventTrackData.BoolPayload,
                                intPayload = eventTrackData.IntPayload,
                                floatPayload = eventTrackData.FloatPayload,
                                stringPayload = eventTrackData.StringPayload,
                                objectPayload = eventTrackData.ObjectPayload,
                                channelId = childChannel.Data.Id,
                            });
                        }

                        actionClip.channels.Add(new ActionChannelData
                        {
                            id = childChannel.Data.Id,
                            name = childChannel.Data.Name,
                        });
                    }

                    actionClip.events.eventClips = eventClipList;
                }
            }
        }

        public ActionChannelEditorData CreateAnimationChannelData(string channelId, [CanBeNull] string channelName)
        {
            return new ActionChannelEditorData
            {
                Id = channelId,
                Name = channelName ?? "动画子通道",
                Color = SecondaryChannelColor,
                ShowMoreButton = true,
                TrackNormalColor = AnimationTrackNormalColor,
                TrackSelectedColor = AnimationTrackSelectedColor,
                TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame |
                                        ActionChannelTrackSupportAbility.DeleteTrack,
            };
        }

        public ActionAnimationTrackEditorData CreateAnimationTrackData(
            TransitionAsset transitionAsset,
            int startTick
        )
        {
            var duration = transitionAsset.MaximumDuration / transitionAsset.Speed;
            return new ActionAnimationTrackEditorData
            {
                Name = transitionAsset.name,
                StartTime = startTick * FrameTimeUnit,
                Duration = duration,
                StartTick = startTick,
                DurationTicks = Mathf.RoundToInt(duration * FrameRate),
                Transition = transitionAsset,
                Speed = 1f,
                RestrictionStrategy = ActionTrackRestrictionStrategy.None,
            };
        }

        public ActionChannelEditorData CreateAudioChannelData(string channelId, [CanBeNull] string channelName)
        {
            return new ActionChannelEditorData
            {
                Id = channelId,
                Name = channelName ?? "音频子通道",
                Color = SecondaryChannelColor,
                ShowMoreButton = true,
                TrackNormalColor = AudioTrackNormalColor,
                TrackSelectedColor = AudioTrackSelectedColor,
                TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame |
                                        ActionChannelTrackSupportAbility.DeleteTrack,
            };
        }

        public ActionAudioTrackEditorData CreateAudioTrackSpecifiedData(
            AudioClip audioClip,
            int startTick
        )
        {
            return new ActionAudioTrackEditorData
            {
                Name = audioClip.name,
                StartTime = startTick * FrameTimeUnit,
                Duration = audioClip.length,
                StartTick = startTick,
                DurationTicks = Mathf.RoundToInt(audioClip.length * FrameRate),
                Type = ActionAudioType.Specified,
                AudioClip = audioClip,
                Volume = 1f,
                RestrictionStrategy = ActionTrackRestrictionStrategy.None,
            };
        }

        public ActionAudioTrackEditorData CreateAudioTrackRandomData(
            AudioClipRandomizer audioClipRandomizer,
            int startTick
        )
        {
            return new ActionAudioTrackEditorData
            {
                Name = audioClipRandomizer.name,
                StartTime = startTick * FrameTimeUnit,
                Duration = audioClipRandomizer.averageLength,
                StartTick = startTick,
                DurationTicks = Mathf.RoundToInt(audioClipRandomizer.averageLength * FrameRate),
                Type = ActionAudioType.Random,
                AudioClipRandomizer = audioClipRandomizer,
                Volume = 1f,
                RestrictionStrategy = ActionTrackRestrictionStrategy.None,
            };
        }

        public ActionChannelEditorData CreateEffectChannelData(string channelId, [CanBeNull] string channelName)
        {
            return new ActionChannelEditorData
            {
                Id = channelId,
                Name = channelName ?? "特效子通道",
                Color = SecondaryChannelColor,
                ShowMoreButton = true,
                TrackNormalColor = EffectTrackNormalColor,
                TrackSelectedColor = EffectTrackSelectedColor,
                TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame |
                                        ActionChannelTrackSupportAbility.DeleteTrack |
                                        ActionChannelTrackSupportAbility.CopyToSelectedFrame
            };
        }

        public ActionEffectTrackEditorData CreateEffectTrackData(
            GameObject prefab,
            int startTick,
            float duration
        )
        {
            return new ActionEffectTrackEditorData
            {
                Name = prefab.name,
                Prefab = prefab,
                StartTime = startTick * FrameTimeUnit,
                Duration = duration,
                StartTick = startTick,
                DurationTicks = Mathf.RoundToInt(duration * FrameRate),
                Type = ActionEffectType.Dynamic,
                StartLifetime = 0f,
                LocalPosition = Vector3.zero,
                LocalRotation = Quaternion.identity,
                LocalScale = Vector3.one,
                RestrictionStrategy = ActionTrackRestrictionStrategy.None,
            };
        }

        public ActionChannelEditorData CreateCollideDetectionChannelData(string channelId,
            [CanBeNull] string channelName)
        {
            return new ActionChannelEditorData
            {
                Id = channelId,
                Name = channelName ?? "碰撞检测子通道",
                Color = SecondaryChannelColor,
                ShowMoreButton = true,
                TrackNormalColor = CollideDetectionTrackNormalColor,
                TrackSelectedColor = CollideDetectionTrackSelectedColor,
                TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame |
                                        ActionChannelTrackSupportAbility.DeleteTrack |
                                        ActionChannelTrackSupportAbility.CopyToSelectedFrame
            };
        }

        public ActionCollideDetectionTrackEditorData CreateCollideDetectionTrackData(
            string groupId,
            int startTick,
            float duration
        )
        {
            return new ActionCollideDetectionTrackEditorData
            {
                Name = ActionCollideDetectionTrackEditorData.GetName(ActionCollideDetectionType.None),
                GroupId = groupId,
                Type = ActionCollideDetectionType.None,
                Data = new ActionCollideDetectionEmptyData(),
                StartTime = startTick * FrameTimeUnit,
                Duration = duration,
                StartTick = startTick,
                DurationTicks = Mathf.RoundToInt(duration * FrameRate),
                RestrictionStrategy = ActionTrackRestrictionStrategy.None,
            };
        }

        public ActionChannelEditorData CreateEventChannelData(string channelId,
            [CanBeNull] string channelName)
        {
            return new ActionChannelEditorData
            {
                Id = channelId,
                Name = channelName ?? "事件子通道",
                Color = SecondaryChannelColor,
                ShowMoreButton = true,
                TrackNormalColor = EventTrackNormalColor,
                TrackSelectedColor = EventTrackSelectedColor,
                TrackSupportAbilities = ActionChannelTrackSupportAbility.MoveToSelectedFrame |
                                        ActionChannelTrackSupportAbility.DeleteTrack
            };
        }

        public ActionEventTrackEditorData CreateEventTrackData(string name, int tick)
        {
            return new ActionEventTrackEditorData
            {
                Name = name,
                Time = tick * FrameTimeUnit,
                Tick = tick,
                Parameter = ActionEventParameter.None,
                RestrictionStrategy = ActionTrackRestrictionStrategy.None
            };
        }
    }
}