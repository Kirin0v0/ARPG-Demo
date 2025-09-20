using Character.Data;
using UnityEngine;

namespace Character.Ability
{
    public class CharacterControlAbility: BaseCharacterNecessaryAbility
    {
        /// <summary>
        /// 检查角色控制状态
        /// </summary>
        public void CheckControl()
        {
            // 每帧都重置控制状态
            Owner.Parameters.control = CharacterControl.Origin;
            if (Owner.Parameters.dead)
            {
                Owner.Parameters.control += CharacterControl.Dead;
            }

            if (Owner.Parameters.stunned)
            {
                Owner.Parameters.control += CharacterControl.Stun;
            }

            if (Owner.Parameters.broken)
            {
                Owner.Parameters.control += CharacterControl.Break;
            }

            if (Owner.Parameters.inDialogue)
            {
                Owner.Parameters.control += CharacterControl.Dialogue;
            }

            // 最后遍历角色Buff影响控制
            Owner.Parameters.buffs.ForEach(buff => Owner.Parameters.control += buff.info.control);
        }
    }
}