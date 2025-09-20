using Animancer;
using Character;
using Common;
using Damage;
using Damage.Data;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Defence
{
    public class PlayerDefenceStartState : PlayerDefenceState
    {
        [Title("状态切换")] [SerializeField] private string idleStateName;
        [SerializeField] private string parryStateName;

        [Title("调试")] [SerializeField] private bool debug;

        [Inject] private DamageManager _damageManager;

        private AnimancerState _animancerState;

        private CharacterObject _parryTarget;

        public override float AllowChangeStateTime => 0.3f;
        public override bool InitialState => true;
        public override bool DamageResistant => true;

        protected override void EnterAfterCheck()
        {
            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;

            // 播放开始防御动画
            _animancerState =
                PlayerCharacter.AnimationAbility.PlayAction(
                    weaponData.Defence.defenceAbility.startParameter.transition, true);
            _animancerState.SharedEvents.OnEnd = OnStartEnd;

            // 进入后立即播放音效
            if (weaponData.Defence.defenceAbility.startParameter.playAudio)
            {
                PlayerCharacter.AudioAbility?.PlaySound(
                    weaponData.Defence.defenceAbility.startParameter.audioClipRandomizer.Random(),
                    false,
                    weaponData.Defence.defenceAbility.startParameter.audioVolume
                );
            }

            // 进入时设置标识符
            PlayerCharacter.PlayerParameters.inPerfectDefence = true;

            // 监听伤害处理
            _damageManager.AfterDamageHandled += HandlePlayerDamage;
        }

        protected override void UpdateAfterCheck()
        {
            _parryTarget = null;
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            if (_parryTarget)
            {
                if (nextState is PlayerDefenceParryState parryState)
                {
                    parryState.SetParryTarget(_parryTarget);
                }

                _parryTarget = null;
            }

            // 停止开始防御动画
            if (_animancerState != null)
            {
                PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                _animancerState.SharedEvents.OnEnd = null;
                _animancerState = null;
            }

            // 退出时设置标识符
            PlayerCharacter.PlayerParameters.inPerfectDefence = false;

            // 解除监听伤害处理
            _damageManager.AfterDamageHandled -= HandlePlayerDamage;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "开始防御状态", guiStyle);
            }
        }

        private void OnStartEnd()
        {
            if (Parent.SwitchState(idleStateName, true))
            {
                return;
            }

            Parent.SwitchToDefault();
        }

        private void HandlePlayerDamage(DamageInfo damageInfo)
        {
            if (damageInfo.Source == PlayerCharacter || damageInfo.Target != PlayerCharacter)
            {
                return;
            }

            // 判断伤害触发标识符是否触发了完美防御
            if ((damageInfo.TriggerFlags & DamageInfo.PerfectDefenceFlag) != 0)
            {
                // 设置格挡对象
                _parryTarget = damageInfo.Source;
                // 切换到格挡状态
                Parent.SwitchState(parryStateName, true);
            }
        }
    }
}