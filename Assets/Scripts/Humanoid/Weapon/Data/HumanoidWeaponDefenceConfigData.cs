using System;
using Animancer;
using Animancer.TransitionLibraries;
using Animancer.Units;
using Framework.Common.Audio;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid.Weapon.Data
{
    [Serializable]
    public struct HumanoidWeaponDefenceConfigData
    {
        public bool supportDefend;
        [ShowIf("supportDefend")] public HumanoidWeaponDefenceAbilityConfigData defenceAbility;
    }
    
    [Flags]
    public enum HumanoidWeaponDefenceAbility
    {
        Idle = 1 << 0,
        Move = 1 << 1,
    }

    [Serializable]
    public struct HumanoidWeaponDefenceStartParameter
    {
        public StringAsset transition;
        public bool playAudio;
        [ShowIf("playAudio")] public AudioClipRandomizer audioClipRandomizer;
        [ShowIf("playAudio")] [Range(0f, 1f)] public float audioVolume;
    }

    [Serializable]
    public struct HumanoidWeaponDefenceParryParameter
    {
        public StringAsset transition;
        public bool playAudio;
        [ShowIf("playAudio")] public AudioClipRandomizer audioClipRandomizer;
        [ShowIf("playAudio")] [Range(0f, 1f)] public float audioVolume;
        [MinValue(0f)] public float breakAttackMultiplier;
    }

    [Serializable]
    public struct HumanoidWeaponDefenceBreakParameter
    {
        public StringAsset transition;
        public bool playAudio;
        [ShowIf("playAudio")] public AudioClipRandomizer audioClipRandomizer;
        [ShowIf("playAudio")] [Range(0f, 1f)] public float audioVolume;
    }

    [Serializable]
    public struct HumanoidWeaponDefenceIdleParameter
    {
        public StringAsset transition;
    }

    [Serializable]
    public struct HumanoidWeaponDefenceMoveParameter
    {
        public StringAsset transition;
        public float maxSpeed;
        public StringAsset forwardSpeedParameter;
        public StringAsset lateralSpeedParameter;
        [Seconds] public float parameterSmoothTime;
        [EventNames] public StringAsset footPutDownEvent;
    }

    [Serializable]
    public struct HumanoidWeaponDefenceAbilityConfigData
    {
        [Title("动画库")] public TransitionLibraryAsset transitionLibrary;

        [Title("防御开始")] public HumanoidWeaponDefenceStartParameter startParameter;

        [Title("防御格挡")] public HumanoidWeaponDefenceParryParameter parryParameter;

        [Title("防御破坏")] public HumanoidWeaponDefenceBreakParameter breakParameter;

        [FormerlySerializedAs("commonAudioClipRandomizer")] [Title("普通格挡音效")]
        public AudioClipRandomizer universalAudioClipRandomizer;

        [FormerlySerializedAs("commonAudioVolume")] [Range(0f, 1f)]
        public float universalAudioVolume;

        [Title("完美格挡音效")] public AudioClipRandomizer perfectAudioClipRandomizer;
        [Range(0f, 1f)] public float perfectAudioVolume;

        [FoldoutGroup("防御可选能力")] public HumanoidWeaponDefenceAbility abilities;

        [FoldoutGroup("防御可选能力/防御常态")]
        [ShowInInspector]
        public bool AllowIdle => (abilities & HumanoidWeaponDefenceAbility.Idle) != 0;

        [FoldoutGroup("防御可选能力/防御常态")] [ShowIf(@"AllowMove", true, true)]
        public HumanoidWeaponDefenceIdleParameter idleParameter;

        [FoldoutGroup("防御可选能力/防御移动")]
        [ShowInInspector]
        public bool AllowMove => AllowIdle && (abilities & HumanoidWeaponDefenceAbility.Move) != 0;

        [FoldoutGroup("防御可选能力/防御移动")] [ShowIf(@"AllowMove", true, true)]
        public HumanoidWeaponDefenceMoveParameter moveParameter;
    }
}