using Damage;
using Damage.Data;
using UnityEngine;
using VContainer;

namespace Character.Brain.Unit
{
    public class CharacterDummyBrain : CharacterBrain
    {
        [Inject] private DamageManager _damageManager;

        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
            // 如果自身死亡，则重新恢复全部的Hp和Mp
            if (Owner.Parameters.dead)
            {
                _damageManager.AddDamage(
                    Owner,
                    Owner,
                    DamageEnvironmentMethod.Default,
                    DamageType.DirectHeal,
                    new DamageValue
                    {
                        noType = -Owner.Parameters.property.maxHp,
                    },
                    DamageResourceMultiplier.Hp,
                    0f,
                    Vector3.zero,
                    true
                );
                _damageManager.AddDamage(
                    Owner,
                    Owner,
                    DamageEnvironmentMethod.Default,
                    DamageType.DirectHeal,
                    new DamageValue
                    {
                        noType = -Owner.Parameters.property.maxHp,
                    },
                    DamageResourceMultiplier.Mp,
                    0f,
                    Vector3.zero,
                    true
                );
            }
        }
    }
}