using Animancer;
using Camera;
using Camera.Data;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Player.StateMachine.Defence
{
    public class PlayerDefenceIdleState : PlayerDefenceState
    {
        [Title("状态切换")] [SerializeField] private string moveStateName;

        [Title("调试")] [SerializeField] private bool debug;

        [Inject] private ICameraModel _cameraModel;

        public override bool InitialState => false;
        public override bool DamageResistant => true;

        private AnimancerState _animancerState;

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            return PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data.Defence.defenceAbility.AllowIdle;
        }

        protected override void EnterAfterCheck()
        {
            // 获取武器数据
            var weaponData = PlayerCharacter.WeaponAbility.DefensiveWeaponSlot.Data;
            // 播放防御闲置动画
            _animancerState =
                PlayerCharacter.AnimationAbility.PlayAction(weaponData.Defence.defenceAbility.idleParameter.transition);
        }

        protected override void UpdateAfterCheck()
        {
            HandleMoveInput();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            if (_animancerState != null)
            {
                // 停止动画
                PlayerCharacter.AnimationAbility.StopAction(_animancerState);
                _animancerState = null;
            }
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "防御闲置状态", guiStyle);
            }
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
                PlayerCharacter.transform.rotation = Quaternion.Slerp(
                    PlayerCharacter.transform.rotation,
                    Quaternion.LookRotation(targetDirectionProjection),
                    FixedDeltaTime * PlayerCharacter.PlayerCommonConfigSO.rotationFactorWhenLock
                );
            }

            // 过滤当前玩家未输入移动
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

                // // 判断是否执行转身状态的前置条件: 处于非锁定
                // if (!parameters.cameraLockData.@lock)
                // {
                //     if (AllowStateChanged && AllowTurn(playerMovementInputOffsetAngle))
                //     {
                //         // 如果执行转身状态则不能继续后续操作
                //         if (Parent.SwitchState(turnState, true))
                //         {
                //             return;
                //         }
                //     }
                // }

                // 切换至移动状态
                if (AllowStateChanged && Parent.SwitchState(moveStateName, true))
                {
                    return;
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