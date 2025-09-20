using System;
using Character;
using Damage.Data;

namespace Buff.Config.Logic.BeHurt
{
    [Serializable]
    public abstract class BaseBuffBeHurtLogic
    {
        public abstract void OnBuffBeHurt(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject attacker);
    }
}