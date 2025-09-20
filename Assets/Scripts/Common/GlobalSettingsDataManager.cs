using Framework.Common.Util;
using UnityEngine;

namespace Common
{
    public enum ScreenMode
    {
        ExclusiveFullScreen,
        FullScreenWindow,
        Windowed,
    }

    public enum DisplayResolution
    {
        W1920H1080,
        W1280H960,
        W1440H1080,
        W2560H1440,
    }

    public enum FrameRate
    {
        FPS60,
        FPS30,
        FPS120,
    }

    /// <summary>
    /// 专门用于获取全局配置数据的管理器
    /// </summary>
    public class GlobalSettingsDataManager
    {
        private ScreenMode? _screenMode;

        public ScreenMode ScreenMode
        {
            get
            {
                if (_screenMode.HasValue) return _screenMode.Value;
                var index = PlayerPrefsUtil.LoadData("ScreenMode", typeof(int), 0);
                _screenMode = (ScreenMode)index;
                return _screenMode.Value;
            }
            set
            {
                if (_screenMode == value)
                {
                    return;
                }

                _screenMode = value;
                PlayerPrefsUtil.SaveData("ScreenMode", (int)value);
                OnScreenModeChanged?.Invoke();
            }
        }
        public event System.Action OnScreenModeChanged;
        
        private DisplayResolution? _displayResolution;
        public DisplayResolution DisplayResolution
        {
            get
            {
                if (_displayResolution.HasValue) return _displayResolution.Value;
                var index = (int)PlayerPrefsUtil.LoadData("DisplayResolution", typeof(int), 0);
                _displayResolution = (DisplayResolution)index;
                return _displayResolution.Value;
            }
            set
            {
                if (_displayResolution == value)
                {
                    return;
                }

                _displayResolution = value;
                PlayerPrefsUtil.SaveData("DisplayResolution", (int)value);
                OnResolutionChanged?.Invoke();
            }
        }
        public event System.Action OnResolutionChanged;
        
        private FrameRate? _frameRate;
        public FrameRate FrameRate
        {
            get
            {
                if (_frameRate.HasValue) return _frameRate.Value;
                var index = (int)PlayerPrefsUtil.LoadData("FrameRate", typeof(int), 0);
                _frameRate = (FrameRate)index;
                return _frameRate.Value;
            }
            set
            {
                if (_frameRate == value)
                {
                    return;
                }

                _frameRate = value;
                PlayerPrefsUtil.SaveData("FrameRate", (int)value);
                OnFrameRateChanged?.Invoke();
            }
        }
        public event System.Action OnFrameRateChanged;
        
        private float? _musicVolume;
        public float MusicVolume
        {
            get
            {
                if (_musicVolume.HasValue) return _musicVolume.Value;
                var volume = (float)PlayerPrefsUtil.LoadData("MusicVolume", typeof(float), 100f);
                _musicVolume = volume;
                return volume;
            }
            set
            {
                if (_musicVolume.HasValue && Mathf.Approximately(_musicVolume.Value, value))
                {
                    return;
                }

                _musicVolume = value;
                PlayerPrefsUtil.SaveData("MusicVolume", value);
                OnMusicVolumeChanged?.Invoke();
            }
        }
        public event System.Action OnMusicVolumeChanged;
        
        private float? _soundVolume;
        public float SoundVolume
        {
            get
            {
                if (_soundVolume.HasValue) return _soundVolume.Value;
                var volume = (float)PlayerPrefsUtil.LoadData("SoundVolume", typeof(float), 100f);
                _soundVolume = volume;
                return volume;
            }
            set
            {
                if (_soundVolume.HasValue && Mathf.Approximately(_soundVolume.Value, value))
                {
                    return;
                }

                _soundVolume = value;
                PlayerPrefsUtil.SaveData("SoundVolume", value);
                OnSoundVolumeChanged?.Invoke();
            }
        }
        public event System.Action OnSoundVolumeChanged;
    }
}