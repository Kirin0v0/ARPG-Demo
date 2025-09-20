using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Collider
{
    public class CharacterHitBoxCollider : CharacterCollider
    {
        [InfoBox("受击倍率")] [SerializeField] private float damageMultiplier = 1f;
        public float DamageMultiplier => damageMultiplier;

        [InfoBox("受击优先级，值越大则优先级越高，多用于同帧下多处受击仅一处造成伤害的场景下的受击盒排序")] [SerializeField]
        private int priority = 0;

        public int Priority => priority;
    }
}