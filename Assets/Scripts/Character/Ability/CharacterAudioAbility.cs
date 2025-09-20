using System.Collections.Generic;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Character.Ability
{
    public class CharacterAudioAbility : BaseCharacterOptionalAbility
    {
        [SerializeField, MinValue(0f)] private float soundMinDistance = 1f;
        [SerializeField, MinValue(0f)] private float soundMaxDistance = 10f;
        [SerializeField] private bool debug;

        private GameAudioManager _audioManager;

        protected override void OnInit()
        {
            base.OnInit();
            if (!TryGetComponent<GameAudioManager>(out _audioManager))
            {
                _audioManager = gameObject.AddComponent<GameAudioManager>();
            }
        }

        public void Tick(float deltaTime)
        {
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _audioManager.ClearSounds();
            GameObject.Destroy(_audioManager);
        }

        public int PlaySound(
            AudioClip audioClip,
            bool loop = false,
            float volume = 1f,
            float duration = float.MaxValue
        )
        {
            var id = _audioManager.PlaySound(
                audioClip,
                loop,
                volume,
                duration,
                spatialBlend: 1f,
                minDistance: soundMinDistance,
                maxDistance: soundMaxDistance
            );
            if (debug)
            {
                DebugUtil.LogGrey($"角色({Owner.Parameters.DebugName})播放音频: {audioClip.name}—{id}");
            }

            return id;
        }

        public void StopSound(int id)
        {
            if (debug)
            {
                DebugUtil.LogGrey($"角色({Owner.Parameters.DebugName})停止音频: {id}");
            }

            _audioManager.StopSound(id);
        }

        public void StopSounds(AudioClip audioClip)
        {
            if (debug)
            {
                DebugUtil.LogGrey($"角色({Owner.Parameters.DebugName})停止音频: {audioClip.name}");
            }

            _audioManager.StopSounds(audioClip);
        }

        public void SetSoundSpeed(float speed)
        {
            _audioManager.SetSoundsPitch(speed);
        }
    }
}