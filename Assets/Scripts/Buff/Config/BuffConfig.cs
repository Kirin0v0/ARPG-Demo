using System;
using System.Collections.Generic;
using System.Linq;
using Buff.Config.Logic.Add;
using Buff.Config.Logic.BeHurt;
using Buff.Config.Logic.BeKilled;
using Buff.Config.Logic.Hit;
using Buff.Config.Logic.Kill;
using Buff.Config.Logic.Modify;
using Buff.Config.Logic.Remove;
using Buff.Config.Logic.Tick;
using Buff.Data;
using Buff.Data.Extension;
using Character;
using Character.Data;
using Damage.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.Config
{
    public class BuffConfig : SerializedScriptableObject
    {
        [Title("Buff通用配置"), ReadOnly] public string id; // Buff id，这个由编辑器来生成

        [HorizontalGroup("通用", 120f), HideLabel, PreviewField(120f, ObjectFieldAlignment.Left)]
        public Sprite icon; // Buff图标

        [VerticalGroup("通用/右侧")] public new string name; // Buff名称

        [VerticalGroup("通用/右侧")] public int priority = 1; // Buff优先级，优先级越小的buff越后面执行

        [VerticalGroup("通用/右侧")] public int maxStack = 1; // Buff堆叠层最大值

        [VerticalGroup("通用/右侧")] public float tickTime; // Buff工作周期

        [VerticalGroup("通用/右侧")] public BuffTag tag = BuffTag.None; // Buff标签

        [VerticalGroup("通用/右侧")] public BuffVisibility visibility = BuffVisibility.Invisible; // Buff可见度

        [FoldoutGroup("Buff描述配置"), TextArea] public string description = "";

        [InfoBox("这里是预先定义好的占位符，可直接填入描述中，最终会动态输出为内部数据")] [HorizontalGroup("Buff描述配置/展示"), ReadOnly]
        public List<string> placeHolders = new();

        [InfoBox("这里会展示描述的实际输出")] [HorizontalGroup("Buff描述配置/展示"), ReadOnly, TextArea]
        public string preview;

        [Title("Buff控制配置")] public bool impactToControl = false;
        [ShowIf("impactToControl")] public CharacterControl control = CharacterControl.Origin;

        [Title("Buff属性配置")] public bool impactToProperty = false;
        [ShowIf("impactToProperty")] public CharacterProperty singleStackPlusProperty = CharacterProperty.Zero;

        [ShowIf("impactToProperty")]
        public CharacterPropertyMultiplier singleStackTimesProperty = CharacterPropertyMultiplier.Zero;

        #region 自定义回调列表

        [Title("Buff添加时回调"), TypeFilter("GetAddLogicFilteredTypeList")] [InfoBox("添加至当前未持有该Buff的角色时调用")]
        public List<BaseBuffAddLogic> onAdd;

        [Title("Buff调整时回调"), TypeFilter("GetModifyLogicFilteredTypeList")]
        [InfoBox("当前持有该Buff的角色身上的Buff调整（不包括添加和删除）时调用")]
        public List<BaseBuffModifyLogic> onModify;

        [Title("Buff逻辑帧回调"), TypeFilter("GetTickLogicFilteredTypeList")] [InfoBox("持有Buff后每隔一定的工作周期时调用")]
        public List<BaseBuffTickLogic> onTick;

        [Title("Buff移除时回调"), TypeFilter("GetRemoveLogicFilteredTypeList")] [InfoBox("从当前持有该Buff的角色移除时调用")]
        public List<BaseBuffRemoveLogic> onRemove;

        [Title("Buff携带者攻击时回调"), TypeFilter("GetHitLogicFilteredTypeList")] [InfoBox("当前持有该Buff的角色对其他角色造成伤害时调用")]
        public List<BaseBuffHitLogic> onHit;

        [Title("Buff携带者被攻击时回调"), TypeFilter("GetBeHurtLogicFilteredTypeList")] [InfoBox("当前持有该Buff的角色受到其他角色的伤害时调用")]
        public List<BaseBuffBeHurtLogic> onBeHurt;

        [Title("Buff携带者击杀时回调"), TypeFilter("GetKillLogicFilteredTypeList")] [InfoBox("当前持有该Buff的角色击杀其他角色时调用")]
        public List<BaseBuffKillLogic> onKill;

        [Title("Buff携带者被击杀时回调"), TypeFilter("GetBeKilledLogicFilteredTypeList")] [InfoBox("当前持有该Buff的角色被其他角色击杀时调用")]
        public List<BaseBuffBeKilledLogic> onBeKilled;

        #endregion

        public BuffInfo ToBuffInfo()
        {
            return new BuffInfo
            {
                id = id,
                name = name,
                icon = icon,
                priority = priority,
                maxStack = maxStack,
                tickTime = tickTime,
                tag = tag,
                visibility = visibility,
                Description = description,
                control = impactToControl ? control : CharacterControl.Origin,
                singleStackPlusProperty = impactToProperty ? singleStackPlusProperty : CharacterProperty.Zero,
                singleStackTimesProperty =
                    impactToProperty ? singleStackTimesProperty : CharacterPropertyMultiplier.Zero,
                OnAdd = OnBuffAdd,
                OnModify = OnBuffModify,
                OnTick = OnBuffTick,
                OnRemove = OnBuffRemove,
                OnHit = OnBuffHit,
                OnBeHurt = OnBuffBeHurt,
                OnKill = OnBuffKill,
                OnBeKilled = OnBuffBeKilled
            };
        }

        private void OnBuffAdd(Runtime.Buff buff)
        {
            onAdd?.ForEach(template => template.OnBuffAdd(buff));
        }

        private void OnBuffModify(Runtime.Buff buff, int stack)
        {
            onModify?.ForEach(template => template.OnBuffModify(buff, stack));
        }

        private void OnBuffTick(Runtime.Buff buff)
        {
            onTick?.ForEach(template => template.OnBuffTick(buff));
        }

        private void OnBuffRemove(Runtime.Buff buff)
        {
            onRemove?.ForEach(template => template.OnBuffRemove(buff));
        }

        private void OnBuffHit(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject attacker)
        {
            if (onHit == null)
            {
                return;
            }

            foreach (var template in onHit)
            {
                template.OnBuffHit(buff, ref damageInfo, attacker);
            }
        }

        private void OnBuffBeHurt(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject attacker)
        {
            if (onBeHurt == null)
            {
                return;
            }

            foreach (var template in onBeHurt)
            {
                template.OnBuffBeHurt(buff, ref damageInfo, attacker);
            }
        }

        private void OnBuffKill(Runtime.Buff buff, CharacterObject target)
        {
            if (onKill == null)
            {
                return;
            }

            foreach (var template in onKill)
            {
                template.OnBuffKill(buff, target);
            }
        }

        private void OnBuffBeKilled(Runtime.Buff buff, CharacterObject attacker)
        {
            if (onBeKilled == null)
            {
                return;
            }

            foreach (var template in onBeKilled)
            {
                template.OnBuffBeKilled(buff, attacker);
            }
        }

        private IEnumerable<Type> GetAddLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffAddLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffAddLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetModifyLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffModifyLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffModifyLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetTickLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffTickLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffTickLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetRemoveLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffRemoveLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffRemoveLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetHitLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffHitLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffHitLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetBeHurtLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffBeHurtLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffBeHurtLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetKillLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffKillLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffKillLogic).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetBeKilledLogicFilteredTypeList()
        {
            var q = typeof(BaseBuffBeKilledLogic).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseBuffBeKilledLogic).IsAssignableFrom(x));
            return q;
        }

        private void OnValidate()
        {
            // 只要数据变更就刷新占位符和预览文字
            placeHolders = BuffInfoFormatter.GetPlaceHolderDefinitions();
            if (string.IsNullOrEmpty(description))
            {
                preview = "";
            }
            else
            {
                var buffInfo = ToBuffInfo();
                preview = buffInfo.Description;
            }
        }
    }
}