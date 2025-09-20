using Character.Data;
using UnityEngine;

namespace Player.SO
{
    [CreateAssetMenu(menuName = "Player/Level Property Rule/Simple")]
    public class SimplePlayerLevelPropertyRuleSO : BasePlayerLevelPropertyRuleSO
    {
        public override int CalculateLevelUpExperience(int level)
        {
            return level * 50;
        }

        public override CharacterProperty CalculateLevelProperty(int level)
        {
            // 无论等级如何，玩家硬直时间都是2/3秒
            var meter = 1 + level * 1f;
            var reduceSpeed = (int)(1.5 * meter);
            return new CharacterProperty
            {
                maxHp = 30 + level * 10,
                maxMp = 10 + level * 5,
                stunMeter = meter,
                stunReduceSpeed = reduceSpeed,
                breakMeter = meter,
                breakReduceSpeed = reduceSpeed,
                atbLimit = 3,
                stamina = level,
                strength = level,
                magic = level,
                reaction = level * 2,
                luck = level * 2,
            };
        }
    }
}