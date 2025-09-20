using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using Framework.Common.Audio;
using Framework.Core.Attribute;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace Action
{
    [CreateAssetMenu(menuName = "Action Clip")]
    public class ActionClip : SerializedScriptableObject
    {
        [ReadOnly, LabelText("名称")] public new string name; // 片段名称
        [ReadOnly, LabelText("帧率")] public int frameRate = 60; // 片段帧率
        [ReadOnly, LabelText("时长")] public float duration = 1f; // 片段时长，影响片段总帧数
        [ReadOnly, LabelText("总帧数")] public int totalTicks = 60; // 片段总帧数
        [ReadOnly, LabelText("流程")] public ActionProcess process; // 片段流程
        [ReadOnly, LabelText("动画")] public ActionAnimation animation; // 片段动画
        [ReadOnly, LabelText("音频")] public ActionAudio audio; // 片段音频
        [ReadOnly, LabelText("特效")] public ActionEffect effect; // 片段特效
        [ReadOnly, LabelText("碰撞检测")] public ActionCollideDetection collideDetection; // 片段碰撞检测
        [ReadOnly, LabelText("事件")] public ActionEvent events; // 片段事件
#if UNITY_EDITOR
        [ReadOnly, LabelText("通道列表")] public List<ActionChannelData> channels = new(); // 片段通道列表，仅在编辑器下使用，用于可视化动作片段的通道
#endif
    }

    [Serializable]
    public class ActionProcess
    {
        [ReadOnly, LabelText("前摇时间点")] public float anticipationTime; // 前摇时间点
        [ReadOnly, LabelText("判定时间点")] public float judgmentTime; // 判定时间点
        [ReadOnly, LabelText("后摇时间点")] public float recoveryTime; // 后摇时间点
        [ReadOnly, LabelText("前摇帧")] public int anticipationTick; // 前摇帧
        [ReadOnly, LabelText("判定帧")] public int judgmentTick; // 判定帧
        [ReadOnly, LabelText("后摇帧")] public int recoveryTick; // 后摇帧
    }

    [Serializable]
    public class ActionAnimation
    {
        [ReadOnly, LabelText("动画库")] public TransitionLibraryAsset transitionLibrary; // 动画库
        [ReadOnly, LabelText("动画片段列表")] public List<ActionAnimationClipData> animationClips = new(); // 动画片段列表
    }

    [Serializable]
    public class ActionAnimationClipData
    {
        [ReadOnly, LabelText("片段")] public TransitionAsset transition; // 动画片段
        [ReadOnly, LabelText("片段开始时间点")] public float startTime; // 动画片段开始时间点，影响开始帧
        [ReadOnly, LabelText("片段时长")] public float duration; // 动画片段时长，影响持续帧数
        [ReadOnly, LabelText("片段开始帧")] public int startTick; // 动画片段开始帧
        [ReadOnly, LabelText("片段持续帧数")] public int durationTicks; // 动画片段持续帧数
        [ReadOnly, LabelText("片段速度")] public float speed = 1f; // 动画片段速度
#if UNITY_EDITOR
        public string channelId;
#endif
    }

    [Serializable]
    public class ActionAudio
    {
        [FormerlySerializedAs("newAudioClips")] [ReadOnly, LabelText("音频片段列表")]
        public List<ActionAudioClipData> audioClips = new(); // 音频片段列表
    }

    public enum ActionAudioType
    {
        Specified,
        Random,
    }

    [Serializable]
    public class ActionAudioClipData
    {
        [ReadOnly, LabelText("片段类型")] public ActionAudioType type; // 音频片段类型

        [ReadOnly, LabelText("片段音频")] [ShowIf("type", ActionAudioType.Specified)]
        public AudioClip specifiedAudioClip; // 音频片段特定音频

        [ReadOnly, LabelText("片段音频")] [ShowIf("type", ActionAudioType.Random)]
        public AudioClipRandomizer randomAudioClip; // 音频片段随机音频

        [ReadOnly, LabelText("片段开始时间点")] public float startTime; // 音频片段开始时间点，影响开始帧
        [ReadOnly, LabelText("片段时长")] public float duration; // 音频片段时长，影响持续帧数
        [ReadOnly, LabelText("片段开始帧")] public int startTick; // 音频片段开始帧
        [ReadOnly, LabelText("片段持续帧数")] public int durationTicks; // 音频片段持续帧数
        [ReadOnly, LabelText("片段音量")] public float volume = 1f; // 音频片段音量

        public string Name => type switch
        {
            ActionAudioType.Specified => specifiedAudioClip != null ? specifiedAudioClip.name : "",
            ActionAudioType.Random => randomAudioClip != null ? randomAudioClip.name : "",
            _ => "",
        };

        public AudioClip AudioClip => type switch
        {
            ActionAudioType.Specified => specifiedAudioClip,
            ActionAudioType.Random => randomAudioClip?.Random(),
            _ => null,
        };

#if UNITY_EDITOR
        public string channelId;
#endif
    }

    [Serializable]
    public class ActionEffect
    {
        [ReadOnly, LabelText("特效片段列表")] public List<ActionEffectClipData> effectClips = new(); // 特效片段列表
    }

    public enum ActionEffectType
    {
        Dynamic,
        Fixed,
    }

    [Serializable]
    public class ActionEffectClipData
    {
        [ReadOnly, LabelText("片段预设体")] public GameObject prefab; // 特效片段预设体
        [ReadOnly, LabelText("片段开始时间点")] public float startTime; // 特效片段开始时间点，影响开始帧
        [ReadOnly, LabelText("片段时长")] public float duration; // 特效片段时长，影响持续帧数
        [ReadOnly, LabelText("片段开始帧")] public int startTick; // 特效片段开始帧
        [ReadOnly, LabelText("片段持续帧数")] public int durationTicks; // 特效片段持续帧数
        [ReadOnly, LabelText("片段类型")] public ActionEffectType type = ActionEffectType.Dynamic; // 特效片段类型
        [ReadOnly, LabelText("片段起始时间")] public float startLifetime = 0f; // 特效片段起始时间
        [ReadOnly, LabelText("片段模拟速度")] public float simulationSpeed = 1f; // 特效片段模拟速度
        [ReadOnly, LabelText("片段本地位置")] public Vector3 localPosition = Vector3.zero; // 特效片段本地位置

        [FormerlySerializedAs("newLocalRotation")] [ReadOnly, LabelText("片段本地旋转量")]
        public Quaternion localRotation = Quaternion.identity; // 特效片段本地旋转量

        [ReadOnly, LabelText("片段本地缩放")] public Vector3 localScale = Vector3.one; // 特效片段本地缩放
#if UNITY_EDITOR
        public string channelId;
#endif
    }

    [Serializable]
    public class ActionCollideDetection
    {
        [ReadOnly, LabelText("碰撞检测片段列表")]
        public List<ActionCollideDetectionClipData> collideDetectionClips = new(); // 碰撞检测片段列表
    }

    [Serializable]
    public class ActionCollideDetectionClipData
    {
        [ReadOnly, LabelText("片段类型")]
        public ActionCollideDetectionType type = ActionCollideDetectionType.None; // 碰撞检测片段类型

        [ReadOnly, LabelText("片段开始时间点")] public float startTime; // 碰撞检测片段开始时间点，影响开始帧
        [ReadOnly, LabelText("片段时长")] public float duration; // 碰撞检测片段时长，影响持续帧数
        [ReadOnly, LabelText("片段开始帧")] public int startTick; // 碰撞检测片段开始帧
        [ReadOnly, LabelText("片段持续帧数")] public int durationTicks; // 碰撞检测片段持续帧数
        [ReadOnly, LabelText("片段组")] public string groupId; // 碰撞检测片段组，用于外部判定是否处于同一碰撞，过滤多种碰撞

        [ReadOnly, LabelText("片段类型数据"), SerializeReference]
        public ActionCollideDetectionBaseData data = new ActionCollideDetectionEmptyData(); // 碰撞检测片段类型数据
#if UNITY_EDITOR
        public string channelId;
#endif
    }

    public enum ActionCollideDetectionType
    {
        None,
        Bind,
        Box,
        Sphere,
        Sector,
    }

    [Serializable]
    public abstract class ActionCollideDetectionBaseData : ICloneable
    {
        public object Clone()
        {
            return SerializationUtility.CreateCopy(this);
        }
    }

    [Serializable]
    public class ActionCollideDetectionEmptyData : ActionCollideDetectionBaseData
    {
    }

    [Serializable]
    public class ActionCollideDetectionBindData : ActionCollideDetectionBaseData
    {
    }

    [Serializable]
    public abstract class ActionCollideDetectionShapeData : ActionCollideDetectionBaseData
    {
#if UNITY_EDITOR
        [LabelText("是否在编辑器中展示")] public bool showInEditor = false;
#endif

        [LabelText("本地位置")] public Vector3 localPosition = Vector3.zero;
    }

    [Serializable]
    public class ActionCollideDetectionBoxData : ActionCollideDetectionShapeData
    {
        [LabelText("本地旋转量")] public Quaternion localRotation = Quaternion.identity;
        [LabelText("尺寸")] public Vector3 size = Vector3.one;
    }

    [Serializable]
    public class ActionCollideDetectionSphereData : ActionCollideDetectionShapeData
    {
        [LabelText("半径"), MinValue(0f)] public float radius = 1f;
    }

    [Serializable]
    public class ActionCollideDetectionSectorData : ActionCollideDetectionShapeData
    {
        [LabelText("本地旋转量")] public Quaternion localRotation = Quaternion.identity;
        [LabelText("内环半径"), MinValue(0f)] public float insideRadius = 0f;

        [FormerlySerializedAs("radius")] [LabelText("外圈半径"), MinValue(0f)]
        public float outsideRadius = 1f;

        [LabelText("高度")] public float height = 1f;
        [LabelText("扇形中心角度")] public float anglePivot = 0f;
        [LabelText("扇形角度")] public float angle = 60f;
    }

    public enum ActionEventParameter
    {
        None,
        Bool,
        Int,
        Float,
        String,
        UnityObject,
    }

    [Serializable]
    public class ActionEvent
    {
        [ReadOnly, LabelText("事件片段列表")] public List<ActionEventClipData> eventClips = new(); // 事件片段列表
    }

    [Serializable]
    public class ActionEventClipData
    {
        [ReadOnly, LabelText("片段名称")] public string name; // 事件片段名称
        [ReadOnly, LabelText("片段触发时间点")] public float time; // 事件片段触发时间点
        [ReadOnly, LabelText("片段触发帧")] public int tick; // 事件片段触发帧

        [FormerlySerializedAs("type")] [ReadOnly, LabelText("片段参数类型")]
        public ActionEventParameter parameter; // 事件片段触发类型

        [ReadOnly, LabelText("片段参数值")] [ShowIf("parameter", ActionEventParameter.Bool)]
        public bool boolPayload; // 事件片段参数值

        [ReadOnly, LabelText("片段参数值")] [ShowIf("parameter", ActionEventParameter.Int)]
        public int intPayload; // 事件片段参数值

        [ReadOnly, LabelText("片段参数值")] [ShowIf("parameter", ActionEventParameter.Float)]
        public float floatPayload; // 事件片段参数值

        [ReadOnly, LabelText("片段参数值")] [ShowIf("parameter", ActionEventParameter.String)]
        public string stringPayload; // 事件片段参数值

        [ReadOnly, LabelText("片段参数值")] [ShowIf("parameter", ActionEventParameter.UnityObject)]
        public Object objectPayload; // 事件片段参数值
#if UNITY_EDITOR
        public string channelId;
#endif
    }

#if UNITY_EDITOR
    [Serializable]
    public class ActionChannelData
    {
        [ReadOnly, LabelText("通道id")] public string id;
        [ReadOnly, LabelText("通道名称")] public string name;
    }
#endif
}