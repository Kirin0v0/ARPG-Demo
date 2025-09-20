using System;
using System.Collections.Generic;
using System.Linq;
using Buff.Data;
using Buff.SO;
using Character;
using Common;
using Framework.Common.Debug;
using Framework.Common.UI.PopupText;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Buff
{
    public class BuffManager : MonoBehaviour
    {
        [FormerlySerializedAs("buffPool")] [LabelText("Buff池")] [SerializeField] private BuffPool buffPoolTemplate;

        [LabelText("Buff标签规则")] [SerializeField]
        private BaseBuffTagRuleSO buffTagRule;

        [Inject] private IObjectResolver _objectResolver;
        [Inject] private PopupTextManager _popupTextManager;

        private BuffPool _runningBuffPool;

        private void Awake()
        {
            _runningBuffPool = buffPoolTemplate.Clone();
            _objectResolver.Inject(_runningBuffPool);
            _runningBuffPool.BuffConfigs.ForEach(buffConfig =>
            {
                buffConfig.onAdd?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onModify?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onTick?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onRemove?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onHit?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onBeHurt?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onKill?.ForEach(logic => _objectResolver.Inject(logic));
                buffConfig.onBeKilled?.ForEach(logic => _objectResolver.Inject(logic));
            });
        }

        private void OnDestroy()
        {
            _runningBuffPool.Clear();
            GameObject.Destroy(_runningBuffPool);
            _runningBuffPool = null;
        }

        public bool TryGetBuffInfo(string buffId, out BuffInfo buffInfo)
        {
            return _runningBuffPool.TryGetBuffInfo(buffId, out buffInfo);
        }

        public void AddBuff(BuffAddInfo addInfo)
        {
            // 检查目标角色是否拥有Buff能力，没有则说明Buff在其身上不生效
            if (!addInfo.Target.BuffAbility)
            {
                return;
            }

            // 目标角色无敌状态下无法添加非同阵营来源的Buff
            if (addInfo.Target.Parameters.immune && addInfo.Target.Parameters.side != addInfo.Caster.Parameters.side)
            {
                DebugUtil.LogOrange(
                    $"角色({addInfo.Target.Parameters.DebugName})无敌，无视来自角色({addInfo.Caster.Parameters.DebugName})非友方Buff(id={addInfo.Info.id})的添加");
                return;
            }

            // 检查是否需要执行Buff标签规则的禁止添加逻辑
            if (buffTagRule)
            {
                // 获取Buff目标角色身上的Buff列表，检查是否存在禁止该Buff添加的旧Buff，有则不执行后续逻辑
                foreach (var oldBuff in addInfo.Target.Parameters.buffs)
                {
                    if (!buffTagRule.AllowNewBuffAddWhenOldBuffExists(addInfo.Info.tag, oldBuff.info.tag))
                    {
                        return;
                    }
                }
            }

            // 获取Buff施法者
            var buffCasters = new List<CharacterObject>();
            if (addInfo.Caster)
            {
                buffCasters.Add(addInfo.Caster);
            }

            // 获取Buff目标角色上的同id的Buff列表
            var targetBuffs = addInfo.Target.BuffAbility.GetBuffs(addInfo.Info.id, buffCasters);
            var addStack = Mathf.Clamp(
                addInfo.Stack,
                0,
                Mathf.Max(addInfo.Info.maxStack, 0)
            );

            // 判断Buff是否已存在，根据情况执行添加层数或者添加新Buff的逻辑
            if (targetBuffs.Count > 0)
            {
                // 直接取第一个Buff（默认优先级最高）
                var buff = targetBuffs[0];

                // 重置运行时参数
                buff.RuntimeParams = new Dictionary<string, object>();
                if (addInfo.RuntimeParams != null)
                {
                    addInfo.RuntimeParams.ForEach(pair => { buff.RuntimeParams.Add(pair.Key, pair.Value); });
                }

                // 设置持续时间
                switch (addInfo.DurationType)
                {
                    case BuffAddDurationType.AppendDuration:
                    {
                        buff.duration += addInfo.Duration;
                    }
                        break;
                    case BuffAddDurationType.SetDuration:
                    {
                        buff.duration = addInfo.Duration;
                    }
                        break;
                }

                // 调整Buff层数并执行节点函数
                addStack = buff.stack + addStack > buff.info.maxStack
                    ? buff.info.maxStack - buff.stack
                    : addStack;
                buff.permanent = addInfo.Permanent;
                buff.expectTime = buff.duration;
                ModifyBuff(addInfo.Target, buff, addStack);
            }
            else
            {
                // 创建Buff并执行节点函数
                var runtimeParams = new Dictionary<string, object>();
                if (addInfo.RuntimeParams != null)
                {
                    addInfo.RuntimeParams.ForEach(pair => { runtimeParams.Add(pair.Key, pair.Value); });
                }

                var buff = new Runtime.Buff
                {
                    runningNumber = MathUtil.RandomId(),
                    info = addInfo.Info,
                    permanent = addInfo.Permanent,
                    duration = addInfo.Duration,
                    elapsedTime = 0,
                    expectTime = addInfo.Duration,
                    stack = addStack,
                    tickTimes = 0,
                    caster = addInfo.Caster,
                    carrier = addInfo.Target,
                    RuntimeParams = runtimeParams
                };
                AddBuff(addInfo.Target, buff);
            }

            // 检查是否需要执行Buff标签规则的移除逻辑
            if (buffTagRule)
            {
                // 获取Buff目标角色身上的Buff列表，通过Tag获取列表中在添加新Buff后会被移除的旧Buff，最后移除那些Buff
                addInfo.Target.Parameters.buffs
                    .Where(oldBuff => buffTagRule.IsRemovedOldBuffAfterNewBuffAdded(oldBuff.info.tag, addInfo.Info.tag))
                    .ToArray()
                    .ForEach(oldBuff =>
                    {
                        RemoveBuff(new BuffRemoveInfo
                        {
                            Info = oldBuff.info,
                            Caster = oldBuff.caster,
                            Target = oldBuff.carrier,
                            Stack = -oldBuff.info.maxStack,
                            DurationType = BuffRemoveDurationType.SetDuration,
                            Duration = 0,
                            RuntimeParams = new(),
                        });
                    });
            }
        }

        public void RemoveBuff(BuffRemoveInfo removeInfo)
        {
            // 检查目标角色是否拥有Buff能力，没有则说明Buff在其身上不生效
            if (!removeInfo.Target.BuffAbility)
            {
                return;
            }

            // // 目标角色无敌状态下无法移除非同阵营来源的Buff
            // if (removeInfo.Target.Parameters.immune &&
            //     removeInfo.Target.Parameters.side != removeInfo.Caster.Parameters.side)
            // {
            //     DebugUtil.LogOrange(
            //         $"角色{removeInfo.Target.Parameters.name}无敌，无视角色{removeInfo.Caster.Parameters.name}非友方Buff(id={removeInfo.Info.id})的移除");
            //     return;
            // }

            // 获取Buff施法者
            var buffCasters = new List<CharacterObject>();
            if (removeInfo.Caster)
            {
                buffCasters.Add(removeInfo.Caster);
            }

            // 获取Buff目标角色上的同id的Buff列表
            var targetBuffs = removeInfo.Target.BuffAbility.GetBuffs(removeInfo.Info.id, buffCasters);
            var removeStack = Mathf.Clamp(
                removeInfo.Stack,
                Mathf.Min(-removeInfo.Info.maxStack, 0),
                0
            );
            // 如果Buff不存在，就不用删除Buff
            if (targetBuffs.Count <= 0)
            {
                return;
            }

            // 直接取第一个Buff（默认优先级最高）
            var buff = targetBuffs[0];

            // 重置运行时参数
            buff.RuntimeParams = new Dictionary<string, object>();
            if (removeInfo.RuntimeParams != null)
            {
                removeInfo.RuntimeParams.ForEach(pair => { buff.RuntimeParams.Add(pair.Key, pair.Value); });
            }

            // 设置持续时间
            switch (removeInfo.DurationType)
            {
                case BuffRemoveDurationType.AppendDuration:
                {
                    buff.duration += removeInfo.Duration;
                }
                    break;
                case BuffRemoveDurationType.SetDuration:
                {
                    buff.duration = removeInfo.Duration;
                }
                    break;
            }

            // 判断是否将层数减少到0，是则直接删除Buff，否则只是调整Buff层数
            buff.expectTime = buff.duration;
            if (buff.stack + removeStack <= 0)
            {
                buff.stack = 0;
                RemoveBuff(removeInfo.Target, buff);
            }
            else
            {
                ModifyBuff(removeInfo.Target, buff, removeStack);
            }
        }

        public void AddBuff(CharacterObject character, Buff.Runtime.Buff buff)
        {
            character.Parameters.buffs.Add(buff);
            buff.info.OnAdd?.Invoke(buff);
            // 添加后重新对角色Buff列表进行排序
            character.Parameters.buffs.Sort((a, b) => a.info.priority.CompareTo(b.info.priority));
            // 最终更新目标角色属性
            character.PropertyAbility?.CheckProperty();
            // 显示添加Buff飘字
            _popupTextManager.ShowAddBuffPopupText(buff.info.name, buff.info.tag, buff.carrier.Visual.Center.position,
                buff.carrier.Visual.Center.position - buff.caster.Visual.Center.position);
        }

        public void RemoveBuff(CharacterObject character, Buff.Runtime.Buff buff)
        {
            character.Parameters.buffs.Remove(buff);
            buff.info.OnRemove?.Invoke(buff);
            // 最终更新目标角色属性
            character.PropertyAbility?.CheckProperty();
            // 显示移除Buff飘字
            _popupTextManager.ShowRemoveBuffPopupText(buff.info.name, buff.info.tag, buff.carrier.Visual.Center.position,
                buff.carrier.Visual.Center.position - buff.caster.Visual.Center.position);
        }

        private void ModifyBuff(CharacterObject character, Buff.Runtime.Buff buff, int modifyStack)
        {
            buff.stack += modifyStack;
            buff.info.OnModify?.Invoke(buff, modifyStack);
            // 最终更新目标角色属性
            character.PropertyAbility?.CheckProperty();
        }
    }
}