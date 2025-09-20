using System;
using System.Collections.Generic;
using Framework.Core.Singleton;
using Rendering;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class GameAllyInformationConfigurationData
{
    public string tag;
    public string prefixTitle;
    public Color color;
}

[CreateAssetMenu(menuName = "Global Rule Singleton Config")]
public class GlobalRuleSingletonConfigSO : SingletonScriptableObject<GlobalRuleSingletonConfigSO>
{
    [Title("层级配置")] public LayerMask uiLayer;
    [InfoBox("视为地面的层级，用于计算角色是否处于地面")] public LayerMask groundLayer;
    [FormerlySerializedAs("obstacleLayer")] [InfoBox("视为遮挡的层级，用于角色可视化检测")] public LayerMask maskLayer;

    [FormerlySerializedAs("characterSelfLayer")] [InfoBox("视为角色物理碰撞的层级，用于角色和场景之间以及角色间的物理碰撞")]
    public LayerMask characterPhysicsLayer;

    [FormerlySerializedAs("characterBattleLayer")] [InfoBox("视为角色战斗受击碰撞的层级，用于角色间受击碰撞以及角色与AoE和子弹的受击碰撞")]
    public LayerMask characterHitLayer;

    [InfoBox("视为角色模型的层级，用于角色物体整体默认配置")]
    public LayerMask characterModelLayer;

    [InfoBox("视为角色触发器的层级，将触发器设置为该层级，需要保证层级能够与角色物理层发生碰撞")] public string characterTriggerLayer;

    [InfoBox("视为交互物体的层级")] public LayerMask interactLayer;

    [InfoBox("视为子弹障碍物的层级，用于计算与子弹的交互")] public LayerMask bulletObstacleLayer;

    [Title("渲染配置")] public RenderingLayerMask outlineRenderingLayerMask;
    public RenderingLayerMask interactableRenderingLayerMask;
    public RenderingLayerMask lockRenderingLayerMask;
    public RenderingLayerMask targetRenderingLayerMask;

    [Title("碰撞检测配置")] [Min(0f)] public float collideDetectionExtraRadius = 0.5f;

    [Title("音频配置")] [Min(0f)] public float aoeSoundMinDistance = 1f;
    [Min(0f)] public float aoeSoundMaxDistance = 15f;
    [Min(0f)] public float bulletSoundMinDistance = 1f;
    [Min(0f)] public float bulletSoundMaxDistance = 15f;

    [Title("友方配置")] public Color allyDefaultColor;
    public List<GameAllyInformationConfigurationData> allyInformation = new();
        
    [Title("重力")] public float gravity = 9.8f;
}