using Character.Collider;
using UnityEngine;

namespace Character
{
    public static class CharacterExtension
    {
        /// <summary>
        /// 从目标物体中获取角色信息
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        public static bool TryGetCharacter(
            this UnityEngine.Collider collider,
            out CharacterObject character
        )
        {
            if (collider.gameObject.TryGetComponent<CharacterReference>(out var reference))
            {
                character = reference.Value;
                return true;
            }

            if (collider.gameObject.TryGetComponent<CharacterObject>(out character))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 从目标物体中获取受击角色信息
        /// </summary>
        /// <param name="hitCollider"></param>
        /// <param name="character"></param>
        /// <param name="damageMultiplier"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static bool TryGetHitCharacter(
            this UnityEngine.Collider hitCollider,
            out CharacterObject character,
            out float damageMultiplier,
            out int priority
        )
        {
            damageMultiplier = 1f;
            priority = 0;
            if (hitCollider.gameObject.TryGetComponent<CharacterHitBoxCollider>(out var hitBoxCollider))
            {
                character = hitBoxCollider.Owner;
                damageMultiplier = hitBoxCollider.DamageMultiplier;
                priority = hitBoxCollider.Priority;
                return true;
            }

            if (hitCollider.gameObject.TryGetComponent<CharacterObject>(out character))
            {
                return true;
            }

            return false;
        }
    }
}