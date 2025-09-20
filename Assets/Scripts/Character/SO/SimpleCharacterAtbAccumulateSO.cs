using Character.Data;
using UnityEngine;

namespace Character.SO
{
    [CreateAssetMenu(menuName = "Character/Atb Accumulate/Simple")]
    public class SimpleCharacterAtbAccumulateSO : BaseCharacterAtbAccumulateSO
    {
        [SerializeField] private float reactionBaseNumber = 4f;
        [SerializeField] private float accumulationPerSecond = 0.01f;

        public override float AccumulateAtb(CharacterProperty property, float time)
        {
            // 使用Log对数来控制Atb累积乘积
            var reactionFactor = property.reaction <= 4 ? 1 : Mathf.Log(property.reaction, 4);
            return reactionFactor * accumulationPerSecond * time;
        }
    }
}