using System.Collections.Generic;
using Framework.Common.Audio;
using UnityEngine;

namespace Common
{
    public class GameAudioManager : AudioManager
    {
        protected override void Awake()
        {
            base.Awake();
            // 添加全局配置监听
            GameApplication.Instance.GlobalSettingsDataManager.OnMusicVolumeChanged += HandleMusicVolumeChanged;
            GameApplication.Instance.GlobalSettingsDataManager.OnSoundVolumeChanged += HandleSoundVolumeChanged;
            // 初始化参数
            HandleMusicVolumeChanged();
            HandleSoundVolumeChanged();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 删除全局配置监听
            if (GameApplication.Instance)
            {
                GameApplication.Instance.GlobalSettingsDataManager.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
                GameApplication.Instance.GlobalSettingsDataManager.OnSoundVolumeChanged -= HandleSoundVolumeChanged;
            }
        }

        private void HandleMusicVolumeChanged()
        {
            var volume = Mathf.Clamp01(GameApplication.Instance.GlobalSettingsDataManager.MusicVolume / 100f);
            SetBackgroundMusicVolume(volume);
        }

        private void HandleSoundVolumeChanged()
        {
            var volume = Mathf.Clamp01(GameApplication.Instance.GlobalSettingsDataManager.SoundVolume / 100f);
            SetSoundsVolume(volume);
        }
    }
}