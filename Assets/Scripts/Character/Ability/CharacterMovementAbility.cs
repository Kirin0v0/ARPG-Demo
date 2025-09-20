using System;
using System.Collections.Generic;
using CollideDetection;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VContainer;

namespace Character.Ability
{
    public enum CharacterMovementType
    {
        Transform,
        CharacterController,
    }

    public class CharacterMoveTask
    {
        public float Duration;
        public float Time;
        public Vector3 Delta;
        public bool Proactive;
    }

    public delegate (Vector3 newDeltaProactivePosition, Vector3 newDeltaReactivePosition) OnCharacterMoveIntercepted(
        Vector3 oldDeltaProactivePosition, Vector3 oldDeltaReactivePosition);

    public class CharacterMovementAbility : BaseCharacterOptionalAbility
    {
        [Inject] private CollideDetectionManager _collideDetectionManager;

        [SerializeField] private bool debug = false;

        private CharacterMovementType _type = CharacterMovementType.CharacterController;

        // 每渲染帧位移量
        private Vector3 _deltaProactivePositionInFrame;
        private Vector3 _deltaReactivePositionInFrame;

        // 每渲染帧旋转量
        private Quaternion _deltaProactiveRotationInFrame = Quaternion.identity;
        private Quaternion _deltaReactiveRotationInFrame = Quaternion.identity;
        private Quaternion? _targetRotationInFrame = null;

        // 持续位移列表
        private readonly List<CharacterMoveTask> _moveTasks = new();

        public void SwitchType(CharacterMovementType type)
        {
            _type = type;
        }

        /// <summary>
        /// 增量移动函数，移动函数分为主动移动和被动移动，主动移动是指角色主动移动，如自身动画位移等，被动移动是指角色受环境或其他角色影响被动移动，如重力、其他角色技能等。
        /// </summary>
        /// <param name="deltaPosition"></param>
        /// <param name="proactive"></param>
        public void Move(Vector3 deltaPosition, bool proactive)
        {
            if (proactive)
            {
                _deltaProactivePositionInFrame += deltaPosition;
            }
            else
            {
                _deltaReactivePositionInFrame += deltaPosition;
            }
        }

        public void ContinuousMove(float duration, Vector3 deltaPosition, bool proactive)
        {
            _moveTasks.Add(new CharacterMoveTask
            {
                Duration = duration,
                Time = duration,
                Delta = deltaPosition,
                Proactive = proactive
            });
        }

        /// <summary>
        /// 增量旋转函数，旋转函数分为主动旋转和被动旋转，主动旋转是指角色主动旋转，如自身动画旋转等，被动移动是指角色受环境或其他角色影响被动旋转，如其他角色技能等。
        /// </summary>
        /// <param name="deltaRotation"></param>
        /// <param name="proactive"></param>
        public void Rotate(Quaternion deltaRotation, bool proactive)
        {
            if (proactive)
            {
                _deltaProactiveRotationInFrame *= deltaRotation;
            }
            else
            {
                _deltaReactiveRotationInFrame *= deltaRotation;
            }
        }

        /// <summary>
        /// 指定方向旋转函数，仅由主动控制
        /// </summary>
        /// <param name="rotation"></param>
        public void RotateTo(Quaternion rotation)
        {
            _targetRotationInFrame = rotation;
        }

        /// <summary>
        /// 每渲染帧调用的移动处理函数，内部处理主动和被动移动/旋转
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Tick(float deltaTime)
        {
            Owner.Parameters.movementInFrame = Vector3.zero;

            // 计算本帧主动和被动移动量
            var index = 0;
            while (index < _moveTasks.Count)
            {
                var task = _moveTasks[index];
                if (task.Time <= 0)
                {
                    _moveTasks.RemoveAt(index);
                }
                else
                {
                    if (task.Proactive)
                    {
                        _deltaProactivePositionInFrame += task.Delta * deltaTime / task.Duration;
                    }
                    else
                    {
                        _deltaReactivePositionInFrame += task.Delta * deltaTime / task.Duration;
                    }

                    task.Time -= deltaTime;
                    index++;
                }
            }

            // 通过碰撞检测调整移动量
            _collideDetectionManager.LimitCharacterMovement(Owner, ref _deltaProactivePositionInFrame,
                ref _deltaReactivePositionInFrame);

            // 最终进行移动
            switch (_type)
            {
                case CharacterMovementType.Transform:
                    Owner.transform.position += _deltaReactivePositionInFrame;
                    Owner.Parameters.movementInFrame += _deltaReactivePositionInFrame;
                    if (debug)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})Transform被动移动: " +
                                            _deltaReactivePositionInFrame);
                    }

                    // 判断是否允许主动移动
                    if (Owner.Parameters.control.allowMove)
                    {
                        Owner.transform.position += _deltaProactivePositionInFrame;
                        Owner.Parameters.movementInFrame += _deltaProactivePositionInFrame;
                        if (debug)
                        {
                            DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})Transform主动移动: " +
                                                _deltaProactivePositionInFrame);
                        }
                    }

                    break;
                case CharacterMovementType.CharacterController:
                    Owner.CharacterController.Move(_deltaReactivePositionInFrame);
                    Owner.Parameters.movementInFrame += _deltaReactivePositionInFrame;
                    if (debug)
                    {
                        DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})CharacterController被动移动: " +
                                            _deltaReactivePositionInFrame);
                    }

                    // 判断是否允许主动移动
                    if (Owner.Parameters.control.allowMove)
                    {
                        Owner.CharacterController.Move(_deltaProactivePositionInFrame);
                        Owner.Parameters.movementInFrame += _deltaProactivePositionInFrame;
                        if (debug)
                        {
                            DebugUtil.LogOrange($"角色({Owner.Parameters.DebugName})CharacterController主动移动: " +
                                                _deltaProactivePositionInFrame);
                        }
                    }

                    break;
            }

            // 判断是否允许主动旋转并设置旋转量
            if (Owner.Parameters.control.allowRotate)
            {
                if (_targetRotationInFrame.HasValue)
                {
                    Owner.transform.rotation = _targetRotationInFrame.Value;
                }
                else
                {
                    Owner.transform.rotation *= _deltaProactiveRotationInFrame;
                    Owner.transform.rotation *= _deltaReactiveRotationInFrame;
                }
            }
            else
            {
                Owner.transform.rotation *= _deltaReactiveRotationInFrame;
            }

            // 更新角色参数
            Owner.Parameters.position = Owner.transform.position;
            Owner.Parameters.rotation = Owner.transform.rotation;
            Owner.Parameters.forwardAngle =
                Quaternion.Angle(Owner.transform.rotation, Quaternion.Euler(Vector3.up));

            // 重置每帧数据
            _deltaProactivePositionInFrame = Vector3.zero;
            _deltaReactivePositionInFrame = Vector3.zero;
            _deltaProactiveRotationInFrame = Quaternion.identity;
            _deltaReactiveRotationInFrame = Quaternion.identity;
            _targetRotationInFrame = null;
        }
    }
}