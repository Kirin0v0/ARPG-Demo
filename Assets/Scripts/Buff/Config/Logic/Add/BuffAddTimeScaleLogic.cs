using System;
using Common;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Buff.Config.Logic.Add
{
    [Serializable]
    public class BuffAddTimeScaleLogic : BaseBuffAddLogic
    {
        [Title("时间缩放配置")] [SerializeField] private float timeScale = 1f;

        [Inject] private GameManager _gameManager;

        public override void OnBuffAdd(Runtime.Buff buff)
        {
            _gameManager.AddTimeScaleSingleCommand(buff.runningNumber, timeScale, buff.carrier);
        }
    }
}