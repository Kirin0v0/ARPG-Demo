using System;
using Buff.Data;
using Damage;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Buff.Config.Logic.Remove
{
    [Serializable]
    public class BuffRemoveStackReplaceLogic : BaseBuffRemoveLogic
    {
        [Title("层数配置")]
        [InfoBox("如果当前Buff移除时层数在指定层数以上，则重新创建Buff，同时减少指定层数，以到达减少层数替换去除Buff的效果")]
        [SerializeField, MinValue(1)]
        private int reduceStackWhenRemoveBuff = 1;

        [SerializeField] private float duration;

        [Inject] private BuffManager _buffManager;

        public override void OnBuffRemove(Runtime.Buff buff)
        {
            if (buff.stack <= reduceStackWhenRemoveBuff)
            {
                return;
            }

            _buffManager.AddBuff(new BuffAddInfo
            {
                Info = buff.info,
                Caster = buff.caster,
                Target = buff.carrier,
                Stack = buff.stack - reduceStackWhenRemoveBuff,
                Permanent = buff.permanent,
                DurationType = BuffAddDurationType.SetDuration,
                Duration = duration,
                RuntimeParams = buff.RuntimeParams
            });
        }
    }
}