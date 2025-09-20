using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.Debug;
using Player;
using Player.StateMachine.Action;
using Player.StateMachine.Defence;
using UnityEngine;

namespace Damage.SO
{
    [CreateAssetMenu(menuName = "Damage/Trigger Mechanism/Simple")]
    public class SimpleDamageTriggerMechanismSO : BaseDamageTriggerMechanismSO
    {
        public override int GetTriggerFlags(DamageInfo damageInfo)
        {
            var flags = 0;
            // 如果目标角色处于破防状态，则触发破防机制，否则才会检测其他机制
            if (damageInfo.Target.Parameters.broken)
            {
                flags |= DamageInfo.BrokenFlag;
            }
            else
            {
                // 如果目标角色处于防御状态，则触发防御机制
                if (damageInfo.Type.AllowDefenceImpact() && damageInfo.Target.Parameters.inDefence)
                {
                    flags |= DamageInfo.DefenceFlag;
                }

                // 如果目标角色是玩家，就额外检测完美防御和完美闪避这两种机制
                if (damageInfo.Type.AllowTriggerPerfectMechanism() && damageInfo.Target is PlayerCharacterObject player)
                {
                    // 检测是否触发完美防御
                    if (player.PlayerParameters.inPerfectDefence)
                    {
                        flags |= DamageInfo.PerfectDefenceFlag;
                    }

                    // 检测是否触发完美闪避
                    if (player.PlayerParameters.inPerfectEvade)
                    {
                        flags |= DamageInfo.PerfectEvadeFlag;
                    }
                }
            }

            return flags;
        }
    }
}