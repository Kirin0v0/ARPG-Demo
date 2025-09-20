using System.Collections.Generic;
using Character;
using Framework.Core.LiveData;
using Player;

namespace Features.Game
{
    public interface IGameModel
    {
        LiveData<GameTimeData> GetGameTime();
        LiveData<bool> AllowPlayerInput();
        LiveData<bool> ShowCursor();
        LiveData<bool> IsPlayerWitchTimeActive();
    }

    public enum GameTimeMode
    {
        Default,
        Pause,
    }

    public struct GameTimeData
    {
        private const int PauseTimeFlag = 1 << 0;

        private int _flags;

        public GameTimeMode Mode
        {
            get
            {
                if ((_flags & PauseTimeFlag) != 0)
                {
                    return GameTimeMode.Pause;
                }

                return GameTimeMode.Default;
            }
        }

        public GameTimeData PauseTime()
        {
            return new GameTimeData
            {
                _flags = _flags | PauseTimeFlag
            };
        }

        public GameTimeData ResumeTime()
        {
            return new GameTimeData
            {
                _flags = _flags & ~PauseTimeFlag
            };
        }
    }
}