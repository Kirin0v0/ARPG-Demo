using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Action;
using Camera.Data;
using Common;
using Damage.Data;
using Framework.Common.Audio;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combo
{
    public enum ComboStateConfigurationType
    {
        None,
        Process,
        Event,
    }

    public enum ComboStateProcessTimePoint
    {
        Start,
        Anticipation,
        Judgment,
        Recovery,
        End,
    }

    [CreateAssetMenu(menuName = "Combo/Config")]
    public class ComboConfig : SerializedScriptableObject
    {
        [Title("基础属性")] public ActionClip actionClip;
        public bool useActionNameAsName = true;
        [HideIf("useActionNameAsName")] public new string name;
        public string Name => useActionNameAsName ? actionClip?.name : name;

        [InfoBox("启用loop后动作将跳过结束阶段，在结束时从头开始播放，这意味着只有外部调用Stop函数才能停止连招动作")]
        public bool loop;

        [Title("碰撞配置")] [InfoBox("统一设置动作片段中每个碰撞轨道内置的碰撞同一物体后的检测间隔（单位/秒）")]
        public float colliderDetectionInterval = 1f;

        [InfoBox("统一设置动作片段中每个碰撞轨道与碰撞体发生碰撞的最大数量，一般在指定数量（比如单体攻击）时调整数量")]
        public int colliderDetectionMaximum = 100;

        [ValueDropdown("GetUniqueColliderGroupConfigurableSettings")] [InfoBox("定制碰撞组数据，将不采用统一配置数据")]
        public List<ComboColliderGroupSetting> colliderGroupSettings;

        [ValueDropdown("GetUniqueGlobalSharedColliderGroupConfigurableSettings")]
        [InfoBox("连招全局共享的碰撞组列表，设置后碰撞数据由连招共享数据决定。适用于多个连招共享碰撞检测，连招A检测到敌人1后，连招B过滤该敌人")]
        public List<string> globalSharedColliderGroups;

        [Title("霸体配置")] public ComboStateConfigurationType endureType;

        [ShowIf("endureType", ComboStateConfigurationType.Process, true)]
        public ComboStateProcessTimePoint startEndureTimePoint;

        [ShowIf("endureType", ComboStateConfigurationType.Process, true)]
        public ComboStateProcessTimePoint endEndureTimePoint;

        [ShowIf("endureType", ComboStateConfigurationType.Event, true)]
        [InfoBox("如果动作片段未配置事件，则不执行霸体")]
        [ValueDropdown("GetEventNames")]
        public string startEndureEventName;

        [ShowIf("endureType", ComboStateConfigurationType.Event, true)]
        [InfoBox("如果动作片段未配置事件，则不执行霸体")]
        [ValueDropdown("GetEventNames")]
        public string endEndureEventName;

        [Title("不可破防配置")] public ComboStateConfigurationType unbreakableType;

        [ShowIf("unbreakableType", ComboStateConfigurationType.Process, true)]
        public ComboStateProcessTimePoint startUnbreakableTimePoint;

        [ShowIf("unbreakableType", ComboStateConfigurationType.Process, true)]
        public ComboStateProcessTimePoint endUnbreakableTimePoint;

        [ShowIf("unbreakableType", ComboStateConfigurationType.Event, true)]
        [InfoBox("如果动作片段未配置事件，则不执行不可破防")]
        [ValueDropdown("GetEventNames")]
        public string startUnbreakableEventName;

        [ShowIf("unbreakableType", ComboStateConfigurationType.Event, true)]
        [InfoBox("如果动作片段未配置事件，则不执行不可破防")]
        [ValueDropdown("GetEventNames")]
        public string endUnbreakableEventName;

        [Title("伤害配置")] [InfoBox("固定数值物理伤害")] public int fixedPhysicsDamage;
        [InfoBox("攻击力物理伤害乘区")] public float physicsDamageMultiplier = 1f;
        [InfoBox("最终伤害转为扣除资源乘区")] public DamageResourceMultiplier resourceMultiplier = DamageResourceMultiplier.Default;

#if UNITY_EDITOR
        [Title("伤害测试配置")] [InlineButton("TestDamageAndResource", label: "测试伤害、资源和Atb")]
        public AlgorithmDamageAndResourceAndAtbTestParameters testParameters;

        private void TestDamageAndResource()
        {
            new ConfiguredAlgorithmTest().TestDamageAndResourceAndAtb(
                testParameters.attackerReaction,
                testParameters.attackerLuck,
                testParameters.attackerPhysicsAttack,
                testParameters.attackerMagicAttack,
                testParameters.damageMethod,
                new DamageValue
                {
                    physics = fixedPhysicsDamage,
                },
                new DamageValueMultiplier
                {
                    physics = physicsDamageMultiplier,
                },
                DamageType.DirectDamage,
                resourceMultiplier,
                testParameters.defenderReaction,
                testParameters.defenderDefence,
                1f,
                DamageValueType.None,
                DamageValueType.None
            );
        }
#endif

        [Title("命中音效配置")] public List<ComboHitAudioSetting> hitAudioSettings = new();

        [Title("命中顿帧配置")] public bool openHitFreeze = true;
        [ShowIf("openHitFreeze")] public float hitFreezeDuration = 0.1f;
        [ShowIf("openHitFreeze")] public float hitFreezeTimeScale = 0.2f;

        [Title("命中震动配置")] [SerializeReference] public BaseCameraShakeData hitShake = null;

        private IEnumerable GetUniqueColliderGroupConfigurableSettings()
        {
            var items = new List<ValueDropdownItem>();
            if (!actionClip)
            {
                return items;
            }

            actionClip.collideDetection.collideDetectionClips.GroupBy(clip => clip.groupId).ForEach(group =>
            {
                if (colliderGroupSettings.FindIndex(setting => String.Equals(setting.groupId, group.Key)) != -1)
                {
                    return;
                }

                items.Add(new ValueDropdownItem
                {
                    Text = group.Key,
                    Value = new ComboColliderGroupSetting
                    {
                        groupId = group.Key,
                        detectionInterval = colliderDetectionInterval,
                        detectionMaximum = colliderDetectionMaximum,
                    },
                });
            });

            return items;
        }

        private IEnumerable GetUniqueGlobalSharedColliderGroupConfigurableSettings()
        {
            var items = new List<ValueDropdownItem>();
            if (!actionClip)
            {
                return items;
            }

            actionClip.collideDetection.collideDetectionClips.GroupBy(clip => clip.groupId).ForEach(group =>
            {
                if (globalSharedColliderGroups.FindIndex(groupId => String.Equals(groupId, group.Key)) != -1)
                {
                    return;
                }

                items.Add(new ValueDropdownItem
                {
                    Text = group.Key,
                    Value = group.Key,
                });
            });

            return items;
        }

        private IEnumerable GetEventNames()
        {
            var items = new List<ValueDropdownItem>();
            if (!actionClip)
            {
                return items;
            }

            actionClip.events.eventClips.Select(clip => clip.name).ForEach(name =>
            {
                items.Add(new ValueDropdownItem
                {
                    Text = name,
                    Value = name,
                });
            });

            return items;
        }
    }

    [Serializable]
    public class ComboColliderGroupSetting
    {
        [ReadOnly] public string groupId;
        public float detectionInterval;
        public int detectionMaximum;
    }

    public enum ComboHitAudioType
    {
        Specified,
        Random,
    }

    [Serializable]
    public class ComboHitAudioSetting
    {
        public ComboHitAudioType type = ComboHitAudioType.Specified;

        [ShowIf("type", ComboHitAudioType.Specified)]
        public AudioClip specifiedAudioClip;

        [ShowIf("type", ComboHitAudioType.Random)]
        public AudioClipRandomizer randomAudioClip;

        public float volume = 1f;
        public bool useOwnLengthAsDuration = true;
        [HideIf("useOwnLengthAsDuration")] public float duration = 0f;

        public AudioClip AudioClip => type switch
        {
            ComboHitAudioType.Specified => specifiedAudioClip,
            ComboHitAudioType.Random => randomAudioClip.Random(),
            _ => specifiedAudioClip,
        };
    }
}