using Character.Data;
using Common;
using Framework.Common.Debug;

namespace Character.Ability
{
    public class CharacterPropertyAbility: BaseCharacterNecessaryAbility
    {
        protected readonly AlgorithmManager AlgorithmManager;

        public CharacterPropertyAbility(AlgorithmManager algorithmManager)
        {
            AlgorithmManager = algorithmManager;
        }

        public CharacterProperty BaseProperty
        {
            get => Owner.Parameters.baseProperty;
            set
            {
                // 设置基础属性后调整最终属性和当前资源
                Owner.Parameters.baseProperty = value;
                CheckProperty();
            }
        }

        public void CheckProperty()
        {
            // 每次检查时重置角色属性
            Owner.Parameters.property = CharacterProperty.Zero;

            // 遍历计算Buff属性
            Owner.Parameters.buffPlusProperty = CharacterProperty.Zero;
            Owner.Parameters.buffTimesProperty = CharacterPropertyMultiplier.DefaultTimes;
            Owner.Parameters.buffs.ForEach(buff =>
            {
                if (!AlgorithmManager) return;
                // 这里计算Buff最终属性影响
                var buffProperty = AlgorithmManager.BuffPropertyCalculateSO.CalculateBuffProperty(buff);
                // 加区和乘区的每个Buff影响都是加算，避免乘算导致数值爆炸
                Owner.Parameters.buffPlusProperty += buffProperty.plus;
                Owner.Parameters.buffTimesProperty += buffProperty.times;
            });

            // 最终计算角色属性和当前资源
            Owner.Parameters.buffTimesProperty = Owner.Parameters.buffTimesProperty.Check();
            Owner.Parameters.property = CalculateFinalProperty();
            Owner.ResourceAbility?.Modify(CharacterResource.Empty);

            // 根据最终属性计算攻击力和防御力
            if (AlgorithmManager)
            {
                var attackTuple =
                    AlgorithmManager.CharacterAttackAlgorithmSO.CalculateAttack(Owner.Parameters.property);
                Owner.Parameters.physicsAttack = attackTuple.physicsAttack;
                Owner.Parameters.magicAttack = attackTuple.magicAttack;
                Owner.Parameters.defence =
                    AlgorithmManager.CharacterDefenceAlgorithmSO.CalculateDefence(Owner.Parameters.property);
            }
            else
            {
                Owner.Parameters.physicsAttack = 0;
                Owner.Parameters.magicAttack = 0;
                Owner.Parameters.defence = 0;
            }
        }

        protected virtual CharacterProperty CalculateFinalProperty()
        {
            return ((Owner.Parameters.baseProperty + Owner.Parameters.buffPlusProperty) *
                    Owner.Parameters.buffTimesProperty).Check();
        }
    }
}