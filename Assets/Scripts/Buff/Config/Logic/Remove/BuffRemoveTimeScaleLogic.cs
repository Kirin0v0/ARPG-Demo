using System;
using Common;
using VContainer;

namespace Buff.Config.Logic.Remove
{
    [Serializable]
    public class BuffRemoveTimeScaleLogic : BaseBuffRemoveLogic
    {
        [Inject] private GameManager _gameManager;

        public override void OnBuffRemove(Runtime.Buff buff)
        {
            _gameManager.RemoveTimeScaleCommand(buff.runningNumber);
        }
    }
}