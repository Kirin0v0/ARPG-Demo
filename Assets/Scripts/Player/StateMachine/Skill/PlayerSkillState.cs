using System;
using Animancer;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using Skill;
using Skill.Runtime;
using UnityEngine;

namespace Player.StateMachine.Skill
{
    public class PlayerSkillState : PlayerState
    {
        [Title("动画")] [SerializeField] private StringAsset idleTransition;

        [Title("调试")] [SerializeField] private bool debug;

        private SkillReleaseInfo CurrentSkillReleaseInfo => PlayerCharacter?.SkillAbility?.NewestReleasingSkill;

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "技能状态: " + CurrentSkillReleaseInfo.Name, guiStyle);
            }
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            // 设置玩家默认动画
            PlayerCharacter.AnimationAbility?.SwitchBase(idleTransition);
            // 进入技能状态时设置标识符
            PlayerCharacter.PlayerParameters.inSkill = true;
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            // 退出技能状态时重新设置姿势，本质上是重新设置动画过渡库
            PlayerCharacter.SetPose(PlayerCharacter.HumanoidParameters.pose);
            // 退出技能状态时设置标识符
            PlayerCharacter.PlayerParameters.inSkill = false;
        }
    }
}