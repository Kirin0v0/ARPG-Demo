using System;
using System.Collections.Generic;
using Animancer;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Humanoid;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Player.StateMachine.Action
{
    [Serializable]
    public class PlayerActionAudioConfigData
    {
        public HumanoidCharacterRace race;
        public AudioClipRandomizer audioClipRandomizer;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public abstract class PlayerActionState : PlayerState, IPlayerStateWeapon, IPlayerStateLocomotion
    {
        [Title("状态属性")] [SerializeField] private bool onlyWeapon = false;
        [SerializeField] private bool onlyNoWeapon = true;
        public bool OnlyWeapon => onlyWeapon;
        public bool OnlyNoWeapon => onlyNoWeapon;

        [SerializeField] private bool locomotionState = false;
        public bool ForwardLocomotion => locomotionState;
        public bool LateralLocomotion => locomotionState;

        [Title("音效")] [SerializeField] private bool playSoundWhenEnter = false;

        [SerializeField] [ShowIf("playSoundWhenEnter")]
        private List<PlayerActionAudioConfigData> audioConfigs = new();

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            if (playSoundWhenEnter)
            {
                // 查找种族对应的动作音效
                var audioSetting =
                    audioConfigs.Find(setting => setting.race == PlayerCharacter.HumanoidParameters.race);
                if (audioSetting == null)
                {
                    throw new Exception(
                        $"Can't find the audio setting of the race({PlayerCharacter.HumanoidParameters.race})");
                }

                // 播放对应种族的动作音效
                PlayerCharacter.AudioAbility?.PlaySound(audioSetting.audioClipRandomizer.Random(), false,
                    audioSetting.volume);
            }
        }
    }
}