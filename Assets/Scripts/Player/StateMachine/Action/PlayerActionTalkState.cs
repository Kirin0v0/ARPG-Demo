using System.Collections;
using Animancer;
using Character;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using IState = Framework.Common.StateMachine.IState;

namespace Player.StateMachine.Action
{
    public class PlayerActionTalkState : PlayerActionState
    {
        [Title("动画")] [SerializeField] private StringAsset talkTransition;
        
        [Title("转向")] [SerializeField] private float turnDuration = 0.3f;
        
        [Title("调试")] [SerializeField] private bool debug;

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);
            PlayerCharacter.AnimationAbility.SwitchBase(talkTransition);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "谈话动作", guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);
            StopAllCoroutines();
        }

        public void StartFacingTarget(CharacterObject target)
        {
            StopAllCoroutines();
            StartCoroutine(KeepFaceToTarget(target));
        }

        private IEnumerator KeepFaceToTarget(CharacterObject target)
        {
            var time = 0f;
            var origin = PlayerCharacter.Parameters.rotation;
            while (time <= turnDuration)
            {
                yield return new WaitForNextFrameUnit();
                if (!target || target.Parameters.dead)
                {
                    break;
                }

                time += Time.deltaTime;
                TurnToTarget();
            }

            TurnToTarget();

            void TurnToTarget()
            {
                var direction = target.Parameters.position - PlayerCharacter.Parameters.position;
                var currentFacingDirection = Quaternion.Slerp(origin,
                    Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)),
                    time / turnDuration);
                PlayerCharacter.MovementAbility?.RotateTo(currentFacingDirection);
            }
        }
    }
}