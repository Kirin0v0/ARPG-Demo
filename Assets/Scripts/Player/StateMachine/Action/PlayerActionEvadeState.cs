using Animancer;
using Common;
using Damage;
using Damage.Data;
using Events;
using Framework.Common.StateMachine;
using Player.StateMachine.Attack;
using Player.StateMachine.Defence;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Action
{
    public class PlayerActionEvadeState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset evadeTransition;
        [SerializeField] private StringAsset forwardInputParameter;
        [SerializeField] private StringAsset lateralInputParameter;
        [SerializeField] private StringAsset anticipationEvent;
        [SerializeField] private StringAsset judgmentEvent;
        [SerializeField] private StringAsset recoveryEvent;
        [SerializeField] private StringAsset evadeAgainEvent;

        [Title("闪避攻击倒计时")] [SerializeField] private float evadeAttackCountdown = 1f;

        [Title("状态切换")] [SerializeField] private string jumpStateName;
        [SerializeField] private string evadeStateName;
        [SerializeField] private string defenceStateName;
        [SerializeField] private string attackStateName;

        [Title("调试")] [SerializeField] private bool debug;

        [Inject] private DamageManager _damageManager;
        [Inject] private GameManager _gameManager;

        private AnimancerState _animancerState;

        private bool _allowReceiveInput;
        private bool _allowEvadeAgain;

        private bool _triggerPerfectEvade;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 判断玩家输入转相机坐标系在角色本地坐标系的位移是否为零，为零则默认设置为向后移动
            Vector3 characterMovementInFrame;
            if (PlayerCharacter.PlayerParameters.playerInputCharacterMovementInFrame.magnitude == 0)
            {
                characterMovementInFrame = -transform.forward;
            }
            else
            {
                characterMovementInFrame =
                    PlayerCharacter.PlayerParameters.playerInputCharacterMovementInFrame.normalized;
            }

            // 设置翻滚参数
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(lateralInputParameter,
                characterMovementInFrame.x);
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(forwardInputParameter,
                characterMovementInFrame.z);

            // 播放翻滚动画
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(evadeTransition, true);
            _animancerState.SharedEvents.OnEnd ??= OnEvadeEnd;
            _animancerState.SharedEvents.AddCallback(judgmentEvent, HandleJudgmentEvent);
            _animancerState.SharedEvents.AddCallback(recoveryEvent, HandleRecoveryEvent);
            _animancerState.SharedEvents.AddCallback(evadeAgainEvent, HandleEvadeAgainEvent);

            // 在翻滚期间翻滚攻击倒计时为无限
            PlayerCharacter.PlayerParameters.evadeAttackCountdown = float.MaxValue;
            
            // 监听伤害处理
            _damageManager.AfterDamageHandled += HandlePlayerDamage;

            // 重置标识符
            _triggerPerfectEvade = false;
            _allowReceiveInput = false;
            _allowEvadeAgain = false;
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            // 允许接受玩家输入则处理输入
            if (_allowReceiveInput)
            {
                if (PlayerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame)
                {
                    if (Parent.SwitchState(jumpStateName, true))
                    {
                        return;
                    }
                }

                if (_allowEvadeAgain && PlayerCharacter.PlayerParameters.isEvadeInFrame)
                {
                    if (Parent.SwitchState(evadeStateName, true))
                    {
                        return;
                    }
                }

                if (PlayerCharacter.PlayerParameters.isEnterDefendInFrame)
                {
                    if (Parent.SwitchState(defenceStateName, true))
                    {
                        return;
                    }
                }

                if (PlayerCharacter.PlayerParameters.isAttackInFrame ||
                    PlayerCharacter.PlayerParameters.isHeavyAttackInFrame)
                {
                    if (Parent.SwitchState(attackStateName, true))
                    {
                        return;
                    }
                }
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 重置翻滚攻击倒计时
            PlayerCharacter.PlayerParameters.evadeAttackCountdown = evadeAttackCountdown;

            // 隐藏动画层并置空回调
            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState.SharedEvents.OnEnd = null;
            _animancerState.SharedEvents.RemoveCallback(judgmentEvent, HandleJudgmentEvent);
            _animancerState.SharedEvents.RemoveCallback(recoveryEvent, HandleRecoveryEvent);
            _animancerState.SharedEvents.RemoveCallback(evadeAgainEvent, HandleEvadeAgainEvent);
            _animancerState = null;
            
            // 解除监听伤害处理
            _damageManager.AfterDamageHandled -= HandlePlayerDamage;
            
            // 重置触发标识符
            _triggerPerfectEvade = false;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "翻滚动作", guiStyle);
            }
        }

        private void OnEvadeEnd()
        {
            Parent.SwitchToDefault();
        }

        private void HandleJudgmentEvent()
        {
            // 开始预输入
            PlayerCharacter.Brain.StartInputBuffer();
            // 开启角色无敌和霸体
            PlayerCharacter.StateAbility?.StartImmune(nameof(PlayerActionEvadeState), float.MaxValue);
            PlayerCharacter.StateAbility?.StartEndure(nameof(PlayerActionEvadeState), float.MaxValue);
            // 设置自身进入完美闪避状态
            PlayerCharacter.PlayerParameters.inPerfectEvade = true;
        }

        private void HandleRecoveryEvent()
        {
            _allowReceiveInput = true;
            // 停止预输入
            PlayerCharacter.Brain.StopInputBuffer();
            // 关闭角色无敌和霸体
            PlayerCharacter.StateAbility?.StopImmune(nameof(PlayerActionEvadeState));
            PlayerCharacter.StateAbility?.StopEndure(nameof(PlayerActionEvadeState));
            // 设置自身退出完美闪避状态
            PlayerCharacter.PlayerParameters.inPerfectEvade = false;
            // 如果在期间触发了完美闪避，则发送删除全局时间缩放指令并发送激活魔女时间的事件
            if (_triggerPerfectEvade)
            {
                _gameManager.RemoveTimeScaleCommand("PlayerEvade");
                GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.TriggerWitchTime);
            }
        }

        private void HandleEvadeAgainEvent()
        {
            _allowEvadeAgain = true;
        }

        private void HandlePlayerDamage(DamageInfo damageInfo)
        {
            if (damageInfo.Source == PlayerCharacter || damageInfo.Target != PlayerCharacter)
            {
                return;
            }
            
            // 判断伤害触发标识符是否触发了完美闪避
            if ((damageInfo.TriggerFlags & DamageInfo.PerfectEvadeFlag) != 0 && !_triggerPerfectEvade)
            {
                _triggerPerfectEvade = true;
                // 添加全局时间缩放指令
                _gameManager.AddTimeScaleGlobalCommand("PlayerEvade", 0.5f);
            }
        }
    }
}