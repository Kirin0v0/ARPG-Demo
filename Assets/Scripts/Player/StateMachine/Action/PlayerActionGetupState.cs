using System;
using System.Collections.Generic;
using Animancer;
using Character.Data;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Player.StateMachine.Dead;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Action
{
    [Serializable]
    public class PlayerGetupConfigData
    {
        public PlayerDeadPose deadPose;
        public TransitionAsset getupTransition;
    }

    public class PlayerActionGetupState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private List<PlayerGetupConfigData> getupConfigs;
        [SerializeField] private TransitionAsset defaultGetupTransition;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        public override bool AllowEnter(IState currentState)
        {
            // 只有满足非死亡且上一状态是死亡状态才允许进入
            return base.AllowEnter(currentState) && !PlayerCharacter.Parameters.dead &&
                   currentState is PlayerDeadState;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            var deadState = previousState as PlayerDeadState;
            var data = getupConfigs.Find(getupConfigData => getupConfigData.deadPose == deadState.DeadPose);
            if (data != null)
            {
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(data.getupTransition, true);
            }
            else
            {
                _animancerState = PlayerCharacter.AnimationAbility.PlayAction(defaultGetupTransition, true);
            }

            _animancerState.SharedEvents.OnEnd ??= HandleGetupAnimationEnd;
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState.SharedEvents.OnEnd = null;
            _animancerState = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "起身动作", guiStyle);
            }
        }

        private void HandleGetupAnimationEnd()
        {
            Parent.SwitchToDefault();
        }
    }
}