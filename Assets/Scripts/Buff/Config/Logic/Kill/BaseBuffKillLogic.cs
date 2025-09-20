using System;
using Character;

namespace Buff.Config.Logic.Kill
{
    [Serializable]
    public abstract class BaseBuffKillLogic
    {
        public abstract void OnBuffKill(Runtime.Buff buff, CharacterObject target);
    }
}