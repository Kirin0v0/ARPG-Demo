using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.Common.Function;
using Framework.DataStructure;
using UnityEngine;

namespace Framework.Common.Audio
{
    /// <summary>
    /// 音频管理器，提供播放/暂停BGM以及播放/暂停音效的功能
    /// 音效会附着在音效物体上，而背景音乐则是附着在AudioManager的游戏对象上
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private class MusicData
        {
            public int Id;
            public AudioClip Music;
            public int Priority;
            public float Volume;
        }

        private class AudioData
        {
            public int Id;
            public AudioSource Source;
            public float Volume;
            public float Duration;
        }

        private int _idSeed = 0;

        // 背景音乐播放组件
        private AudioSource _backgroundMusic;

        // 背景音乐待播放队列
        private readonly PriorityQueue<MusicData> _toPlayBackgroundMusicQueue = new(new MusicPriorityComparer(), 10);

        // 背景音乐音量
        private float _backgroundMusicVolume = 1f;

        // 音效对象池，用于缓存音效游戏对象
        private ObjectPool<GameObject> _soundObjectPool;

        // 管理正在播放的音效
        private readonly List<AudioData> _playingSounds = new();

        // 整体音效音量大小
        private float _soundsVolume = 1f;

        // 整体音效音量音调
        private float _soundsPitch = 1f;

        protected virtual void Awake()
        {
            if (!gameObject.TryGetComponent<AudioSource>(out _backgroundMusic))
            {
                _backgroundMusic = gameObject.AddComponent<AudioSource>();
            }

            _backgroundMusic.playOnAwake = false;

            _soundObjectPool = new ObjectPool<GameObject>(
                (() =>
                {
                    var soundObject = new GameObject("Sound")
                    {
                        transform = { parent = transform }
                    };
                    if (soundObject.GetComponent<AudioSource>() == null)
                    {
                        var audioSource = soundObject.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                    }
                    else
                    {
                        soundObject.GetComponent<AudioSource>().playOnAwake = false;
                    }

                    soundObject.SetActive(false);

                    return soundObject;
                }),
                (gameObject => { GameObject.Destroy(gameObject); }),
                10,
                20
            );
        }

        protected virtual void Update()
        {
            // 遍历播放音效，音效已经停止或超出预设时间则移除
            for (var i = _playingSounds.Count - 1; i >= 0; --i)
            {
                var playingSound = _playingSounds[i];
                if (!playingSound.Source.isPlaying || playingSound.Source.time >= playingSound.Duration)
                {
                    _playingSounds.RemoveAt(i);
                    ReleaseSound(playingSound.Source);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            ClearBackgroundMusic();
            ClearSounds();
            _soundObjectPool.Clear();
        }

        public int AddBackgroundMusic(AudioClip audioClip, int priority, float volume = 1f)
        {
            if (!audioClip)
            {
                return -1;
            }

            var musicData = new MusicData
            {
                Id = _idSeed++,
                Music = audioClip,
                Priority = priority,
                Volume = volume,
            };
            _toPlayBackgroundMusicQueue.Enqueue(musicData);
            // 更新背景音乐
            UpdateBackgroundMusicInternal();

            return musicData.Id;
        }

        public void RemoveBackgroundMusic(int id)
        {
            // 将待播放音乐从队列挪至临时队列中，如果音乐id正好是指定id则停止
            var tempMusicQueue = new Queue<MusicData>();
            while (_toPlayBackgroundMusicQueue.TryDequeue(out var oldData))
            {
                if (oldData.Id == id)
                {
                    break;
                }

                tempMusicQueue.Enqueue(oldData);
            }

            // 重新放入待播放音乐队列
            if (tempMusicQueue.TryDequeue(out var newData))
            {
                _toPlayBackgroundMusicQueue.Enqueue(newData);
            }

            // 更新背景音乐
            UpdateBackgroundMusicInternal();
        }

        public void RemoveBackgroundMusic(AudioClip audioClip)
        {
            if (!audioClip)
            {
                return;
            }

            // 将待播放音乐从队列挪至临时队列中，如果音乐片段正好是指定音乐片段则丢弃
            var tempMusicQueue = new Queue<MusicData>();
            while (_toPlayBackgroundMusicQueue.TryDequeue(out var oldData))
            {
                if (oldData.Music != audioClip)
                {
                    tempMusicQueue.Enqueue(oldData);
                }
            }

            // 重新放入待播放音乐队列
            if (tempMusicQueue.TryDequeue(out var newData))
            {
                _toPlayBackgroundMusicQueue.Enqueue(newData);
            }

            // 更新背景音乐
            UpdateBackgroundMusicInternal();
        }

        public void ClearAllBackgroundMusic()
        {
            _toPlayBackgroundMusicQueue.Clear();
            UpdateBackgroundMusicInternal();
        }

        public void ResumeBackgroundMusic()
        {
            _backgroundMusic.UnPause();
        }

        public void PauseBackgroundMusic()
        {
            _backgroundMusic.Pause();
        }

        public void SetBackgroundMusicVolume(float volume)
        {
            _backgroundMusicVolume = volume;
            if (_toPlayBackgroundMusicQueue.TryPeek(out var musicData))
            {
                _backgroundMusic.volume = _backgroundMusicVolume * musicData.Volume;
            }
        }

        public bool IsPlayingBackgroundMusic(AudioClip audioClip) => _backgroundMusic.clip == audioClip;

        public void ClearBackgroundMusic()
        {
            _toPlayBackgroundMusicQueue.Clear();
            _backgroundMusic.Stop();
            _backgroundMusic.clip = null;
        }

        private void UpdateBackgroundMusicInternal()
        {
            // 获取当前队列第一个音乐，设置为当前音乐，如果正好是音乐组件的音乐则不需进行设置
            if (_toPlayBackgroundMusicQueue.TryPeek(out var data))
            {
                if (_backgroundMusic.clip != data.Music)
                {
                    _backgroundMusic.clip = data.Music;
                    _backgroundMusic.loop = true;
                    _backgroundMusic.volume = _backgroundMusicVolume * data.Volume;
                    _backgroundMusic.Play();
                }
            }
            else
            {
                _backgroundMusic.Stop();
                _backgroundMusic.clip = null;
            }
        }

        public int PlaySound(
            AudioClip audioClip,
            bool loop = false,
            float volume = 1f,
            float duration = float.MaxValue,
            float spatialBlend = 0f,
            float minDistance = 1f,
            float maxDistance = 500f
        )
        {
            if (!audioClip)
            {
                return -1;
            }

            // 从缓存池中取出音效对象得到对应组件
            AudioSource audioSource = _soundObjectPool.Get((gameObject =>
            {
                gameObject.SetActive(true);
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.name = $"Sound({audioClip.name})";
                var audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.loop = loop;
                audioSource.pitch = _soundsPitch;
                audioSource.volume = _soundsVolume * volume;
                audioSource.spatialBlend = spatialBlend;
                audioSource.minDistance = minDistance;
                audioSource.maxDistance = maxDistance;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.Play();
            })).GetComponent<AudioSource>();

            // 记录播放音效对象
            var audioData = new AudioData
            {
                Id = _idSeed++,
                Source = audioSource,
                Volume = volume,
                Duration = duration
            };
            _playingSounds.Add(audioData);

            return audioData.Id;
        }

        public void StopSound(int id)
        {
            // 如果播放音效存在则移除并放入缓存池
            var index = _playingSounds.FindIndex(audioData => audioData.Id == id);
            if (index != -1)
            {
                var audioData = _playingSounds[index];
                _playingSounds.RemoveAt(index);
                ReleaseSound(audioData.Source);
            }
        }

        public void StopSounds(AudioClip audioClip)
        {
            if (!audioClip)
            {
                return;
            }

            var index = 0;
            while (index < _playingSounds.Count)
            {
                var audioData = _playingSounds[index];
                if (audioData.Source.clip == audioClip)
                {
                    _playingSounds.RemoveAt(index);
                    ReleaseSound(audioData.Source);
                }
                else
                {
                    index++;
                }
            }
        }

        public void SetSoundsVolume(float volume)
        {
            _soundsVolume = Mathf.Clamp01(volume);
            _playingSounds.ForEach(audioData => audioData.Source.volume = _soundsVolume * audioData.Volume);
        }

        public void SetSoundsPitch(float pitch)
        {
            _soundsPitch = pitch;
            _playingSounds.ForEach(audioData => audioData.Source.pitch = _soundsPitch);
        }

        public void PlaySounds()
        {
            _playingSounds.ForEach(audioData => audioData.Source.Play());
        }

        public void PauseSounds()
        {
            _playingSounds.ForEach(audioData => audioData.Source.Pause());
        }

        public void ClearSounds()
        {
            _playingSounds.ForEach(audioData =>
            {
                _soundObjectPool.Release(audioData.Source.gameObject, gameObject =>
                {
                    gameObject.SetActive(false);
                    gameObject.name = "Sound";
                    gameObject.transform.parent = transform;
                    var audioSource = gameObject.GetComponent<AudioSource>();
                    audioSource.Stop();
                    audioSource.clip = null;
                });
            });
            _playingSounds.Clear();
        }

        public bool IsPlayingSound(AudioClip audioClip) =>
            _playingSounds.Any(audioSource => audioSource.Source.clip == audioClip);

        private void ReleaseSound(AudioSource audioSource)
        {
            _soundObjectPool.Release(audioSource.gameObject, gameObject =>
            {
                gameObject.SetActive(false);
                gameObject.name = "Sound";
                gameObject.transform.parent = transform;
                var audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.Stop();
                audioSource.clip = null;
            });
        }

        /// <summary>
        /// 音乐优先级比较器，优先级高的放在前面，优先级相同则新添加的放在前面
        /// </summary>
        private class MusicPriorityComparer : IComparer<MusicData>
        {
            public int Compare(MusicData x, MusicData y)
            {
                if (y == null)
                {
                    return -1;
                }

                if (x == null)
                {
                    return 1;
                }

                if (x.Priority == y.Priority)
                {
                    return x.Id >= y.Id ? -1 : 1;
                }

                return x.Priority >= y.Priority ? -1 : 1;
            }
        }
    }
}