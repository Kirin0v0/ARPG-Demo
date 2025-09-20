using System.Collections.Generic;
using Animancer;
using Animancer.Units;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Action;
using Player.StateMachine.Attack;
using Player.StateMachine.Base;
using Player.StateMachine.Defence;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Player.StateMachine.Locomotion
{
    public class PlayerLocomotionSprintState : PlayerLocomotionState
    {
        private const int SprintIdleState = -1;
        private const int SprintingState = 0;
        private const int SprintBrakeState = 1;
        private const int SprintTurnState = 2;

        private const int LiftUpLeftFoot = -1;
        private const int LiftUpNoneFoot = 0;
        private const int LiftUpRightFoot = 1;

        [Title("动画")] [SerializeField] private StringAsset sprintTransition;
        [SerializeField] private StringAsset sprintBrakeLeftFootLiftUpTransition;
        [SerializeField] private StringAsset sprintBrakeRightFootLiftUpTransition;
        [SerializeField] private StringAsset sprintTurnTransition;
        [SerializeField] private float maxSprintSpeed = 5f;
        [SerializeField, Seconds] private float parameterSmoothTime = 0.15f;
        [SerializeField, EventNames] private StringAsset liftUpLeftFootEvent;
        [SerializeField, EventNames] private StringAsset liftUpRightFootEvent;
        [SerializeField, EventNames] private StringAsset footPutDownEvent;

        [Title("脚步音效")] [SerializeField] private AudioClipRandomizer defaultAudioClipRandomizer;
        [SerializeField] [Range(0f, 1f)] private float defaultAudioVolume = 1f;
        [SerializeField] private List<PlayerFootstepsAudioConfigData> audioConfigs = new();
        [SerializeField, MinValue(0f)] private float minPlayAudioInterval = 0.3f;

        [Title("急停、转身时间")] [SerializeField] private float brakeAvailableTimes = 0.1f;
        [SerializeField] private float turnAvailableTimes = 0.05f;
        
        [Title("状态切换")] [SerializeField] private string moveStateName;
        [SerializeField] private string airborneStateName;
        [SerializeField] private string jumpStateName;
        [SerializeField] private string vaultStateName;
        [SerializeField] private string lowClimbStateName;
        [SerializeField] private string highClimbStateName;
        [SerializeField] private string equipStateName;
        [SerializeField] private string unequipStateName;
        [SerializeField] private string evadeStateName;
        [SerializeField] private string defenceStateName;
        [SerializeField] private string attackStateName;

        [Title("调试")] [SerializeField] private bool debug;

        private PlayerFootstepsPlayer _footstepsPlayer;
        private SmoothedFloatParameter _smoothedFloatParameter;
        private AnimancerState _animancerState;
        private float _forwardSpeedBeforeSprint;

        private float _brakeAccumulateTimes;
        private float _turnAccumulateTimes;

        private int _sprintState = SprintIdleState;

        private int _liftUpFoot;

        private int SprintState
        {
            get => _sprintState;
            set
            {
                if (_sprintState == value) return;
                
                switch (_sprintState)
                {
                    // 隐藏动画动作层
                    case SprintBrakeState:
                    case SprintTurnState:
                        PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                        break;
                    // 清除先前的动画事件
                    case SprintingState:
                        _animancerState?.SharedEvents
                            .RemoveCallbacks(liftUpLeftFootEvent, HandleLiftUpLeftFootEvent);
                        _animancerState?.SharedEvents
                            .RemoveCallbacks(liftUpRightFootEvent, HandleLiftUpRightFootEvent);
                        _animancerState?.SharedEvents.RemoveCallbacks(footPutDownEvent, HandleFootPutDownEvent);
                        break;
                }

                _sprintState = value;

                _animancerState = _sprintState switch
                {
                    SprintingState => PlayerCharacter.AnimationAbility.SwitchBase(sprintTransition),
                    SprintBrakeState when _liftUpFoot == LiftUpLeftFoot => PlayerCharacter.AnimationAbility
                        .PlayAction(sprintBrakeLeftFootLiftUpTransition, true),
                    SprintBrakeState when _liftUpFoot == LiftUpNoneFoot => PlayerCharacter.AnimationAbility
                        .PlayAction(sprintBrakeLeftFootLiftUpTransition, true),
                    SprintBrakeState when _liftUpFoot == LiftUpRightFoot => PlayerCharacter.AnimationAbility
                        .PlayAction(sprintBrakeRightFootLiftUpTransition, true),
                    SprintTurnState => PlayerCharacter.AnimationAbility.PlayAction(sprintTurnTransition,
                        true),
                    _ => null,
                };

                switch (_sprintState)
                {
                    // 设置动画事件
                    case SprintingState:
                        _animancerState?.SharedEvents
                            .AddCallbacks(liftUpLeftFootEvent, HandleLiftUpLeftFootEvent);
                        _animancerState?.SharedEvents
                            .AddCallbacks(liftUpRightFootEvent, HandleLiftUpRightFootEvent);
                        _animancerState?.SharedEvents.AddCallbacks(footPutDownEvent, HandleFootPutDownEvent);
                        break;
                }
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            _footstepsPlayer = new PlayerFootstepsPlayer(
                PlayerCharacter,
                defaultAudioClipRandomizer,
                defaultAudioVolume,
                audioConfigs,
                minPlayAudioInterval
            );
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            _smoothedFloatParameter = new SmoothedFloatParameter(PlayerCharacter.AnimationAbility.Animancer.Graph,
                forwardSpeedParameter, parameterSmoothTime);
            _forwardSpeedBeforeSprint = _smoothedFloatParameter.CurrentValue;

            // 进入冲刺状态一定切换到正在冲刺，而不是急停等状态
            _liftUpFoot = LiftUpNoneFoot;
            SprintState = SprintingState;
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            if (HandleEquipOrUnequip())
            {
                return;
            }

            if (HandleEvade())
            {
                return;
            }

            if (HandleDefence())
            {
                return;
            }

            if (HandleEnvironmentBehaviour())
            {
                return;
            }

            if (HandleAttack())
            {
                return;
            }

            UpdateSprintParameter();
            HandleMoveInput();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), SprintState switch
                {
                    SprintingState => "正在冲刺状态",
                    SprintBrakeState when _liftUpFoot == LiftUpLeftFoot => "抬左脚急停状态",
                    SprintBrakeState when _liftUpFoot == LiftUpNoneFoot => "默认抬脚急停状态",
                    SprintBrakeState when _liftUpFoot == LiftUpRightFoot => "抬右脚急停状态",
                    SprintTurnState => "冲刺转身状态",
                    _ => "",
                }, guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 清空动画参数
            _smoothedFloatParameter.Dispose();
            _smoothedFloatParameter = null;

            // 冲刺状态复位
            SprintState = SprintIdleState;

            _brakeAccumulateTimes = 0f;
            _turnAccumulateTimes = 0f;
        }

        protected override void OnClear()
        {
            base.OnClear();
            _footstepsPlayer = null;
        }

        private void HandleLiftUpLeftFootEvent()
        {
            _liftUpFoot = LiftUpLeftFoot;
        }

        private void HandleLiftUpRightFootEvent()
        {
            _liftUpFoot = LiftUpRightFoot;
        }

        private void HandleFootPutDownEvent()
        {
            // 去除权重总和异常的情况
            if (AnimancerEvent.Current.State.Weight <= 0f)
            {
                return;
            }

            // 播放脚步音效
            _footstepsPlayer.PlayAudio(Time.time);
        }

        /// <summary>
        /// 处理装备/卸下行为
        /// </summary>
        /// <returns></returns>
        protected bool HandleEquipOrUnequip()
        {
            if (PlayerCharacter.PlayerParameters.isEquipOrUnequipInFrame)
            {
                if (PlayerCharacter.HumanoidParameters.WeaponUsed)
                {
                    return Parent.SwitchState(unequipStateName, true);
                }
                else
                {
                    return Parent.SwitchState(equipStateName, true);
                }
            }

            return false;
        }

        /// <summary>
        /// 处理翻滚行为
        /// </summary>
        /// <returns></returns>
        public bool HandleEvade()
        {
            if (PlayerCharacter.PlayerParameters.isEvadeInFrame)
            {
                return Parent.SwitchState(evadeStateName, true);
            }

            return false;
        }

        /// <summary>
        /// 处理防御行为
        /// </summary>
        /// <returns></returns>
        public bool HandleDefence()
        {
            if (PlayerCharacter.PlayerParameters.isEnterDefendInFrame)
            {
                return Parent.SwitchState(defenceStateName, true);
            }

            return false;
        }

        /// <summary>
        /// 处理环境行为（即下落、跳跃、翻越、攀爬和悬挂行为等和环境交互的状态）
        /// </summary>
        /// <returns></returns>
        protected bool HandleEnvironmentBehaviour()
        {
            var parameters = PlayerCharacter.PlayerParameters;

            // 处于空中就直接切换下落状态
            if (PlayerCharacter.Parameters.Airborne)
            {
                return Parent.SwitchState(airborneStateName, true);
            }

            // 在按键输入后判断当前角色行为
            if (parameters.isJumpOrVaultOrClimbInFrame)
            {
                if (parameters.obstacleActionIdea == PlayerObstacleActionIdea.Vault)
                {
                    if (Parent.SwitchState(vaultStateName, true))
                    {
                        return true;
                    }
                }

                if (parameters.obstacleActionIdea == PlayerObstacleActionIdea.LowClimb)
                {
                    if (Parent.SwitchState(lowClimbStateName, true))
                    {
                        return true;
                    }
                }

                if (parameters.obstacleActionIdea == PlayerObstacleActionIdea.HighClimb)
                {
                    if (Parent.SwitchState(highClimbStateName, true))
                    {
                        return true;
                    }
                }

                return Parent.SwitchState(jumpStateName, true);
            }

            return false;
        }

        private bool HandleAttack()
        {
            if (PlayerCharacter.PlayerParameters.isAttackInFrame ||
                PlayerCharacter.PlayerParameters.isHeavyAttackInFrame)
            {
                if (Parent.SwitchState(attackStateName, true))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateSprintParameter()
        {
            if (_animancerState == null)
            {
                return;
            }

            if (SprintState == SprintingState)
            {
                // 保证正在冲刺时，向前速度也在递增
                _smoothedFloatParameter.TargetValue = Mathf.Clamp(
                    _forwardSpeedBeforeSprint + Mathf.Repeat(_animancerState.NormalizedTime, 1f) * maxSprintSpeed,
                    _smoothedFloatParameter.CurrentValue,
                    maxSprintSpeed
                );
            }
            else
            {
                // 其他状态则将向前速度置空
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(forwardSpeedParameter, 0f);
                _forwardSpeedBeforeSprint =
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter);
                if (_animancerState?.NormalizedTime >= 1f)
                {
                    // 切换到冲刺闲置状态
                    SprintState = SprintIdleState;
                }
            }
        }


        private void HandleMoveInput()
        {
            // 如果处于急停动画或转身动画则忽略玩家输入
            if (CheckBrakeOrTurn())
            {
                // 抬脚设置为空
                _liftUpFoot = LiftUpNoneFoot;
                return;
            }

            var parameters = PlayerCharacter.PlayerParameters;

            // 如果玩家停止冲刺键输入就根据当前帧输入转为对应状态
            if (!parameters.isSprintInFrame)
            {
                // 如果当前帧有输入转为奔跑状态，否则转为默认状态
                if (parameters.playerInputRawValueInFrame.magnitude != 0)
                {
                    Parent.SwitchState(moveStateName, true);
                }
                else
                {
                    Parent.SwitchToDefault();
                }

                return;
            }

            // 如果玩家当前帧无移动输入
            if (parameters.playerInputRawValueInFrame.magnitude == 0)
            {
                // 如果当前状态是冲刺状态就累计无输入时间
                if (SprintState == SprintingState)
                {
                    // 累计无输入时间，如果无输入时间超过预设时间就代表玩家急停，设置冲刺急停状态
                    if (_brakeAccumulateTimes >= brakeAvailableTimes)
                    {
                        Brake();
                    }

                    _brakeAccumulateTimes += FixedDeltaTime;
                }
                else
                {
                    // 当前状态是转身或急停就切换到默认状态
                    Parent.SwitchToDefault();
                }

                return;
            }

            _brakeAccumulateTimes = 0f;

            // 计算偏差角度，规定顺时针为正值，逆时针为负值
            var inputCross = Vector3.Cross(PlayerCharacter.transform.forward,
                parameters.playerInputMovementInFrame);
            var playerMovementInputOffsetAngle = inputCross.y > 0
                ? Vector3.Angle(PlayerCharacter.transform.forward,
                    parameters.playerInputMovementInFrame)
                : -Vector3.Angle(PlayerCharacter.transform.forward,
                    parameters.playerInputMovementInFrame);

            // 判断能否满足180转身，如果满足就设置冲刺转身状态，否则就是单纯地冲刺
            if (Mathf.Abs(playerMovementInputOffsetAngle) >= PlayerCharacter.PlayerCommonConfigSO.turn180StartAngle)
            {
                if (_turnAccumulateTimes >= turnAvailableTimes)
                {
                    Turn();
                }

                _turnAccumulateTimes += FixedDeltaTime;
            }
            else
            {
                _turnAccumulateTimes = 0f;
                Sprint();
            }

            bool CheckBrakeOrTurn()
            {
                if (SprintState == SprintBrakeState)
                {
                    DebugUtil.Log("冲刺急停中忽略移动输入");
                    return true;
                }

                if (SprintState == SprintTurnState)
                {
                    DebugUtil.Log("冲刺转身中忽略移动输入");
                    return true;
                }

                return false;
            }

            void Brake()
            {
                SprintState = SprintBrakeState;
            }

            void Turn()
            {
                SprintState = SprintTurnState;
            }

            void Sprint()
            {
                // 每帧使用球形差值旋转角色（旋转方向最终值为玩家输入方向）
                PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(parameters.playerInputMovementInFrame),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenUnlock
                ));

                SprintState = SprintingState;
            }
        }
    }
}