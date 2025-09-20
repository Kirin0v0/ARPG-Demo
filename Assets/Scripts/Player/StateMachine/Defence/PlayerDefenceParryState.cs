using Animancer;
using Character;
using Common;
using Damage;
using Damage.Data;
using Events;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Defence
{
    public class PlayerDefenceParryState : PlayerDefenceState
    {
        [Title("调试")] [SerializeField] private bool debug;

        [Inject] private DamageManager _damageManager;
        [Inject] private GameManager _gameManager;

        private AnimancerState _animancerState;
        private CharacterObject _parryTarget;

        public override float AllowChangeStateTime => 0f;
        public override bool InitialState => false;
        public override bool DamageResistant => true;

        protected override void EnterAfterCheck()
        {
            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;

            // 播放动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(
                weaponData.Defence.defenceAbility.parryParameter.transition, true);
            _animancerState.SharedEvents.OnEnd = OnStartEnd;

            // 进入后立即播放音效
            if (weaponData.Defence.defenceAbility.parryParameter.playAudio)
            {
                PlayerCharacter.AudioAbility?.PlaySound(
                    weaponData.Defence.defenceAbility.parryParameter.audioClipRandomizer.Random(),
                    false,
                    weaponData.Defence.defenceAbility.parryParameter.audioVolume
                );
            }

            // 进入时设置标识符
            PlayerCharacter.PlayerParameters.inPerfectDefence = true;

            // 进入时对格挡对象造成破防伤害
            if (_parryTarget)
            {
                var damageValue = (PlayerCharacter.Parameters.physicsAttack + PlayerCharacter.Parameters.magicAttack) *
                                  weaponData.Defence.defenceAbility.parryParameter.breakAttackMultiplier;
                _damageManager.AddDamage(
                    PlayerCharacter,
                    _parryTarget,
                    new DamageParryMethod(),
                    DamageType.TrueDamage,
                    new DamageValue
                    {
                        physics = (int)damageValue,
                    },
                    DamageResourceMultiplier.Break,
                    0f,
                    (_parryTarget.Parameters.position - PlayerCharacter.Parameters.position).normalized
                );
            }

            // 进入即添加全局时间缩放指令
            _gameManager.AddTimeScaleGlobalCommand("PlayerParry", 0.5f);
        }

        protected override void UpdateAfterCheck()
        {
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 停止动画
            if (_animancerState != null)
            {
                PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                _animancerState.SharedEvents.OnEnd = null;
                _animancerState = null;
            }

            // 退出时设置标识符
            PlayerCharacter.PlayerParameters.inPerfectDefence = false;

            // 退出即删除全局时间缩放指令
            _gameManager.RemoveTimeScaleCommand("PlayerParry");

            // 退出时发送激活魔女时间的事件
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.TriggerWitchTime);

            _parryTarget = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "防御格挡状态", guiStyle);
            }
        }

        public void SetParryTarget(CharacterObject target)
        {
            _parryTarget = target;
        }

        private void OnStartEnd()
        {
            Parent.SwitchToDefault();
        }
    }
}