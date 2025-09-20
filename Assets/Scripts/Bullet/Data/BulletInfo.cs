using System;
using System.Collections.Generic;
using Character;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bullet.Data
{
    public enum BulletColliderType
    {
        Box, // 0: Vector3 localPosition, 1: Vector3 localRotation, 2: Vector3 size
        Sphere, // 0: Vector3 localPosition, 1: float radius
    }

    [Serializable]
    public struct BulletInfo
    {
        public string id; // 子弹Id
        public int hitTimes; // 子弹命中次数，每命中一个对象则减少一次，到0则销毁
        public float hitColdDown; // 子弹命中对象后的冷却时间，单位为秒，如果为0或负数则会等待一个逻辑帧后才进行下一次命中逻辑
        public BulletColliderType colliderType; // 子弹碰撞体类型
        public object[] ColliderTypeParams; // 子弹碰撞类型参数
        public bool destroyOnObstacle; // 子弹是否在障碍物上销毁，如果为否就会实现无视障碍物的效果
        public LayerMask obstacleLayers; // 障碍物标签
        public bool hitEnemy; // 子弹是否允许命中敌人
        public bool hitAlly; // 子弹是否允许命中友军
        public bool hitSelf; // 子弹是否允许命中自身

        #region 以下为事件委托

        public System.Action<BulletObject> OnCreate; // 子弹创建事件
        public System.Action<BulletObject, CharacterObject> OnHit; // 子弹命中角色事件
        public System.Action<BulletObject> OnDestroy; // 子弹销毁事件

        #endregion
    }
}