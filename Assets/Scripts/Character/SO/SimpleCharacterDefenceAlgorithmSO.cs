using Character.Data;
using UnityEngine;

namespace Character.SO
{
    [CreateAssetMenu(menuName = "Character/Defence Algorithm/Simple")]
    public class SimpleCharacterDefenceAlgorithmSO: BaseCharacterDefenceAlgorithmSO
    {
        [SerializeField, Min(0f)] private float initialSectionFactor = 1f;
        [SerializeField, Min(0f)] private float sectionRange = 5f;
        [SerializeField, Min(0f)] private float sectionAccumulateFactor = 0.2f;
        
        public override int CalculateDefence(CharacterProperty property)
        {
            // 根据耐力计算防御力
            return CalculateDefenceValue(property.stamina);
        }
        
        /// <summary>
        /// 计算防御力，使用简易分段计算
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private int CalculateDefenceValue(int property)
        {
            var defenceValue = 0f;
            if (property <= 0)
            {
                return (int)defenceValue;
            }

            var sections = property / sectionRange;
            var lastSectionValue = property % sectionRange;
            var sectionFactor = initialSectionFactor;
            var sectionIndex = 0;
            while (sectionIndex < sections)
            {
                defenceValue += sectionFactor * sectionRange;
                sectionFactor += sectionAccumulateFactor;
                sectionIndex++;
            }

            defenceValue += sectionFactor * lastSectionValue;

            return (int)defenceValue;
        }
    }
}