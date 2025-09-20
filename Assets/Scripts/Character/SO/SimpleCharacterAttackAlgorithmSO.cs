using Character.Data;
using UnityEngine;

namespace Character.SO
{
    [CreateAssetMenu(menuName = "Character/Attack Algorithm/Simple")]
    public class SimpleCharacterAttackAlgorithmSO : BaseCharacterAttackAlgorithmSO
    {
        [SerializeField, Min(0f)] private float initialSectionFactor = 1f;
        [SerializeField, Min(0f)] private float sectionRange = 5f;
        [SerializeField, Min(0f)] private float sectionAccumulateFactor = 0.2f;
        
        public override (int physicsAttack, int magicAttack) CalculateAttack(CharacterProperty property)
        {
            // 根据力量计算物理攻击力，根据魔法计算魔法攻击力
            return (CalculateAttackValue(property.strength), CalculateAttackValue(property.magic));
        }

        /// <summary>
        /// 计算攻击力，使用简易分段计算
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private int CalculateAttackValue(int property)
        {
            var attackValue = 0f;
            if (property <= 0)
            {
                return (int)attackValue;
            }

            var sections = property / sectionRange;
            var lastSectionValue = property % sectionRange;
            var sectionFactor = initialSectionFactor;
            var sectionIndex = 0;
            while (sectionIndex < sections)
            {
                attackValue += sectionFactor * sectionRange;
                sectionFactor += 0.2f;
                sectionIndex++;
            }

            attackValue += sectionFactor * lastSectionValue;

            return (int)attackValue;
        }
    }
}