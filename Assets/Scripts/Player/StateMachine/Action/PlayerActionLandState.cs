using Animancer;
using Camera.Data;
using Character;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Player.StateMachine.Action
{
    public class PlayerActionLandState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset landTransition;
        [SerializeField] private StringAsset baseTransition;
        [SerializeField] private StringAsset maxGroundClearanceParameter;

        [Title("相机震动")] [SerializeField] private CameraShakeShape shape = CameraShakeShape.Recoil;
        [SerializeField, MinValue(0f)] private float duration = 0.2f;
        [SerializeField, MinValue(0f)] private float force = 1f;
        [SerializeField, MinValue(0f)] private float heightBase = 2f;

        [Title("调试")] [SerializeField] private bool debug;

        private AnimancerState _animancerState;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 设置动画参数
            _animancerState = PlayerCharacter.AnimationAbility.PlayAction(landTransition, true);
            PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(maxGroundClearanceParameter,
                PlayerCharacter.GravityAbility.AirborneDropHeight);

            // 添加相机震动
            var shakeUniformData = new CameraShakeUniformData
            {
                shape = shape,
                duration = duration,
                useDamageDirectionAsVelocity = false,
                defaultVelocity = Vector3.down * Mathf.Log(PlayerCharacter.GravityAbility.AirborneDropHeight,heightBase),
                force = force,
            };
            shakeUniformData.GenerateShake(PlayerCharacter.transform.position);
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            HandleLandEnd();
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            PlayerCharacter.AnimationAbility.StopAction(_animancerState);
            _animancerState = null;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "落地动作", guiStyle);
            }
        }

        private void HandleLandEnd()
        {
            // 落地动画结束就切换到默认状态
            if (_animancerState?.NormalizedTime >= 1f)
            {
                Parent.SwitchToDefault();
            }
        }
    }
}