using Character;
using Damage.Data;
using Damage.Data.Extension;
using UnityEngine;

namespace Damage.SO
{
    [CreateAssetMenu(menuName = "Damage/Attack Convert/Simple")]
    public class SimpleDamageAttackConvertSO : BaseDamageAttackConvertSO
    {
        public override DamageValue ConvertToDamageValue(
            DamageValue originFixedDamage,
            DamageValueMultiplier originDamageTimes,
            DamageType damageType,
            CharacterParameters attacker
        )
        {
            originFixedDamage = originFixedDamage.Check(damageType.IsHeal());
            originDamageTimes = originDamageTimes.Check(damageType.IsHeal());

            // 计算单纯攻击力增幅后的伤害，物理攻击力影响物理攻击，魔法攻击力影响四大元素攻击和无属性攻击
            var attackDamage = originFixedDamage + new DamageValue
            {
                noType = (int)(originDamageTimes.noType * attacker.magicAttack),
                physics = (int)(originDamageTimes.physics * attacker.physicsAttack),
                fire = (int)(originDamageTimes.fire * attacker.magicAttack),
                ice = (int)(originDamageTimes.ice * attacker.magicAttack),
                wind = (int)(originDamageTimes.wind * attacker.magicAttack),
                lightning = (int)(originDamageTimes.lightning * attacker.magicAttack),
            };
            return attackDamage.Check(damageType.IsHeal());
        }
    }
}