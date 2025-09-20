using System;
using System.Collections.Generic;
using Animancer;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Player.StateMachine.Locomotion;
using UnityEngine;

namespace Player.StateMachine
{
    public enum PlayerFootstepsGroundType
    {
        Concrete,
        Gravel,
        Grass,
    }

    [Serializable]
    public class PlayerFootstepsAudioConfigData
    {
        public PlayerFootstepsGroundType groundType;
        public AudioClipRandomizer audioClipRandomizer;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public class PlayerFootstepsPlayer
    {
        private readonly PlayerCharacterObject _playerCharacter;
        private readonly AudioClipRandomizer _defaultAudioClipRandomizer;
        private readonly float _defaultAudioVolume;
        private readonly List<PlayerFootstepsAudioConfigData> _audioConfigs;
        private readonly float _minPlayAudioInterval;

        private float _lastPlayAudioTime;

        public PlayerFootstepsPlayer(
            PlayerCharacterObject playerCharacter,
            AudioClipRandomizer defaultAudioClipRandomizer,
            float defaultAudioVolume,
            List<PlayerFootstepsAudioConfigData> audioConfigs,
            float minPlayAudioInterval
        )
        {
            _playerCharacter = playerCharacter;
            _defaultAudioClipRandomizer = defaultAudioClipRandomizer;
            _defaultAudioVolume = defaultAudioVolume;
            _audioConfigs = audioConfigs;
            _minPlayAudioInterval = minPlayAudioInterval;
        }

        public void PlayAudio(float currentTime)
        {
            // 去除角色不接触地面或斜坡的场景
            if (_playerCharacter.GravityAbility && !_playerCharacter.GravityAbility.StandOnGround &&
                !_playerCharacter.GravityAbility.StandOnSlope)
            {
                return;
            }

            // 去除脚步音效播放间隔内多次播放的情况
            if (currentTime - _lastPlayAudioTime < _minPlayAudioInterval)
            {
                return;
            }

            Collider collider = null;
            if (_playerCharacter.GravityAbility)
            {
                collider = _playerCharacter.GravityAbility.GroundCollider;
            }

            // 判断脚的接触面的类型获取音效配置
            PlayerFootstepsAudioConfigData audioConfigData;
            if (!collider)
            {
                // 如果无法获取到就采用默认的脚步音效
                audioConfigData = new PlayerFootstepsAudioConfigData
                {
                    audioClipRandomizer = _defaultAudioClipRandomizer,
                    volume = _defaultAudioVolume
                };
            }
            else
            {
                // 否则就采用脚的接触面的类型的脚步音效，没有配置也采用默认音效
                audioConfigData = _audioConfigs.Find(setting =>
                {
                    switch (setting.groundType)
                    {
                        case PlayerFootstepsGroundType.Gravel:
                        {
                            if (collider.CompareTag("Mud"))
                            {
                                return true;
                            }
                        }
                            break;
                        case PlayerFootstepsGroundType.Grass:
                        {
                            if (collider.CompareTag("Grass"))
                            {
                                return true;
                            }
                        }
                            break;
                    }

                    return false;
                }) ?? new PlayerFootstepsAudioConfigData
                {
                    audioClipRandomizer = _defaultAudioClipRandomizer,
                    volume = _defaultAudioVolume
                };
            }

            // 判断音效配置是否为空
            if (audioConfigData == null)
            {
                throw new Exception(
                    $"Can't find the audio configuration of the footsteps");
            }

            // 播放对应接触面的脚步音效
            _playerCharacter.AudioAbility?.PlaySound(audioConfigData.audioClipRandomizer.Random(), false,
                audioConfigData.volume);

            _lastPlayAudioTime = currentTime;
        }
    }
}