using System.Collections.Generic;
using Animancer;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Action
{
    public class PlayerActionHangState : PlayerActionState
    {
        private const int HangUnstartStage = -1;
        private const int HangStartStage = 0;
        private const int HangIdleStage = 1;
        private const int HangUpFirstStage = 2;
        private const int HangUpSecondStage = 3;
        private const int HangDownStage = 4;

        [Title("动画")] [SerializeField] private StringAsset hangStartTransition;
        [SerializeField] private StringAsset hangIdleTransition;
        [SerializeField] private StringAsset hangUpStartTransition;
        [SerializeField] private StringAsset hangUpEndTransition;
        [SerializeField] private StringAsset hangDownTransition;

        [SerializeField] private float leftHandHangOffset = 0.2f;
        [SerializeField] private float leftHandHangTopOffset = 0f;
        [SerializeField] private float rightHandHangOffset = 0.2f;
        [SerializeField] private float rightHandHangTopOffset = 0f;

        [InfoBox("该参数用于对动画的指定时间区域提供额外向上偏移量，最终达到动画结束时角色高度与目标障碍物平面高度一致")] [SerializeField, MinMaxSlider(0f, 1f)]
        private Vector2 hangUpForceNormalizedTimeRange;

        [SerializeField] private float hangUpAnimationHeight = 0.65f;

        [Title("爬上音效")] [SerializeField] private List<PlayerActionAudioConfigData> hangUpAudioConfigs = new();

        [Title("调试")] [SerializeField] private bool debug = false;

        private AnimancerState _hangStartAnimancerState;
        private AnimancerState _hangIdleAnimancerState;
        private AnimancerState _hangUpFirstAnimancerState;
        private AnimancerState _hangUpSecondAnimancerState;
        private AnimancerState _hangDownAnimancerState;

        private Vector3 _obstaclePeak;
        private Vector3 _leftHandTargetPosition;
        private Vector3 _rightHandTargetPosition;
        private float _bodyTopOffset;

        private int _stage = HangUnstartStage;

        private int Stage
        {
            set
            {
                if (_stage == value)
                {
                    return;
                }

                simulateGravity = value == HangDownStage;
                // 执行先前阶段的退出
                switch (_stage)
                {
                    case HangStartStage:
                    {
                        if (_hangStartAnimancerState != null)
                        {
                            PlayerCharacter.AnimationAbility.StopAction(_hangStartAnimancerState);
                            _hangStartAnimancerState.SharedEvents.OnEnd = null;
                            _hangStartAnimancerState = null;
                        }
                    }
                        break;
                    case HangIdleStage:
                    {
                        if (_hangIdleAnimancerState != null)
                        {
                            PlayerCharacter.AnimationAbility.StopAction(_hangIdleAnimancerState);
                            _hangIdleAnimancerState = null;
                        }
                    }
                        break;
                    case HangUpFirstStage:
                    {
                        if (_hangUpFirstAnimancerState != null)
                        {
                            PlayerCharacter.AnimationAbility.StopAction(_hangUpFirstAnimancerState);
                            _hangUpFirstAnimancerState.SharedEvents.OnEnd = null;
                            _hangUpFirstAnimancerState = null;
                        }
                    }
                        break;
                    case HangUpSecondStage:
                    {
                        if (_hangUpSecondAnimancerState != null)
                        {
                            PlayerCharacter.AnimationAbility.StopAction(_hangUpSecondAnimancerState);
                            _hangUpSecondAnimancerState.SharedEvents.OnEnd = null;
                            _hangUpSecondAnimancerState = null;
                        }
                    }
                        break;
                    case HangDownStage:
                    {
                        if (_hangDownAnimancerState != null)
                        {
                            PlayerCharacter.AnimationAbility.StopAction(_hangDownAnimancerState);
                            _hangDownAnimancerState.SharedEvents.OnEnd = null;
                            _hangDownAnimancerState = null;
                        }
                    }
                        break;
                }

                _stage = value;

                // 执行当前阶段的进入
                switch (_stage)
                {
                    case HangStartStage:
                    {
                        // 播放悬挂开始动画，并监听结束回调
                        _hangStartAnimancerState =
                            PlayerCharacter.AnimationAbility.PlayAction(hangStartTransition, true);
                        _hangStartAnimancerState.SharedEvents.OnEnd = OnHangStartEnd;
                    }
                        break;
                    case HangIdleStage:
                    {
                        // 播放悬挂闲置动画
                        _hangIdleAnimancerState = PlayerCharacter.AnimationAbility.PlayAction(hangIdleTransition, true);
                    }
                        break;
                    case HangUpFirstStage:
                    {
                        // 执行爬上动作
                        _hangUpFirstAnimancerState =
                            PlayerCharacter.AnimationAbility.PlayAction(hangUpStartTransition, true);
                        // 设置爬上动作完成回调
                        _hangUpFirstAnimancerState.SharedEvents.OnEnd = OnHangUpFirstPartEnd;
                        // 查找种族对应的爬上音效
                        var audioSetting =
                            hangUpAudioConfigs.Find(setting => setting.race == PlayerCharacter.HumanoidParameters.race);
                        if (audioSetting == null)
                        {
                            Debug.LogError(
                                $"Can't find the audio setting of the race({PlayerCharacter.HumanoidParameters.race})");
                        }
                        else
                        {
                            // 播放对应种族的爬上音效
                            PlayerCharacter.AudioAbility?.PlaySound(audioSetting.audioClipRandomizer.Random(), false,
                                audioSetting.volume);
                        }
                    }
                        break;
                    case HangUpSecondStage:
                    {
                        // 执行爬上后续动作
                        _hangUpSecondAnimancerState =
                            PlayerCharacter.AnimationAbility.PlayAction(hangUpEndTransition, true);
                        _hangUpSecondAnimancerState.SharedEvents.OnEnd = OnHangUpSecondPartEnd;
                    }
                        break;
                    case HangDownStage:
                    {
                        // 执行放下动作
                        _hangDownAnimancerState =
                            PlayerCharacter.AnimationAbility.PlayAction(hangDownTransition, true);
                        _hangDownAnimancerState.SharedEvents.OnEnd = OnHangDownEnd;
                    }
                        break;
                }
            }
            get => _stage;
        }

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.PlayerParameters.obstacleActionIdea == PlayerObstacleActionIdea.Hang;
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            // 保证进入时默认阶段是未开始
            Stage = HangUnstartStage;
            // 获取障碍物数据
            var parameters = PlayerCharacter.PlayerParameters;
            _obstaclePeak = parameters.obstacleData.peak;
            _leftHandTargetPosition =
                _obstaclePeak + Vector3.Cross(-parameters.obstacleData.collideNormal, Vector3.up).normalized *
                leftHandHangOffset +
                leftHandHangTopOffset * Vector3.up;
            _rightHandTargetPosition =
                _obstaclePeak + Vector3.Cross(parameters.obstacleData.collideNormal, Vector3.up).normalized *
                rightHandHangOffset +
                rightHandHangTopOffset * Vector3.up;
            // 关闭重力能力坠落超时死亡
            PlayerCharacter.GravityAbility?.CloseFallTimeoutDead();
            // 开启IK
            PlayerCharacter.AnimationAbility.ApplyActionIK(true);
            // 进入悬挂开始阶段
            Stage = HangStartStage;
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);

            // 仅在悬挂闲置阶段时监听输入
            if (Stage == HangIdleStage)
            {
                switch (PlayerCharacter.PlayerParameters.playerInputRawValueInFrame.y)
                {
                    case > 0:
                    {
                        // 计算爬上高度差
                        _bodyTopOffset =
                            Mathf.Max(
                                (_obstaclePeak.y - PlayerCharacter.Parameters.position.y) - hangUpAnimationHeight +
                                PlayerCharacter.CharacterController.skinWidth,
                                0f);
                        // 进入爬上阶段
                        Stage = HangUpFirstStage;
                    }
                        break;
                    case < 0:
                    {
                        // 进入爬下阶段
                        Stage = HangDownStage;
                    }
                        break;
                }
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            // 保证退出时阶段转为未开始
            Stage = HangUnstartStage;
            // 清空数据
            _obstaclePeak = Vector3.zero;
            _leftHandTargetPosition = Vector3.zero;
            _rightHandTargetPosition = Vector3.zero;
            // 开启重力能力坠落超时死亡
            PlayerCharacter.GravityAbility?.OpenFallTimeoutDead();
            // 关闭IK
            PlayerCharacter.AnimationAbility.ApplyActionIK(false);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (debug)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_obstaclePeak, 0.1f);
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_leftHandTargetPosition, 0.1f);
                Gizmos.DrawSphere(_rightHandTargetPosition, 0.1f);
            }
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), Stage switch
                {
                    HangStartStage => "开始悬挂状态",
                    HangIdleStage => "正在悬挂状态",
                    HangUpFirstStage => "悬挂转向上状态",
                    HangUpSecondStage => "悬挂转向上状态",
                    HangDownStage => "悬挂转向下状态",
                    _ => "悬挂未开始状态"
                }, guiStyle);
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(
                Animator animator)
        {
            switch (Stage)
            {
                // 在指定时间区域内提供额外高度移动值
                case HangUpFirstStage
                    when _hangUpFirstAnimancerState.NormalizedTime >= hangUpForceNormalizedTimeRange.x &&
                         _hangUpFirstAnimancerState.NormalizedTime <= hangUpForceNormalizedTimeRange.y:
                {
                    var deltaForceHeight = _bodyTopOffset * DeltaTime /
                                           (_hangUpFirstAnimancerState.Duration * (hangUpForceNormalizedTimeRange.y -
                                               hangUpForceNormalizedTimeRange.x));
                    var deltaPosition = new Vector3(animator.deltaPosition.x, deltaForceHeight,
                        animator.deltaPosition.z);
                    return (deltaPosition, animator.deltaRotation, false);
                }
                case HangUpFirstStage:
                case HangUpSecondStage:
                    return (animator.deltaPosition, animator.deltaRotation, false);
                default:
                    return base.CalculateRootMotionDelta(animator);
            }
        }

        public override void HandleAnimatorIK(Animator animator)
        {
            switch (Stage)
            {
                case HangStartStage:
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                        Mathf.Clamp01(_hangStartAnimancerState.NormalizedTime));
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, animator.GetIKRotation(AvatarIKGoal.LeftHand));
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand,
                        Mathf.Clamp01(_hangStartAnimancerState.NormalizedTime));
                    animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, animator.GetIKRotation(AvatarIKGoal.RightHand));
                }

                    break;
                case HangIdleStage:
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, animator.GetIKRotation(AvatarIKGoal.LeftHand));
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, animator.GetIKRotation(AvatarIKGoal.RightHand));
                }
                    break;
                case HangUpFirstStage:
                {
                    // 在爬上前半阶段保证双手始终与障碍物平面接触
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, animator.GetIKRotation(AvatarIKGoal.LeftHand));
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, animator.GetIKRotation(AvatarIKGoal.RightHand));
                }
                    break;
                case HangUpSecondStage:
                {
                    // 在爬上后半阶段保证双手慢慢放开障碍物平面
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,
                        1 - _hangUpSecondAnimancerState.NormalizedTime);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, animator.GetIKRotation(AvatarIKGoal.LeftHand));
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand,
                        1 - _hangUpSecondAnimancerState.NormalizedTime);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTargetPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, animator.GetIKRotation(AvatarIKGoal.RightHand));
                }
                    break;
            }
        }

        private void OnHangStartEnd()
        {
            Stage = HangIdleStage;
        }

        private void OnHangUpFirstPartEnd()
        {
            Stage = HangUpSecondStage;
        }

        private void OnHangUpSecondPartEnd()
        {
            Stage = HangUnstartStage;
            Parent.SwitchToDefault();
        }

        private void OnHangDownEnd()
        {
            Stage = HangUnstartStage;
            Parent.SwitchToDefault();
        }
    }
}