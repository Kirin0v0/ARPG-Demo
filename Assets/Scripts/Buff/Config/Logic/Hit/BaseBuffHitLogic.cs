using System;
using Character;
using Damage.Data;

namespace Buff.Config.Logic.Hit
{
    [Serializable]
    public abstract class BaseBuffHitLogic
    {
        public abstract void OnBuffHit(Runtime.Buff buff, ref DamageInfo damageInfo, CharacterObject target);
    }
}