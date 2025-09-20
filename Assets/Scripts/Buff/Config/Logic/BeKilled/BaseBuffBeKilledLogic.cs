using System;
using Character;

namespace Buff.Config.Logic.BeKilled
{
    [Serializable]
    public abstract class BaseBuffBeKilledLogic
    {
        public abstract void OnBuffBeKilled(Runtime.Buff buff, CharacterObject attacker);
    }
}