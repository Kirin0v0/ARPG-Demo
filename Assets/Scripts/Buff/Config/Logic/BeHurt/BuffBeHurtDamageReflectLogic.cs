using System;
using Character;
using Damage;
using Damage.Data;
using Damage.Data.Extension;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Buff.Config.Logic.BeHurt
{
    [Serializable]
    public class BuffBeHurtDamageReflectLogic : BaseBuffBeHurtLogic
    {
        [Title("伤害数值配置")] [InfoBox("固定反射伤害数值，数值都不得小于0")] [SerializeField]
        private DamageValue fixedDamage = DamageValue.Zero;

        [InfoBox("伤害反射系数，数值都不得小于0")] [SerializeField]
        private DamageValueMultiplier damageMultiplier = DamageValueMultiplier.Zero;

        [InfoBox("最终反射伤害转资源系数，数值都不得小于0")] [SerializeField]
        private DamageResourceMultiplier resourceMultiplier = DamageResourceMultiplier.Hp;

        [Inject] private DamageManager _damageManager;

        public override void OnBuffBeHurt(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject attacker)
        {
            // 如果伤害无视反射，则不处理
            if (damageInfo.Type.IgnoreReflect())
            {
                return;
            }

            // 计算反射伤害数值
            var multiplier = damageMultiplier.Check(false);
            var reflectDamage = fixedDamage + new DamageValue
            {
                noType = (int)(damageInfo.Value.noType * multiplier.noType),
                physics = (int)(damageInfo.Value.physics * multiplier.physics),
                fire = (int)(damageInfo.Value.fire * multiplier.fire),
                ice = (int)(damageInfo.Value.ice * multiplier.ice),
                wind = (int)(damageInfo.Value.wind * multiplier.wind),
                lightning = (int)(damageInfo.Value.lightning * multiplier.lightning),
            };
            // 对伤害源头造成反射伤害
            _damageManager.AddDamage(
                source: buff.carrier,
                target: attacker,
                method: new DamageBuffMethod
                {
                    Name = buff.info.name
                },
                type: DamageType.TrueDamage,
                value: reflectDamage,
                resourceMultiplier: resourceMultiplier,
                criticalRate: 0f,
                direction: -damageInfo.Direction,
                ignoreSideLimit: true
            );
        }
    }
}