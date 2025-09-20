using System;
using System.Collections.Generic;
using Character.Data;
using Common;
using Framework.Common.Audio;
using Framework.Common.StateMachine;
using Humanoid;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Player.StateMachine.Hit
{
    [Serializable]
    public class PlayerHitAudioConfigData
    {
        public HumanoidCharacterRace race;
        public AudioClipRandomizer audioClipRandomizer;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public abstract class PlayerHitState : PlayerState
    {
        [Title("伤害增幅设置")] [SerializeField, MinValue(1f)]
        private float hitDamageMultiplier = 1.2f;

        [FormerlySerializedAs("audioSettings")] [Title("硬直音效")] [SerializeField]
        private List<PlayerHitAudioConfigData> audioConfigs = new();

        protected override void OnInit()
        {
            base.OnInit();
            // 设置受击状态下存在霸体
            endureUntilExit = true;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            // 在硬直时增加角色受到的系数，作为游戏惩罚
            PlayerCharacter.Parameters.damageMultiplier = hitDamageMultiplier;

            // 查找种族对应的硬直音效
            var audioSetting = audioConfigs.Find(setting => setting.race == PlayerCharacter.HumanoidParameters.race);
            if (audioSetting == null)
            {
                throw new Exception(
                    $"Can't find the audio setting of the race({PlayerCharacter.HumanoidParameters.race})");
            }

            // 播放对应种族的硬直音效
            PlayerCharacter.AudioAbility?.PlaySound(
                audioSetting.audioClipRandomizer.Random(),
                false,
                audioSetting.volume
            );
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            // 恢复受到的伤害系数
            PlayerCharacter.Parameters.damageMultiplier = 1f;
        }

        private void OnValidate()
        {
            endureUntilExit = true;
        }
    }
}