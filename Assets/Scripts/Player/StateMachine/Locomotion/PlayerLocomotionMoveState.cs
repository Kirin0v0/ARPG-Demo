using System;
using System.Collections.Generic;
using Animancer;
using Animancer.Units;
using Camera;
using Camera.Data;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Action;
using Player.StateMachine.Attack;
using Player.StateMachine.Defence;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using VContainer;

namespace Player.StateMachine.Locomotion
{
    public class PlayerLocomotionMoveState : PlayerLocomotionState
    {
        [Title("动画")] [SerializeField] private StringAsset moveTransition;
        [SerializeField] private float maxSpeed = 3.4f;
        [SerializeField, Seconds] private float parameterSmoothTime = 0.15f;
        [SerializeField, EventNames] private StringAsset footPutDownEvent;

        [Title("脚步音效")] [SerializeField] private AudioClipRandomizer defaultAudioClipRandomizer;
        [SerializeField] [Range(0f, 1f)] private float defaultAudioVolume = 1f;
        [SerializeField] private List<PlayerFootstepsAudioConfigData> audioConfigs = new();
        [SerializeField, MinValue(0f)] private float minPlayAudioInterval = 0.3f;

        [Title("状态切换")] [SerializeField] private string sprintStateName;
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

        [Inject] private ICameraModel _cameraModel;

        private PlayerFootstepsPlayer _footstepsPlayer;
        private SmoothedVector2Parameter _smoothedVector2Parameter;
        private AnimancerState _animancerState;

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

            // 设置移动动画参数并播放动画
            _smoothedVector2Parameter = new SmoothedVector2Parameter(PlayerCharacter.AnimationAbility.Animancer,
                lateralSpeedParameter,
                forwardSpeedParameter,
                parameterSmoothTime);
            _animancerState = PlayerCharacter.AnimationAbility.SwitchBase(moveTransition);
            // 设置动画事件
            _animancerState.SharedEvents.AddCallbacks(footPutDownEvent, HandleFootPutDownEvent);
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

            HandleMoveInput();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "移动状态", guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            // 删除动画事件
            _animancerState.SharedEvents.RemoveCallbacks(footPutDownEvent, HandleFootPutDownEvent);
            _animancerState = null;
            // 清空动画参数
            _smoothedVector2Parameter.Dispose();
            _smoothedVector2Parameter = null;

            base.OnExit(nextState);
        }

        protected override void OnClear()
        {
            base.OnClear();
            _footstepsPlayer = null;
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
        protected bool HandleEvade()
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
        protected bool HandleDefence()
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

        private void HandleMoveInput()
        {
            var parameters = PlayerCharacter.PlayerParameters;

            // 在相机场景处于通常场景且锁定状态下，角色会缓慢向锁定目标方向旋转
            if (_cameraModel.GetScene().Value.Scene == CameraScene.Normal && parameters.cameraLockData.@lock &&
                parameters.cameraLockData.lockTarget)
            {
                // 计算锁定目标方向向量与角色本地坐标系Z轴正方向向量的偏差向量投影
                var targetDirection = parameters.cameraLockData.lockTarget.transform.position -
                                      PlayerCharacter.transform.position;
                var targetDirectionProjection = new Vector3(targetDirection.x, 0, targetDirection.z);
                // 每帧使用球形差值旋转角色（旋转方向最终值为看向目标方向）
                PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(targetDirectionProjection),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenLock
                ));
            }

            // 判断当前玩家是否输入移动
            if (parameters.playerInputRawValueInFrame.magnitude != 0)
            {
                // 判断是否冲刺
                if (parameters.isSprintInFrame)
                {
                    // 切换至冲刺状态
                    Parent.SwitchState(sprintStateName, true);
                }
                else
                {
                    // 普通移动，根据锁定和非锁定执行不同移动逻辑
                    if (parameters.cameraLockData.@lock)
                    {
                        MoveWhenLock();
                    }
                    else
                    {
                        MoveWhenUnlock();
                    }
                }
            }
            else
            {
                // 等待动画速度下降
                _smoothedVector2Parameter.TargetValue = new Vector2(0, 0);

                // 在动画速度为0时切换至默认状态，否则仍然保持原状态
                if (_smoothedVector2Parameter.X.CurrentValue == 0 && _smoothedVector2Parameter.Y.CurrentValue == 0)
                {
                    Parent.SwitchToDefault();
                }
            }

            void MoveWhenLock()
            {
                _smoothedVector2Parameter.TargetValue = new(
                    maxSpeed * parameters.playerInputCharacterMovementInFrame.x,
                    maxSpeed * parameters.playerInputCharacterMovementInFrame.z
                );
            }

            void MoveWhenUnlock()
            {
                // 每帧使用球形差值旋转角色（旋转方向最终值为玩家输入方向）
                PlayerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(parameters.playerInputMovementInFrame),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenUnlock
                ));

                _smoothedVector2Parameter.TargetValue =
                    new(0, Mathf.Abs(maxSpeed * parameters.playerInputCharacterMovementInFrame.z));
            }
        }
    }
}