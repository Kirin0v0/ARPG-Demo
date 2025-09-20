using System;
using Damage.Data;
using UnityEngine;

namespace Common
{
    [Serializable]
    public class AlgorithmDamageAndResourceAndAtbTestParameters
    {
        public int attackerReaction;
        public int attackerLuck;
        public int attackerPhysicsAttack;
        public int attackerMagicAttack;
        public DamageMethod damageMethod;
        public int defenderReaction;
        public int defenderDefence;
    }
    
    public interface IAlgorithmTest
    {
        public void TestDamageAndResourceAndAtb(
            int attackerReaction,
            int attackerLuck,
            int attackerPhysicsAttack,
            int attackerMagicAttack,
            DamageMethod damageMethod,
            DamageValue fixedDamage,
            DamageValueMultiplier damageTimes,
            DamageType damageType,
            DamageResourceMultiplier resourceMultiplier,
            int defenderReaction,
            int defenderDefence,
            float damageMultiplier,
            DamageValueType weakness,
            DamageValueType immunity
        );
    }
}