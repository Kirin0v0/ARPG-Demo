using System;
using System.Collections.Generic;
using Common;
using Framework.Common.Audio;
using Framework.Common.StateMachine;
using Humanoid;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Dead
{
    [Serializable]
    public class PlayerDeadAudioConfigData
    {
        public HumanoidCharacterRace race;
        public AudioClipRandomizer audioClipRandomizer;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public abstract class PlayerDeadState : PlayerState, IPlayerStateDead
    {
        [Title("音效")] [SerializeField] private List<PlayerDeadAudioConfigData> audioConfigs = new();

        public virtual PlayerDeadPose DeadPose { get; }

        protected override void OnInit()
        {
            base.OnInit();
            endureUntilExit = true;
        }

        public override bool AllowEnter(IState currentState)
        {
            // 只有满足死亡条件才允许进入
            return base.AllowEnter(currentState) && PlayerCharacter.Parameters.dead;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState); // 查找种族对应的硬直音效

            if (previousState is not PlayerDeadState)
            {
                // 查找种族对应的死亡音效
                var audioSetting =
                    audioConfigs.Find(setting => setting.race == PlayerCharacter.HumanoidParameters.race);
                if (audioSetting == null)
                {
                    throw new Exception(
                        $"Can't find the audio setting of the race({PlayerCharacter.HumanoidParameters.race})");
                }

                // 播放对应种族的死亡音效
                PlayerCharacter.AudioAbility?.PlaySound(audioSetting.audioClipRandomizer.Random(), false,
                    audioSetting.volume);
            }
        }
    }
}