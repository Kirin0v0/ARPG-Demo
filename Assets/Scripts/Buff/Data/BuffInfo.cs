using System;
using Buff.Data.Extension;
using Character;
using Character.Data;
using Damage.Data;
using Framework.Common.Timeline;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.Data
{
    public enum BuffVisibility
    {
        Invisible, // 完全不可见
        LowVisibility, // 低可见度，角色状态列表展示Buff图标和名称，此时鼠标移上去会展示Buff描述
        FullVisible, // 完全可见，在战斗UI中都展示Buff图标，角色状态列表展示Buff图标和名称，此时鼠标移上去会展示Buff描述
    }

    [Serializable]
    public struct BuffInfo
    {
        public string id; // Buff id
        public string name; // Buff名称
        public Sprite icon; // Buff图标
        public int priority; // Buff优先级，优先级越小的buff越后面执行
        public int maxStack; // Buff堆叠层最大值
        public float tickTime; // Buff工作周期
        public BuffTag tag; // Buff标签
        public BuffVisibility visibility; // Buff可见度
        private string _description; // Buff描述，可内置预定义占位符

        [ShowInInspector]
        public string Description
        {
            set => _description = value;
            get => BuffInfoFormatter.Format(_description, this);
        }

        public CharacterControl control; // Buff对角色的控制影响
        public CharacterProperty singleStackPlusProperty; // 单层Buff对角色相加的属性影响
        public CharacterPropertyMultiplier singleStackTimesProperty; // 单层Buff对角色相乘的属性影响

        #region 以下为事件委托

        public OnBuffAdd OnAdd;

        public OnBuffModify OnModify;

        public OnBuffTick OnTick;

        public OnBuffRemove OnRemove;

        public OnBuffHit OnHit;

        public OnBuffBeHurt OnBeHurt;

        public OnBuffKill OnKill;

        public OnBuffBeKilled OnBeKilled;

        #endregion
    }

    /// <summary>
    /// Buff添加回调（原先没有该Buff时添加，无论添加多少层都只执行一次）
    /// <param name="buff">Buff对象</param>
    /// </summary>
    public delegate void OnBuffAdd(Runtime.Buff buff);

    /// <summary>
    /// Buff在调整时（在添加时不触发，在删除时也不触发）执行的函数
    /// <param name="buff">Buff对象</param>
    /// <param name="modifyStack">本次改变的层数</param>
    /// </summary>
    public delegate void OnBuffModify(Runtime.Buff buff, int modifyStack);

    /// <summary>
    /// Buff在每个工作周期会执行的函数，如果这个函数为空，或者未进入新的Tick，都不会发生周期性工作
    /// <param name="buff">Buff对象</param>
    /// </summary>
    public delegate void OnBuffTick(Runtime.Buff buff);

    /// <summary>
    /// Buff删除回调（剩余时间为0或层数为0时删除，只执行一次）
    /// <param name="buff">Buff对象</param>
    /// </summary>
    public delegate void OnBuffRemove(Runtime.Buff buff);

    /// <summary>
    /// 在伤害流程中，攻击者在持有该Buff时的回调
    /// <param name="buff">Buff对象</param>
    /// <param name="damageInfo">伤害信息</param>
    /// <param name="target">目标角色对象</param>
    /// </summary>
    public delegate void OnBuffHit(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject target);

    /// <summary>
    /// 在伤害流程中，受击者在持有该Buff时的回调
    /// <param name="buff">Buff对象</param>
    /// <param name="damageInfo">伤害信息</param>
    /// <param name="attacker">攻击角色对象</param>
    ///</summary>
    public delegate void OnBuffBeHurt(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject attacker);

    /// <summary>
    /// 在伤害流程中，攻击者在持有该Buff击杀目标后的回调
    /// <param name="buff">会传递给脚本buffObj作为参数</param>
    /// <param name="target">目标角色对象</param>
    /// </summary>
    public delegate void OnBuffKill(Runtime.Buff buff, CharacterObject target);

    /// <summary>
    /// 在伤害流程中，受击者在持有该Buff被击杀后的回调
    /// <param name="buff">会传递给脚本buffObj作为参数</param>
    /// <param name="attacker">攻击角色对象</param>
    /// </summary>
    public delegate void OnBuffBeKilled(Runtime.Buff buff, CharacterObject attacker);
}