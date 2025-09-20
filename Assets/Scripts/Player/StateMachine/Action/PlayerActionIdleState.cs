using Animancer;
using Camera;
using Camera.Data;
using Character;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Action;
using Player.StateMachine.Attack;
using Player.StateMachine.Base;
using Player.StateMachine.Defence;
using Player.StateMachine.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Player.StateMachine.Action
{
    public class PlayerActionIdleState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset idleTransition;

        [Title("状态切换")] [SerializeField] private string turnStateName;
        [SerializeField] private string moveStateName;
        [SerializeField] private string sprintStateName;
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

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 如果处于空中且成功切换成空中状态就不继续后续逻辑 
            if (PlayerCharacter.Parameters.Airborne && Parent.SwitchState(airborneStateName, true))
            {
                return;
            }

            PlayerCharacter.AnimationAbility.SwitchBase(idleTransition);
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "闲置动作", guiStyle);
            }
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
            if (PlayerCharacter.PlayerParameters.isAttackInFrame || PlayerCharacter.PlayerParameters.isHeavyAttackInFrame)
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
            if (_cameraModel.GetScene().Value.Scene == CameraScene.Normal && parameters.cameraLockData.@lock && parameters.cameraLockData.lockTarget)
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
                // 计算偏差角度，规定顺时针为正值，逆时针为负值
                var inputCross = Vector3.Cross(PlayerCharacter.transform.forward,
                    parameters.playerInputMovementInFrame);
                var playerMovementInputOffsetAngle = inputCross.y > 0
                    ? Vector3.Angle(PlayerCharacter.transform.forward,
                        parameters.playerInputMovementInFrame)
                    : -Vector3.Angle(PlayerCharacter.transform.forward,
                        parameters.playerInputMovementInFrame);

                // // 判断是否执行转身状态的前置条件: 未按下冲刺键、处于非锁定
                // if (!parameters.isSprintInFrame && !parameters.cameraLockData.@lock)
                // {
                //     if (AllowTurn(playerMovementInputOffsetAngle))
                //     {
                //         // 如果执行转身状态则不能继续后续操作
                //         if (Parent.SwitchState(turnState, true))
                //         {
                //             return;
                //         }
                //     }
                // }

                // 切换至冲刺/奔跑状态
                if (parameters.isSprintInFrame)
                {
                    Parent.SwitchState(sprintStateName, true);
                }
                else
                {
                    Parent.SwitchState(moveStateName, true);
                }
            }
            
            return;

            bool AllowTurn(float playerMovementInputOffsetAngle)
            {
                // 判断能否满足180转身，再判断能否满足90转身
                return Mathf.Abs(playerMovementInputOffsetAngle) >=
                       PlayerCharacter.PlayerCommonConfigSO.turn180StartAngle ||
                       Mathf.Abs(playerMovementInputOffsetAngle) >=
                       PlayerCharacter.PlayerCommonConfigSO.turn90StartAngle;
            }
        }
    }
}