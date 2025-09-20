using Character.Data;
using UnityEngine;

namespace Buff.SO
{
    [CreateAssetMenu(menuName = "Buff/Property Calculate/Simple")]
    public class SimpleBuffPropertyCalculateSO: BaseBuffPropertyCalculateSO
    {
        public override (CharacterProperty plus, CharacterPropertyMultiplier times) CalculateBuffProperty(Runtime.Buff buff)
        {
            // 加区和乘区属性为层数x单层属性
            var plus = buff.info.singleStackPlusProperty * buff.stack;
            var times = buff.info.singleStackTimesProperty * buff.stack;
            return (plus, times);
        }
    }
}