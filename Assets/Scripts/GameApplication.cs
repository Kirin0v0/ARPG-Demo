using System;
using System.Diagnostics;
using System.Reflection;
using Archive;
using Archive.Data;
using Archive.Security;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Excel;
using Framework.Common.Resource;
using Framework.Common.Util;
using Framework.Core.Event;
using Framework.Core.Singleton;
using UnityEngine;
using Debugger = Framework.Common.Debug.Debugger;

/// <summary>
/// 游戏生命周期单例类，主要用于管理C#类对象单例
/// 此外，还作为应用生命周期跨场景类执行一些全局业务
/// </summary>
public class GameApplication : MonoGlobalSingleton<GameApplication>, IArchivable
{
    public AddressablesManager AddressablesManager { private set; get; }

    public GlobalSettingsDataManager GlobalSettingsDataManager { private set; get; }

    public ExcelBinaryManager ExcelBinaryManager { private set; get; }

    public ArchiveManager ArchiveManager { private set; get; }

    public EventCenter EventCenter { private set; get; }

    public int CurrentArchiveId { set; get; } = -1; // 跨场景传递当前存档Id

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
#if UNITY_EDITOR
        // 在场景加载前重置全局单例Mono类标识符
        MonoGlobalSingletonResetter.Reset();
#endif
        // 初始化调试
        Debugger.Instance.Init(new DebugConfig
        {
            Enable = true,
            LogError = true,
            LogPrefix = "Debugger",
        });
        DebugFpsSystem.Instance.enabled = true;
        // 初始化显示配置
        Instance.InitDisplaySettings();
    }

    private GameApplication()
    {
    }

    public override void OnSingletonInit()
    {
        base.OnSingletonInit();

        // 初始化单例对象
        AddressablesManager = new AddressablesManager();
        GlobalSettingsDataManager = new GlobalSettingsDataManager();
        ExcelBinaryManager = new ExcelBinaryManager();
        ArchiveManager = new ArchiveManager(
            $"{Application.persistentDataPath}/Archives",
            15,
            new NoSecurityStrategy()
        );
        EventCenter = new();

        // 注册存档监听
        ArchiveManager.Register(this);

        // 添加全局配置监听
        GlobalSettingsDataManager.OnScreenModeChanged += HandleScreenModeAndResolutionAndFrameRateChanged;
        GlobalSettingsDataManager.OnResolutionChanged += HandleScreenModeAndResolutionAndFrameRateChanged;
        GlobalSettingsDataManager.OnFrameRateChanged += HandleScreenModeAndResolutionAndFrameRateChanged;
    }

    public override void Dispose()
    {
        AddressablesManager.ClearAllAssets();
        
        // 解除注册存档监听
        ArchiveManager.Unregister(this);

        // 删除全局配置监听
        GlobalSettingsDataManager.OnScreenModeChanged -= HandleScreenModeAndResolutionAndFrameRateChanged;
        GlobalSettingsDataManager.OnResolutionChanged -= HandleScreenModeAndResolutionAndFrameRateChanged;
        GlobalSettingsDataManager.OnFrameRateChanged -= HandleScreenModeAndResolutionAndFrameRateChanged;

        base.Dispose();
    }

    private void Update()
    {
        ArchiveManager.UpdatePlayTime(Time.unscaledDeltaTime);
    }

    public void InitDisplaySettings()
    {
        HandleScreenModeAndResolutionAndFrameRateChanged();
    }

    public void Save(ArchiveData archiveData)
    {
    }

    public void Load(ArchiveData archiveData)
    {
        CurrentArchiveId = archiveData.id;
    }

    private void HandleScreenModeAndResolutionAndFrameRateChanged()
    {
        // 设置屏幕模式、分辨率和刷新率
        var width = GlobalSettingsDataManager.DisplayResolution switch
        {
            DisplayResolution.W1920H1080 => 1920,
            DisplayResolution.W1280H960 => 1280,
            DisplayResolution.W1440H1080 => 1440,
            DisplayResolution.W2560H1440 => 2560,
            _ => 1920
        };
        var height = GlobalSettingsDataManager.DisplayResolution switch
        {
            DisplayResolution.W1920H1080 => 1080,
            DisplayResolution.W1280H960 => 960,
            DisplayResolution.W1440H1080 => 1080,
            DisplayResolution.W2560H1440 => 1440,
            _ => 1080
        };
        var fullScreenMode = GlobalSettingsDataManager.ScreenMode switch
        {
            ScreenMode.ExclusiveFullScreen => FullScreenMode.ExclusiveFullScreen,
            ScreenMode.FullScreenWindow => FullScreenMode.FullScreenWindow,
            ScreenMode.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.ExclusiveFullScreen
        };
        var refreshRate = GlobalSettingsDataManager.FrameRate switch
        {
            FrameRate.FPS60 => new RefreshRate
            {
                numerator = 60,
                denominator = 1U
            },
            FrameRate.FPS30 => new RefreshRate
            {
                numerator = 30,
                denominator = 1U
            },
            FrameRate.FPS120 => new RefreshRate
            {
                numerator = 120,
                denominator = 1U
            },
            _ => new RefreshRate
            {
                numerator = 60,
                denominator = 1U
            }
        };
        Screen.SetResolution(width, height, fullScreenMode, refreshRate);
        // 设置游戏帧率
        Application.targetFrameRate = GlobalSettingsDataManager.FrameRate switch
        {
            FrameRate.FPS60 => 60,
            FrameRate.FPS30 => 30,
            FrameRate.FPS120 => 120,
            _ => 60
        };
    }
}