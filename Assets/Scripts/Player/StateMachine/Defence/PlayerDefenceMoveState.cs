using System.Collections.Generic;
using Animancer;
using Camera;
using Camera.Data;
using Framework.Common.Audio;
using Framework.Common.StateMachine;
using Humanoid.Weapon.Data;
using Package.Data;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Defence
{
    public class PlayerDefenceMoveState : PlayerDefenceState, IPlayerStateLocomotion, IPlayerStateLocomotionParameter
    {
        [Title("脚步音效")] [SerializeField] private AudioClipRandomizer defaultAudioClipRandomizer;
        [SerializeField] [Range(0f, 1f)] private float defaultAudioVolume = 1f;
        [SerializeField] private List<PlayerFootstepsAudioConfigData> audioConfigs = new();
        [SerializeField, MinValue(0f)] private float minPlayAudioInterval = 0.3f;

        [Title("状态切换")] [SerializeField] private string idleStateName;

        [Title("调试")] [SerializeField] private bool debug;

        [Inject] private ICameraModel _cameraModel;

        private PlayerFootstepsPlayer _footstepsPlayer;
        private SmoothedVector2Parameter _smoothedVector2Parameter;
        private float _maxSpeed;
        private AnimancerState _animancerState;

        private PackageWeaponData WeaponData => PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;

        public override bool InitialState => false;
        public override bool DamageResistant => true;

        public bool ForwardLocomotion => true;
        public bool LateralLocomotion => true;

        public StringAsset ForwardSpeedParameter =>
            WeaponData.Defence.defenceAbility.moveParameter.forwardSpeedParameter;

        public StringAsset LateralSpeedParameter =>
            WeaponData.Defence.defenceAbility.moveParameter.lateralSpeedParameter;

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

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data.Defence.defenceAbility.AllowMove;
        }

        protected override void EnterAfterCheck()
        {
            // 获取移动参数并设置最大速度
            _smoothedVector2Parameter = new SmoothedVector2Parameter(
                PlayerCharacter.AnimationAbility.Animancer,
                WeaponData.Defence.defenceAbility.moveParameter.lateralSpeedParameter,
                WeaponData.Defence.defenceAbility.moveParameter.forwardSpeedParameter,
                WeaponData.Defence.defenceAbility.moveParameter.parameterSmoothTime
            );
            _maxSpeed = WeaponData.Defence.defenceAbility.moveParameter.maxSpeed;
            // 播放防御移动动画
            _animancerState =
                PlayerCharacter.AnimationAbility.PlayAction(WeaponData.Defence.defenceAbility.moveParameter.transition);
            // 设置动画事件
            _animancerState.SharedEvents.AddCallbacks(WeaponData.Defence.defenceAbility.moveParameter.footPutDownEvent,
                HandleFootPutDownEvent);
        }

        protected override void UpdateAfterCheck()
        {
            HandleMoveInput();
        }

        protected override void OnExit(IState nextState)
        {
            // 清空动画参数
            _smoothedVector2Parameter.Dispose();
            if (_animancerState != null)
            {
                // 删除动画事件
                _animancerState.SharedEvents
                    .RemoveCallbacks(WeaponData.Defence.defenceAbility.moveParameter.footPutDownEvent,
                        HandleFootPutDownEvent);
                // 停止动画
                PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                _animancerState = null;
            }

            base.OnExit(nextState);
        }

        protected override void OnClear()
        {
            base.OnClear();
            _footstepsPlayer = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "防御移动状态", guiStyle);
            }
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
                PlayerCharacter.transform.rotation = Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(targetDirectionProjection),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenLock
                );
            }

            // 判断当前玩家是否输入移动
            if (parameters.playerInputRawValueInFrame.magnitude != 0)
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
            else
            {
                // 等待动画速度下降
                _smoothedVector2Parameter.TargetValue = new Vector2(0, 0);

                // 在允许状态切换且在动画速度为0时切换至防御闲置状态，否则仍然保持原状态
                if (AllowStateChanged && _smoothedVector2Parameter.X.CurrentValue == 0 &&
                    _smoothedVector2Parameter.Y.CurrentValue == 0)
                {
                    if (Parent.SwitchState(idleStateName, true))
                    {
                        return;
                    }

                    Parent.SwitchToDefault();
                }
            }

            return;

            void MoveWhenLock()
            {
                _smoothedVector2Parameter.TargetValue = new(
                    _maxSpeed * parameters.playerInputCharacterMovementInFrame.x,
                    _maxSpeed * parameters.playerInputCharacterMovementInFrame.z
                );
            }

            void MoveWhenUnlock()
            {
                // 每帧使用球形差值旋转角色（旋转方向最终值为玩家输入方向）
                PlayerCharacter.transform.rotation = Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(parameters.playerInputMovementInFrame),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenUnlock
                );

                _smoothedVector2Parameter.TargetValue =
                    new(0, Mathf.Abs(_maxSpeed * parameters.playerInputCharacterMovementInFrame.z));
            }
        }
    }
}