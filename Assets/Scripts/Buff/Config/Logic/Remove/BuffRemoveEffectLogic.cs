using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.Config.Logic.Remove
{
    [Serializable]
    public class BuffRemoveEffectLogic : BaseBuffRemoveLogic
    {
        public enum TargetType
        {
            Caster,
            Carrier,
        }
        
        [Title("特效配置")] [SerializeField] private GameObject prefab;

        [SerializeField] private TargetType targetType;

        public override void OnBuffRemove(Runtime.Buff buff)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    buff.caster.EffectAbility?.RemoveEffect(prefab.name);
                }
                    break;
                case TargetType.Carrier:
                {
                    buff.carrier.EffectAbility?.RemoveEffect(prefab.name);
                }
                    break;
            }
        }
    }
}