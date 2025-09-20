using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using Combo.Graph;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid.Weapon.Data
{
    [Serializable]
    public struct HumanoidWeaponAttackConfigData
    {
        public bool supportAttack;
        [ShowIf("supportAttack")] public HumanoidWeaponAttackAbilityConfigData attackAbility;
    }
    
    [Serializable]
    public struct HumanoidWeaponAttackAbilityConfigData
    {
        [FormerlySerializedAs("attackComboGraph")] [Title("连招图")] public ComboGraph comboGraph;
    }
    
    // [Flags]
    // public enum HumanoidWeaponAttackAbility
    // {
    //     None = 0,
    //     Common = 1,
    //     Heavy = 1 << 1,
    // }
    //
    // [Flags]
    // public enum HumanoidWeaponAttackRule
    // {
    //     Land = 1,
    //     Airborne = 1 << 1,
    // }
    //
    // [Flags]
    // public enum HumanoidWeaponAttackLandType
    // {
    //     NoCombo = 1,
    //     Combo = 1 << 1,
    //     Evade = 1 << 2,
    //     Counterattack = 1 << 3,
    // }
    //
    // [Flags]
    // public enum HumanoidWeaponAttackAirborneType
    // {
    //     NoCombo = 1,
    //     Combo = 1 << 1,
    // }
    //
    // [Flags]
    // public enum HumanoidWeaponAttackAnticipationInterruptionStrategy
    // {
    //     Jump = 1,
    //     Evade = 1 << 1,
    //     Defend = 1 << 2,
    // }
    //
    // [Serializable]
    // public class WeaponAttackSeriesConfigData
    // {
    //     [Title("攻击规则及类型")] public HumanoidWeaponAttackRule attackRule;
    //     public HumanoidWeaponAttackLandType attackLandType;
    //     public HumanoidWeaponAttackAirborneType attackAirborneType;
    //
    //     [Title("攻击连招列表")] public List<WeaponAttackComboConfigData> attackComboList;
    // }
    //
    // [Serializable]
    // public struct WeaponAttackAnticipationConfigData
    // {
    //     public bool interruptible;
    //
    //     [InfoBox("具体策略的执行依赖对应输入，如果不可打断则无视策略执行")]
    //     public HumanoidWeaponAttackAnticipationInterruptionStrategy interruptionStrategy;
    //
    //     public bool allowTurn;
    // }
    //
    // [Serializable]
    // public struct WeaponAttackJudgmentConfigData
    // {
    // }
    //
    // [Flags]
    // public enum WeaponAttackRecoveryInterruptionStrategy
    // {
    //     None = 0,
    //     Jump = 1 << 2,
    //     Evade = 1 << 3,
    //     Defend = 1 << 4,
    //     ComboAttack = 1 << 5,
    //     OtherAttack = 1 << 6,
    // }
    //
    // [Serializable]
    // public struct WeaponAttackRecoveryConfigData
    // {
    //     public bool interruptible;
    //
    //     [InfoBox("具体策略的执行依赖对应输入，如果不可打断将记录最新策略并在动画结束自动执行")]
    //     public WeaponAttackRecoveryInterruptionStrategy interruptionStrategy;
    //
    //     public bool allowTurn;
    // }
    //
    // [Serializable]
    // public struct WeaponAttackComboConfigData
    // {
    //     [Title("攻击动画")] public StringAsset transition;
    //
    //     [Title("攻击流程事件")] [InfoBox("如果没有配置则代表流程忽略该事件对应的流程")]
    //     public StringAsset anticipation;
    //
    //     public StringAsset judgment;
    //     public StringAsset recovery;
    //
    //     [Title("攻击流程参数")] [InfoBox("注意处于空中时部分前摇和后摇策略会被屏蔽，具体请看对应状态实现")]
    //     public WeaponAttackAnticipationConfigData anticipationConfigData;
    //
    //     public WeaponAttackJudgmentConfigData judgmentConfigData;
    //     public WeaponAttackRecoveryConfigData recoveryConfigData;
    // }
    //
    // [Serializable]
    // public struct WeaponAttackConfigData
    // {
    //     [InfoBox("务必保证动画过渡库存在对应的动画、事件和参数")] public TransitionLibraryAsset transitionLibrary;
    //     public HumanoidWeaponAttackAbility abilities;
    //
    //     [Title("普攻能力")] public List<WeaponAttackSeriesConfigData> commonAttackSeries;
    //
    //     [Title("重击能力")] public List<WeaponAttackSeriesConfigData> heavyAttackSeries;
    // }
}