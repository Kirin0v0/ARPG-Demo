using System.Collections;
using Camera;
using Character;
using Character.Brain;
using Character.Ability;
using Events;
using Features.Game;
using Framework.Common.Debug;
using Humanoid;
using Humanoid.Weapon.Data;
using Inputs;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Player.StateMachine.Dead;
using Player.StateMachine.Locomotion;
using Player.StateMachine.Skill;
using Sirenix.OdinInspector;
using Skill.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Player.Brain
{
    public class PlayerCharacterBrain : CharacterBrain
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;

        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private ICameraModel _cameraModel;
        [Inject] private IGameModel _gameModel;
        
        [Title("障碍物设置")] [SerializeField] private float maxVaultAnimationDistance = 1f;
        [SerializeField] private float maxLowClimbAnimationDistance = 1f;
        [SerializeField] private float maxHighClimbAnimationDistance = 1f;
        [SerializeField] private float maxHangAirborneDistance = 0.2f;

        [SerializeField, MinMaxSlider(0f, 1.5f)]
        private Vector2 hangAirborneHeightRange = new(0f, 1.5f);

        [Title("状态切换")] [SerializeField] private string skillStateName;

        private Transform _cameraTransform;

        private bool _heavyAttackPerformed;

        [Title("输入缓存")] [SerializeField] private float inputBufferTimeout = 0.3f;
        [SerializeField] private bool onlyLastInputPerformed = true;
        [SerializeField] private bool debugInputBuffer = false;
        private readonly InputBuffer _inputBuffer = new();
        private bool _startInputBuffer = false;
        private bool _airborneLastTick = false;

        protected override void OnBrainInit()
        {
            _cameraTransform = UnityEngine.Camera.main.transform;

            if (Owner.WeaponAbility)
            {
                Owner.WeaponAbility.OnWeaponBarChanged += OnWeaponBarChanged;
            }

            _playerInputManager.RegisterActionPerformed(InputConstants.HeavyAttack,
                HandleHeavyAttackPerformed);
            _playerInputManager.RegisterActionCanceled(InputConstants.HeavyAttack,
                HandleHeavyAttackCanceled);

            _playerInputManager.RegisterActionPerformed(InputConstants.Sprint, HandleSprintPerformed);
            _playerInputManager.RegisterActionCanceled(InputConstants.Sprint, HandleSprintCancelled);

            if (Owner.SkillAbility)
            {
                Owner.SkillAbility.OnSkillReleased += OnSkillReleased;
                Owner.SkillAbility.OnSkillStopped += OnSkillFinished;
                Owner.SkillAbility.OnSkillCompleted += OnSkillFinished;
            }

            _inputBuffer.Init(_playerInputManager.PlayerInput, inputBufferTimeout,
                onlyLastInputPerformed);

            // 监听目标选择事件
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(GameEvents.SelectTarget,
                OnTargetSelected);

            _airborneLastTick = Owner.Parameters.Airborne;
        }

        /// <summary>
        /// 玩家角色大脑的渲染帧负责玩家的输入以及时响应玩家输入的业务数据更新
        /// </summary>
        /// <param name="deltaTime"></param>
        protected override void OnRenderThoughtsUpdated(float deltaTime)
        {
            // 更新角色参数
            UpdateParameters();
            // 检查是否允许悬挂
            CheckWhetherAllowHang();
            // 计算障碍物信息
            CalculateObstacleIdea();

            // 如果允许输入响应，则处理输入相关
            if (Owner.Parameters.control.allowInputReaction)
            {
                // 每帧执行输入缓存
                _inputBuffer.Tick(deltaTime);
                // 这里只获取每帧渲染时玩家的输入，不涉及具体逻辑数据
                GetPlayerInput();
                // 处理交互输入
                HandlePlayerInteract();
                CalculatePlayerInput();
            }

            return;

            void UpdateParameters()
            {
                // 更新数据
                Owner.PlayerParameters.cameraLockData = _cameraModel.GetLock().Value;
                Owner.PlayerParameters.evadeAttackCountdown =
                    Mathf.Max(Owner.PlayerParameters.evadeAttackCountdown - deltaTime, 0f);

                // 清除按键输入
                Owner.PlayerParameters.isEquipOrUnequipInFrame = false;
                Owner.PlayerParameters.isInteractInFrame = false;
                Owner.PlayerParameters.isJumpOrVaultOrClimbInFrame = false;
                Owner.PlayerParameters.isEvadeInFrame = false;
                Owner.PlayerParameters.isEnterDefendInFrame = false;
                Owner.PlayerParameters.isDefendingInFrame = false;
                Owner.PlayerParameters.isAttackInFrame = false;
                Owner.PlayerParameters.isHeavyAttackInFrame = false;

                // 清除移动输入
                Owner.PlayerParameters.playerInputRawValueInFrame = Vector2.zero;
                Owner.PlayerParameters.playerInputMovementInFrame = Vector3.zero;
                Owner.PlayerParameters.playerInputCharacterMovementInFrame = Vector3.zero;
            }

            void GetPlayerInput()
            {
                if (_playerInputManager.WasPerformedThisFrame(InputConstants.EquipOrUnequip))
                {
                    Owner.PlayerParameters.isEquipOrUnequipInFrame = true;
                }

                if (_playerInputManager.WasPerformedThisFrame(InputConstants.Interact))
                {
                    Owner.PlayerParameters.isInteractInFrame = true;
                }

                // 玩家连招输入采用每帧玩家输入以及预输入混合
                if (_playerInputManager.WasPerformedThisFrame(InputConstants.Jump) ||
                    _inputBuffer.WasPerformedThisFrame(InputConstants.Jump))
                {
                    Owner.PlayerParameters.isJumpOrVaultOrClimbInFrame = true;
                }

                if (_playerInputManager.WasPerformedThisFrame(InputConstants.Evade) ||
                    _inputBuffer.WasPerformedThisFrame(InputConstants.Evade))
                {
                    Owner.PlayerParameters.isEvadeInFrame = true;
                }

                if (_playerInputManager.WasPerformedThisFrame(InputConstants.Defend) ||
                    _inputBuffer.WasPerformedThisFrame(InputConstants.Defend))
                {
                    Owner.PlayerParameters.isEnterDefendInFrame = true;
                }

                if (_playerInputManager.IsPressed(InputConstants.Defend))
                {
                    Owner.PlayerParameters.isDefendingInFrame = true;
                }

                if (_playerInputManager.WasPerformedThisFrame(InputConstants.Attack) ||
                    _inputBuffer.WasPerformedThisFrame(InputConstants.Attack))
                {
                    Owner.PlayerParameters.isAttackInFrame = true;
                }

                if (_heavyAttackPerformed || _inputBuffer.WasPerformedThisFrame(InputConstants.HeavyAttack))
                {
                    Owner.PlayerParameters.isHeavyAttackInFrame = true;
                }

                if (debugInputBuffer)
                {
                    if (_inputBuffer.WasPerformedThisFrame(InputConstants.Jump))
                    {
                        DebugUtil.LogOrange("预输入: 跳跃");
                    }

                    if (_inputBuffer.WasPerformedThisFrame(InputConstants.Evade))
                    {
                        DebugUtil.LogOrange("预输入: 翻滚");
                    }

                    if (_inputBuffer.WasPerformedThisFrame(InputConstants.Defend))
                    {
                        DebugUtil.LogOrange("预输入: 防御");
                    }

                    if (_inputBuffer.WasPerformedThisFrame(InputConstants.Attack))
                    {
                        DebugUtil.LogOrange("预输入: 攻击");
                    }

                    if (_inputBuffer.WasPerformedThisFrame(InputConstants.HeavyAttack))
                    {
                        DebugUtil.LogOrange("预输入: 重击");
                    }
                }
            }

            void HandlePlayerInteract()
            {
                if (Owner.PlayerParameters.isInteractInFrame)
                {
                    Owner.InteractAbility.Interact();
                }
            }

            void CalculatePlayerInput()
            {
                // 玩家操作输入
                var moveInputAction = _playerInputManager.GetInputAction(InputConstants.Move);
                Owner.PlayerParameters.playerInputRawValueInFrame = moveInputAction == null
                    ? Vector2.zero
                    : moveInputAction.ReadValue<Vector2>();

                // 计算摄像机本地坐标系正前和正右向量在玩家XZ轴平面的投影向量 
                var cameraForwardProjection =
                    new Vector3(_cameraTransform.forward.x, 0, _cameraTransform.forward.z).normalized;
                var cameraRightProjection =
                    new Vector3(_cameraTransform.right.x, 0, _cameraTransform.right.z).normalized;

                // 根据摄像机坐标系重新计算输入方向，目的是保持W方向始终与摄像机到画面中心方向一致
                // 计算公式是 玩家输入的相机坐标系的方向（世界坐标系） = 玩家输入Y轴数值x摄像机本地坐标系正前投影向量 + 玩家输入X轴数值x摄像机本地坐标系正右投影向量
                Owner.PlayerParameters.playerInputMovementInFrame =
                    cameraForwardProjection * Owner.PlayerParameters.playerInputRawValueInFrame.y +
                    cameraRightProjection * Owner.PlayerParameters.playerInputRawValueInFrame.x;

                // 将玩家输入转为角色本地坐标系
                Owner.PlayerParameters.playerInputCharacterMovementInFrame =
                    transform.InverseTransformVector(Owner.PlayerParameters.playerInputMovementInFrame);
            }

            void CalculateObstacleIdea()
            {
                if (Owner.PlayerParameters.obstacleData == null)
                {
                    return;
                }

                var obstacleData = Owner.PlayerParameters.obstacleData;
                Owner.PlayerParameters.obstacleActionIdea = PlayerObstacleActionIdea.None;

                // 如果进入悬挂状态在本次空中流程后就不允许悬挂
                if (Owner.StateMachine.CurrentState is PlayerActionHangState)
                {
                    Owner.PlayerParameters.allowHangInThisAirborneProcess = false;
                }

                if (obstacleData.exist)
                {
                    if (Owner.Parameters.Airborne) // 在空中只考虑悬挂动作
                    {
                        // 如果不是空中动作就不考虑了，避免其他Y轴位移动作衔接悬挂
                        if (Owner.StateMachine.CurrentState is not PlayerLocomotionAirborneState)
                        {
                            return;
                        }

                        // 如果曾经悬挂过就不能再悬挂
                        if (!Owner.PlayerParameters.allowHangInThisAirborneProcess)
                        {
                            return;
                        }

                        var heightOffset = obstacleData.peak.y - Owner.transform.position.y;
                        if (obstacleData is { hasPeak: true, allowPass: true } &&
                            obstacleData.distance <= maxHangAirborneDistance &&
                            heightOffset >= hangAirborneHeightRange.x * Owner.CharacterController.height &&
                            heightOffset <= hangAirborneHeightRange.y * Owner.CharacterController.height)
                        {
                            Owner.PlayerParameters.obstacleActionIdea = PlayerObstacleActionIdea.Hang;
                            return;
                        }
                    }
                    else // 地面上可以考虑除悬挂外其他动作
                    {
                        if (obstacleData.height == ObstacleHeightType.Low &&
                            obstacleData.thickness != ObstacleThicknessType.Thick &&
                            obstacleData.distance <= maxVaultAnimationDistance &&
                            obstacleData.allowPass)
                        {
                            Owner.PlayerParameters.obstacleActionIdea = PlayerObstacleActionIdea.Vault;
                            return;
                        }

                        if ((obstacleData.height == ObstacleHeightType.Low ||
                             obstacleData.height == ObstacleHeightType.Medium) &&
                            obstacleData.width != ObstacleWidthType.OverNarrow &&
                            obstacleData.thickness != ObstacleThicknessType.OverThin &&
                            obstacleData.distance <= maxLowClimbAnimationDistance &&
                            obstacleData.allowPass)
                        {
                            Owner.PlayerParameters.obstacleActionIdea = PlayerObstacleActionIdea.LowClimb;
                            return;
                        }

                        if (obstacleData.height == ObstacleHeightType.High &&
                            obstacleData.width != ObstacleWidthType.OverNarrow &&
                            obstacleData.thickness != ObstacleThicknessType.OverThin &&
                            obstacleData.distance <= maxHighClimbAnimationDistance &&
                            obstacleData.allowPass)
                        {
                            Owner.PlayerParameters.obstacleActionIdea = PlayerObstacleActionIdea.HighClimb;
                            return;
                        }
                    }
                }
            }

            void CheckWhetherAllowHang()
            {
                var inTransition = _airborneLastTick != Owner.Parameters.Airborne;
                // 处于空中的状态过渡则重新设置允许悬挂，处于地面就不允许悬挂，其他空中时间看状态处理
                if (Owner.Parameters.Airborne && inTransition)
                {
                    Owner.PlayerParameters.allowHangInThisAirborneProcess = true;
                }
                else if (!Owner.Parameters.Airborne)
                {
                    Owner.PlayerParameters.allowHangInThisAirborneProcess = false;
                }

                _airborneLastTick = Owner.Parameters.Airborne;
            }
        }

        /// <summary>
        /// 玩家角色大脑的逻辑帧负责固定间隔的业务数据更新，与当前输入无强相关或弱相关
        /// </summary>
        /// <param name="fixedDeltaTime"></param>
        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
        }

        private void OnDrawGizmosSelected()
        {
            if (!Owner)
            {
                return;
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward);
            Gizmos.DrawRay(transform.position, transform.right);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Owner.PlayerParameters.playerInputMovementInFrame.normalized);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Owner.Parameters.movementInFrame.normalized);

            if (Owner.PlayerParameters.obstacleData?.exist == true)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(Owner.PlayerParameters.obstacleData.peak, 0.1f);
            }
        }

        protected override void OnBrainDestroy()
        {
            if (Owner.WeaponAbility)
            {
                Owner.WeaponAbility.OnWeaponBarChanged -= OnWeaponBarChanged;
            }

            _playerInputManager.UnregisterActionPerformed(InputConstants.HeavyAttack,
                HandleHeavyAttackPerformed);
            _playerInputManager.UnregisterActionCanceled(InputConstants.HeavyAttack,
                HandleHeavyAttackCanceled);

            _playerInputManager.UnregisterActionPerformed(InputConstants.Sprint, HandleSprintPerformed);
            _playerInputManager.UnregisterActionCanceled(InputConstants.Sprint, HandleSprintCancelled);

            if (Owner.SkillAbility)
            {
                Owner.SkillAbility.OnSkillReleased -= OnSkillReleased;
                Owner.SkillAbility.OnSkillStopped -= OnSkillFinished;
                Owner.SkillAbility.OnSkillCompleted -= OnSkillFinished;
            }

            _inputBuffer.Destroy();

            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(GameEvents.SelectTarget,
                OnTargetSelected);
        }

        public override void HandleAnimatorMove(Animator animator)
        {
            // 这里直接重写函数，不复用父类函数
            if (Owner.StateMachine is IPlayerState playerStateMachine)
            {
                // 如果当前状态控制了根节点运动，则等待其计算完成后再处理动画数据，否则直接采用动画数据
                if (playerStateMachine.ControlRootMotionBySelf())
                {
                    var rootMotionDelta = playerStateMachine.CalculateRootMotionDelta(animator);
                    if (rootMotionDelta.deltaPosition != null)
                    {
                        // 如果使用物理模拟，则使用物理模拟的移动方法
                        if (Owner.GravityAbility)
                        {
                            Owner.MovementAbility?.SwitchType(rootMotionDelta.useCharacterController
                                ? CharacterMovementType.CharacterController
                                : CharacterMovementType.Transform);
                            Owner.MovementAbility?.Move(
                                Owner.GravityAbility.ProcessSlopeMovement(rootMotionDelta.deltaPosition.Value),
                                true
                            );
                        }
                        else
                        {
                            Owner.MovementAbility?.SwitchType(rootMotionDelta.useCharacterController
                                ? CharacterMovementType.CharacterController
                                : CharacterMovementType.Transform);
                            Owner.MovementAbility?.Move(rootMotionDelta.deltaPosition.Value, true);
                        }
                    }

                    if (rootMotionDelta.deltaRotation != null)
                    {
                        Owner.MovementAbility?.Rotate(rootMotionDelta.deltaRotation.Value, true);
                    }
                }
                else
                {
                    Owner.MovementAbility?.SwitchType(CharacterMovementType.CharacterController);
                    Owner.MovementAbility?.Move(animator.deltaPosition, true);
                    Owner.MovementAbility?.Rotate(animator.deltaRotation, true);
                }
            }
        }

        public override void HandleAnimatorIK(Animator animator)
        {
            // 这里直接重写函数，不复用父类函数
            if (Owner.StateMachine is IPlayerState playerStateMachine)
            {
                playerStateMachine.HandleAnimatorIK(animator);
            }
        }

        public void StartInputBuffer()
        {
            if (_startInputBuffer)
            {
                return;
            }

            if (debugInputBuffer)
            {
                DebugUtil.LogOrange("开始输入缓存");
            }

            _startInputBuffer = true;
            _inputBuffer.Register(InputConstants.Jump);
            _inputBuffer.Register(InputConstants.Evade);
            _inputBuffer.Register(InputConstants.Defend);
            _inputBuffer.Register(InputConstants.Attack);
            _inputBuffer.Register(InputConstants.HeavyAttack);
        }

        public void StopInputBuffer()
        {
            if (!_startInputBuffer)
            {
                return;
            }

            if (debugInputBuffer)
            {
                DebugUtil.LogOrange("停止输入缓存");
            }

            _startInputBuffer = false;
            _inputBuffer.Unregister(InputConstants.Jump);
            _inputBuffer.Unregister(InputConstants.Evade);
            _inputBuffer.Unregister(InputConstants.Defend);
            _inputBuffer.Unregister(InputConstants.Attack);
            _inputBuffer.Unregister(InputConstants.HeavyAttack);

            StartCoroutine(ClearInputBuffer());
        }

        private IEnumerator ClearInputBuffer()
        {
            yield return 0;

            if (debugInputBuffer)
            {
                DebugUtil.LogOrange("清空输入缓存");
            }

            _inputBuffer.Clear();
        }

        private void OnWeaponBarChanged(HumanoidCharacterObject player)
        {
            // // 如果武器已装备就尝试去切换到卸下装备状态
            // if (Owner.HumanoidParameters.WeaponEquipped)
            // {
            //     // 如果没能成功进入状态就手动卸下全部装备
            //     if (!Owner.StateMachine.SwitchState(unequipState, true))
            //     {
            //         Owner.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.LeftHand);
            //         Owner.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.RightHand);
            //         Owner.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.TwoHands);
            //     }
            // }

            // 直接卸下武器
            Owner.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.LeftHand);
            Owner.WeaponAbility.UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition.RightHand);
        }

        private void HandleHeavyAttackPerformed(InputAction.CallbackContext callbackContext)
        {
            _heavyAttackPerformed = true;
        }

        private void HandleHeavyAttackCanceled(InputAction.CallbackContext callbackContext)
        {
            _heavyAttackPerformed = false;
        }

        private void HandleSprintPerformed(InputAction.CallbackContext callbackContext)
        {
            Owner.PlayerParameters.isSprintInFrame = true;
        }

        private void HandleSprintCancelled(InputAction.CallbackContext callbackContext)
        {
            Owner.PlayerParameters.isSprintInFrame = false;
        }

        private void OnSkillReleased(SkillReleaseInfo skillReleaseInfo)
        {
            // 如果此时角色处于死亡或复活状态，则不打断状态
            if (Owner.StateMachine.CurrentState is PlayerDeadState ||
                Owner.StateMachine.CurrentState is PlayerActionGetupState)
            {
                return;
            }

            Owner.StateMachine.SwitchState(skillStateName, true);
        }

        private void OnSkillFinished(SkillReleaseInfo skillReleaseInfo)
        {
            // 如果此时角色处于死亡或复活状态，则不打断状态
            if (Owner.StateMachine.CurrentState is PlayerDeadState ||
                Owner.StateMachine.CurrentState is PlayerActionGetupState)
            {
                return;
            }

            Owner.StateMachine.SwitchToDefault();
        }

        private void OnTargetSelected(CharacterObject target)
        {
            // 这里直接设置玩家面向目标，越过角色运动能力
            var look = target.transform.position - Owner.transform.position;
            Owner.transform.rotation = Quaternion.LookRotation(new Vector3(look.x, 0, look.z));
        }
    }
}