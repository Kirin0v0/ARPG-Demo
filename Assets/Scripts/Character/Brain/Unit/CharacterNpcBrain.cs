using System;
using Character;
using Character.BehaviourTree;
using Character.Brain;
using Common;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using UnityEngine;
using VContainer;

namespace Character.Brain.Unit
{
    public class CharacterNpcBrain : CharacterBehaviourTreeBrain
    {
        protected override void SetBlackboardBeforeExecuteTick(Blackboard blackboard, float deltaTime)
        {
            blackboard.SetBoolParameter("inDialogue", Owner.Parameters.inDialogue);
        }

        protected override void GetBlackboardAfterExecuteTick(Blackboard blackboard)
        {
        }
    }
}