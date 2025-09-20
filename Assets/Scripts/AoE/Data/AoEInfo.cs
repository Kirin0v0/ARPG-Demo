using System;
using System.Collections.Generic;
using Character;
using UnityEngine;
using UnityEngine.Serialization;

namespace AoE.Data
{
    public enum AoEColliderType
    {
        Box, // 0: Vector3 localPosition, 1: Vector3 localRotation, 2: Vector3 size
        Sphere, // 0: Vector3 localPosition, 1: float radius
        Sector, // 0: Vector3 localPosition, 1: Vector3 localRotation, 2: float insideRadius, 3: float radius, 4: float height, 5: float angle
    }

    [Serializable]
    public struct AoEInfo
    {
        public string id; // AoE id
        public float tickTime; // AoE每帧间隔，单位是秒，值为0代表不执行Tick事件
        public AoEColliderType colliderType; // AoE碰撞类型
        public object[] ColliderTypeParams; // AoE碰撞类型参数 
        public bool hitEnemy; // AoE是否允许命中敌人
        public bool hitAlly; // AoE是否允许命中友军
        public bool hitSelf; // AoE是否允许命中自身

        #region 以下为事件委托

        public System.Action<AoEObject> OnCreate; // AoE创建事件
        public System.Action<AoEObject, List<CharacterObject>> OnCharactersEnter; // AoE角色进入事件，事件与帧不挂钩，每个逻辑帧都会执行
        public System.Action<AoEObject, List<CharacterObject>> OnCharactersStay; // AoE角色保持事件，事件与帧不挂钩，每个逻辑帧都会执行
        public System.Action<AoEObject, List<CharacterObject>> OnCharactersLeave; // AoE角色退出事件，事件与帧不挂钩，每个逻辑帧都会执行
        public System.Action<AoEObject> OnTick; // AoE帧事件，帧事件仅在AoE时长进入新帧后执行
        public System.Action<AoEObject> OnDestroy; // AoE销毁事件

        #endregion
    }
}