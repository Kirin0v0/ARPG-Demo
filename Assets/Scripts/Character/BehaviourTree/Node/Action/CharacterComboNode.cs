using System;
using Combo;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Combo")]
    public class CharacterComboNode : ActionNode
    {
        [SerializeField] private ComboConfig comboConfig;

        private IComboPlay _comboPlayer;

        public override string Description => "招式节点，播放角色特定招式，直到该招式完成后才会返回成功";

        protected override void OnStart(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            // 初始化招式播放器并开始招式
            _comboPlayer = new ComboPlayer(
                comboConfig,
                parameters.Character,
                parameters.GameManager,
                parameters.DamageManager,
                parameters.AlgorithmManager
            );
            _comboPlayer.Start();
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (_comboPlayer.Stage == ComboStage.End)
            {
                return NodeState.Success;
            }
            
            _comboPlayer.Tick(deltaTime);

            return NodeState.Running;
        }

        protected override void OnStop(object payload)
        {
            _comboPlayer.Stop();
        }
    }
}