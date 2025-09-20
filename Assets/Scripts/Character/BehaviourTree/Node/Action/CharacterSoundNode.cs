using System;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Sound")]
    public class CharacterSoundNode : ActionNode
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public override string Description =>
            "角色音效节点，播放指定音效并由自身管理生命周期，当前帧立即返回成功";

        protected override void OnStart(object payload)
        {
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            if (audioClip)
            {
                parameters.Character.AudioAbility?.PlaySound(audioClip, false, volume);
            }
            return NodeState.Success;
        }
    }
}