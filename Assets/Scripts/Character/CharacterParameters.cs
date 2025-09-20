using System;
using System.Collections.Generic;
using Character.Data;
using Damage.Data;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;

namespace Character
{
    public enum CharacterSide
    {
        Player,
        Enemy,
        Neutral,
    }

    public enum CharacterBattleState
    {
        Idle,
        Warning,
        Battle,
    }

    [Serializable]
    public class CharacterParameters
    {
        public string DebugName => $"{name}-{id}";
        
        public int id; // 角色id，角色对象的唯一标识符
        public string prototype; // 角色原型，角色对象的标识符，与id不同，不同角色对象存在同一原型的情况

        public string[] tags; // 角色标签，用于区分角色
        public string name; // 角色名称
        public Vector3 spawnPoint; // 角色出生点
        public Vector3 position; // 角色位置
        public Quaternion rotation; // 角色旋转量
        public float forwardAngle; // 角色面朝向角度
        public bool visible; // 角色是否在相机中可见

        [Title("角色控制")] public bool dead; // 角色是否死亡
        public bool stunned; // 角色是否硬直
        public bool broken; // 角色是否破防
        public bool inDefence; // 角色是否处于防御
        public bool inDialogue; // 角色是否处于对话
        public CharacterControl control; // 角色控制，影响玩家输入对角色的控制

        [Title("角色移动")] public bool touchGround; // 是否接触地面
        public float verticalSpeed; // 这里我们规定角色平面移动采用RootMotion方式不计算速度，只有垂直方向的移动需要计算速度
        public bool Airborne => !touchGround || verticalSpeed > 0f;
        public Vector3 movementInFrame; // 该帧角色的位移量，由于存在碰撞，不一定完全符合实际位移

        [Title("角色属性")] public CharacterProperty baseProperty = CharacterProperty.Zero; // 基础属性，指角色无装备无Buff影响的自身属性
        public CharacterProperty buffPlusProperty = CharacterProperty.Zero; // Buff计算相加属性，即所有Buff影响的相加值

        public CharacterPropertyMultiplier
            buffTimesProperty = CharacterPropertyMultiplier.DefaultTimes; // Buff计算相乘属性，即所有Buff影响的相乘值

        public CharacterProperty property = CharacterProperty.Zero; // 角色最终属性
        public int physicsAttack; // 最终属性计算出来的物理攻击力，用于伤害和治疗计算
        public int magicAttack; // 最终属性计算出来的魔法攻击力，用于伤害和治疗计算
        public int defence; // 最终属性计算出来的防御力，用于伤害计算，注意，治疗不受影响
        public float normalDamageMultiplier; // 常态伤害乘算系数，玩家不使用该系数设置
        public float defenceDamageMultiplier; // 破防伤害乘算系数，玩家不使用该系数设置
        public float brokenDamageMultiplier; // 破防伤害乘算系数，玩家不使用该系数设置
        public float damageMultiplier; // 伤害乘算系数，用于伤害计算，默认为1，注意，治疗不受影响（玩家通过状态设置系数，而非玩家则通过预先属性设置系数）
        public DamageValueType weakness; // 角色弱点，用于伤害计算，注意，治疗不受影响
        public DamageValueType immunity; // 角色免疫，用于伤害计算，注意，治疗不受影响

        [Title("角色资源")] public CharacterResource resource = CharacterResource.Empty; // 角色资源

        [Title("角色状态")] public bool immune; // 是否处于无敌，是则攻击不再对该角色判定，无敌状态下自带霸体和不可破防效果
        public bool endure; // 是否处于霸体，是则无法减少和增加硬直值
        public bool unbreakable; // 是否处于不可破防状态，是则无法减少和增加破防值

        [Title("角色Buff")] public List<Buff.Runtime.Buff> buffs = new(); // Buff列表

        [Title("角色技能")] public List<Skill.Runtime.Skill> skills = new(); // 技能列表

        [Title("角色战斗")] public CharacterSide side; // 角色阵营，相对玩家而言，存在中立变成友好或者敌对的情况
        public CharacterBattleState battleState; // 角色战斗状态
        public string battleId = ""; // 当前正在战斗的战斗ID

        [Title("角色掉落物")] public CharacterDrop drop = CharacterDrop.Empty; // 角色在战斗中死亡后的掉落物
    }
}