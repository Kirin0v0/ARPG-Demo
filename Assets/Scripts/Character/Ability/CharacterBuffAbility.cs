using System.Collections.Generic;
using Buff;
using Buff.Data;
using Damage.Data;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterBuffAbility : BaseCharacterOptionalAbility
    {
        [Inject] private BuffManager _buffManager;

        public void Tick(float deltaTime)
        {
            // 我们规定如果角色死亡则冻结其Buff列表
            if (Owner.Parameters.dead)
            {
                return;
            }

            var removedBuffs = new List<Buff.Runtime.Buff>();
            // 每帧遍历Buff列表，在执行Buff生命周期函数时同时获取该被删除的Buff列表
            Owner.Parameters.buffs.ForEach(buff =>
            {
                // 如果Buff施法者不存在则删除该Buff
                if (!buff.caster)
                {
                    removedBuffs.Add(buff);
                    return;
                }
                
                // 记录Buff时间
                if (!buff.permanent)
                {
                    // 如果间隔时间超过剩余时间，就取剩余时间作为间隔时间
                    if (deltaTime >= buff.duration)
                    {
                        buff.duration = 0;
                        deltaTime = buff.duration;
                    }
                    else
                    {
                        buff.duration -= deltaTime;
                    }
                }

                buff.elapsedTime += deltaTime;

                // 如果Buff帧间隔大于0，则记录帧执行次数
                // 这里Buff的帧执行逻辑为Buff开始时不算为执行帧，等待一段工作时间后才开始执行帧，每执行一次帧，帧执行次数加1
                if (buff.info.tickTime > 0)
                {
                    var newTick = (int)(buff.elapsedTime / buff.info.tickTime);
                    while (buff.tickTimes < newTick)
                    {
                        // 如果有配置帧函数，则执行该帧函数
                        buff.info.OnTick?.Invoke(buff);
                        buff.tickTimes++;
                    }
                }

                // 如果Buff剩余时长小等于0或者Buff层数小于等于0或Buff是战斗Buff且在非战斗状态下，则将Buff添加到待删除列表中
                if (buff.duration <= 0 || buff.stack <= 0 ||
                    (buff.RuntimeParams.TryGetValue(BuffRuntimeParameters.ExistOnlyBattle, out var existOnlyBattle) &&
                     (bool)existOnlyBattle && Owner.Parameters.battleState != CharacterBattleState.Battle))
                {
                    removedBuffs.Add(buff);
                }
            });

            // 如果有待删除Buff，则遍历删除
            if (removedBuffs.Count > 0)
            {
                removedBuffs.ForEach(buff => { _buffManager.RemoveBuff(Owner, buff); });
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 删除所有Buff，这里不走回调逻辑（因为执行该函数时代表角色销毁，游戏设计上角色销毁不该触发任何回调）
            Owner.Parameters.buffs.Clear();
        }

        /// <summary>
        /// 获取符合条件的Buff列表，同一个角色身上存在同一Buff的多个实例，例如多个角色同时对该角色附加的某个Buff
        /// </summary>
        /// <param name="buffId">Buff Id</param>
        /// <param name="casters">Buff施法者列表，列表为空或null代表全部施法者的Buff，不为空则是特定的施法者的Buff</param>
        /// <returns>符合条件的Buff列表</returns>
        public List<Buff.Runtime.Buff> GetBuffs(string buffId, List<CharacterObject> casters = null)
        {
            var buffs = new List<Buff.Runtime.Buff>();
            Owner.Parameters.buffs.ForEach(buff =>
            {
                if (buff.info.id == buffId && (casters == null || casters.Count <= 0 || casters.Contains(buff.caster)))
                {
                    buffs.Add(buff);
                }
            });
            return buffs;
        }
    }
}