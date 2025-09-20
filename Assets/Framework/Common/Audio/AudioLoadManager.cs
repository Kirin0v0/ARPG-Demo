using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.Common.Function;
using Framework.Common.Resource;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Framework.Common.Audio
{
    /// <summary>
    /// 音频管理器，提供播放/暂停BGM以及播放/暂停音效的功能
    /// 需要提前设置加载接口和音效物体预设体
    /// 音效会附着在预设体上，而背景音乐则是附着在AudioManager的游戏对象上
    /// </summary>
    public class AudioLoadManager : MonoBehaviour
    {
        public delegate void LoadAsset(string assetName, bool isAsync, UnityAction<AudioClip> callback);

        private class AudioData
        {
            public AudioSource AudioSource;
            public float AudioVolume;
        }

        // 音乐操作常量
        private const int PlayMusic = 0;
        private const int StopMusic = 1;
        private const int PauseMusic = 2;
        private const int ResumeMusic = 3;

        // 音效预设体
        public GameObject soundPrefab;

        // 资源加载接口
        public LoadAsset LoadAssetDelegate;

        // 背景音乐播放组件
        private AudioSource _backgroundMusic;

        // 最近一次背景音乐名称
        private string _backgroundMusicName;

        // 最近一次背景音乐操作
        private int _backgroundMusicOperation;

        // 背景音乐音量
        private float _backgroundMusicVolume = 1f;

        // 背景音乐开关
        private bool _backgroundMusicEnable = true;

        // 缓存音效资源，背景音乐资源不参与缓存
        private readonly Dictionary<string, AudioClip> _soundClips = new();

        // 管理正在播放的音效
        private readonly List<AudioData> _playingSounds = new();

        // 整体音效音量大小
        private float _soundsVolume = 1f;

        // 整体音效开关
        private bool _soundsEnable = true;

        // 整体音效是否在播放
        private bool _playSound = true;

        // 音效对象池，用于缓存音效游戏对象
        private ObjectPool<GameObject> _soundObjectPool;

        private void Awake()
        {
            if (soundPrefab == null)
            {
                throw new NullReferenceException("Sound prefab must be not null");
            }

            if (LoadAssetDelegate == null)
            {
                throw new NullReferenceException("LoadAsset must be not null");
            }

            _soundObjectPool = new ObjectPool<GameObject>(
                (() =>
                {
                    if (soundPrefab.GetComponent<AudioSource>() == null)
                    {
                        var audioSource = soundPrefab.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                    }
                    else
                    {
                        soundPrefab.GetComponent<AudioSource>().playOnAwake = false;
                    }

                    var soundObject = Instantiate(soundPrefab, transform, true);

                    return soundObject;
                }),
                (gameObject => { GameObject.Destroy(gameObject); }),
                10,
                20
            );
        }

        private void Update()
        {
            if (!_playSound)
                return;

            // 遍历播放音效，音效未播放则移除
            for (int i = _playingSounds.Count - 1; i >= 0; --i)
            {
                if (!_playingSounds[i].AudioSource.isPlaying)
                {
                    StopSound(_playingSounds[i].AudioSource);
                }
            }
        }

        public void PlayBackgroundMusic(string name, bool loop = true)
        {
            // 未启用功能则过滤
            if (!_backgroundMusicEnable)
            {
                return;
            }

            // 动态创建播放背景音乐的组件
            if (_backgroundMusic == null)
            {
                _backgroundMusic = gameObject.AddComponent<AudioSource>();
            }

            // 设置最近一次音乐名和操作
            _backgroundMusicName = name;
            _backgroundMusicOperation = PlayMusic;

            // 通过资源加载委托获取背景音乐
            LoadAssetDelegate.Invoke(name, true, (clip) =>
            {
                // 如果音乐不是加载的文件则不用进行设置了
                if (!String.Equals(_backgroundMusicName, clip.name))
                {
                    return;
                }

                if (clip == null)
                {
                    throw new FileNotFoundException($"AudioLoadManager can't find the music file whose name is {name}");
                }

                _backgroundMusic.clip = clip;
                _backgroundMusic.loop = loop;
                _backgroundMusic.volume = _backgroundMusicVolume;

                // 如果加载完毕后音乐仍未变，判断最近一次对音乐的操作
                switch (_backgroundMusicOperation)
                {
                    case PlayMusic:
                    case ResumeMusic:
                        _backgroundMusic.Play();
                        break;
                    case StopMusic:
                        _backgroundMusic.Stop();
                        break;
                    case PauseMusic:
                        _backgroundMusic.Pause();
                        break;
                }
            });
        }

        public void StopBackgroundMusic()
        {
            if (_backgroundMusic == null) return;
            _backgroundMusicOperation = StopMusic;
            _backgroundMusic.Stop();
        }

        public void PauseBackgroundMusic()
        {
            if (_backgroundMusic == null) return;
            _backgroundMusicOperation = PauseMusic;
            _backgroundMusic.Pause();
        }

        public void ResumeBackgroundMusic()
        {
            if (_backgroundMusic == null) return;
            _backgroundMusicOperation = ResumeMusic;
            _backgroundMusic.UnPause();
        }

        public void SetBackgroundMusicVolume(float volume)
        {
            _backgroundMusicVolume = volume;
            if (_backgroundMusic == null) return;
            _backgroundMusic.volume = _backgroundMusicVolume;
        }

        public void SetBackgroundMusicEnable(bool enable)
        {
            _backgroundMusicEnable = enable;
            if (!_backgroundMusicEnable)
            {
                StopBackgroundMusic();
            }
        }

        public bool IsPlayingBackgroundMusic(string name) => String.Equals(_backgroundMusicName, name);

        public void PlaySound(
            string name,
            bool loop = false,
            float volume = 1f,
            bool async = false,
            UnityAction<AudioSource> callBack = null
        )
        {
            // 未启用功能则过滤
            if (!_soundsEnable)
            {
                return;
            }

            if (_soundClips.ContainsKey(name)) // 如果存在音效资源则直接复用
            {
                var soundClip = _soundClips[name];
                PlaySoundInternal(soundClip, loop, volume, callBack);
            }
            else // 否则先加载再使用
            {
                LoadAssetDelegate.Invoke(name, async, audioClip => PlaySoundInternal(audioClip, loop, volume, callBack));
            }
        }

        private void PlaySoundInternal(
            AudioClip audioClip,
            bool loop,
            float volume,
            UnityAction<AudioSource> callBack
        )
        {
            if (audioClip == null)
            {
                throw new FileNotFoundException($"AudioLoadManager can't find the sound file whose name is {name}");
            }

            // 记录播放音效资源
            if (!_soundClips.ContainsKey(audioClip.name))
            {
                _soundClips.Add(audioClip.name, audioClip);
            }

            // 从缓存池中取出音效对象得到对应组件
            AudioSource audioSource = _soundObjectPool.Get((gameObject =>
            {
                var audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.loop = loop;
                audioSource.volume = _soundsVolume * volume;
                audioSource.Play();
            })).GetComponent<AudioSource>();

            // 记录播放音效对象
            var index = _playingSounds.FindIndex(audioData => audioData.AudioSource == audioSource);
            if (index == -1)
            {
                _playingSounds.Add(new AudioData
                {
                    AudioSource = audioSource,
                    AudioVolume = volume,
                });
            }
            else
            {
                _playingSounds[index].AudioVolume = volume;
            }

            callBack?.Invoke(audioSource);
        }

        public void StopSound(AudioSource source)
        {
            // 如果播放音效存在则移除并放入缓存池
            var index = _playingSounds.FindIndex(audioData => audioData.AudioSource == source);
            if (index != -1)
            {
                _playingSounds.RemoveAt(index);
                _soundObjectPool.Release(source.gameObject, gameObject =>
                {
                    var audioSource = gameObject.GetComponent<AudioSource>();
                    audioSource.Stop();
                    audioSource.clip = null;
                });
            }
        }

        public void SetSoundsVolume(float volume)
        {
            _soundsVolume = Mathf.Clamp01(volume);
            for (int i = 0; i < _playingSounds.Count; i++)
            {
                _playingSounds[i].AudioSource.volume = _soundsVolume * _playingSounds[i].AudioVolume;
            }
        }

        public void PlaySounds()
        {
            _playSound = true;
            for (int i = 0; i < _playingSounds.Count; i++)
            {
                _playingSounds[i].AudioSource.Play();
            }
        }

        public void PauseSounds()
        {
            _playSound = false;
            for (int i = 0; i < _playingSounds.Count; i++)
            {
                _playingSounds[i].AudioSource.Pause();
            }
        }

        public void ClearSounds()
        {
            for (int i = 0; i < _playingSounds.Count; i++)
            {
                _soundObjectPool.Release(_playingSounds[i].AudioSource.gameObject, gameObject =>
                {
                    var audioSource = gameObject.GetComponent<AudioSource>();
                    audioSource.Stop();
                    audioSource.clip = null;
                });
            }

            _soundClips.Clear();
            _playingSounds.Clear();
        }

        public void SetSoundsEnable(bool enable)
        {
            _soundsEnable = enable;
            if (!_soundsEnable)
            {
                ClearSounds();
            }
        }

        public bool IsPlayingSound(string name) =>
            _playingSounds.Any(audioSource => String.Equals(audioSource.AudioSource.clip?.name ?? "", name));
    }
}