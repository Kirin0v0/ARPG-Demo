namespace Damage.Data.Extension
{
    public static class DamageDataExtension
    {
        public static bool IsHeal(this DamageType damageType)
        {
            return damageType == DamageType.DirectHeal || damageType == DamageType.PeriodHeal;
        }

        public static bool IgnoreSettlement(this DamageType damageType)
        {
            return damageType == DamageType.TrueDamage;
        }

        public static bool IgnoreImmune(this DamageType damageType)
        {
            // 真实伤害和治疗无视无敌
            return damageType == DamageType.TrueDamage || damageType.IsHeal();
        }

        public static bool IgnoreReflect(this DamageType damageType)
        {
            // 真实伤害和治疗无视反射
            return damageType == DamageType.TrueDamage || damageType.IsHeal();
        }

        public static bool AllowCritical(this DamageType damageType)
        {
            // 周期和真实伤害不允许暴击
            return !(damageType == DamageType.PeriodDamage || damageType == DamageType.TrueDamage ||
                     damageType == DamageType.PeriodHeal);
        }

        public static bool AllowDefenceImpact(this DamageType damageType)
        {
            return damageType != DamageType.TrueDamage && !damageType.IsHeal();
        }

        public static bool AllowTriggerPerfectMechanism(this DamageType damageType)
        {
            return damageType == DamageType.DirectDamage;
        }
    }
}