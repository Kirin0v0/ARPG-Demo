using System;
using System.Collections.Generic;
using Buff.Data;
using Character.SO;
using CollideDetection;
using Common;
using Damage;
using Damage.Data;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Character.Ability
{
    public class CharacterGravityAbility : BaseCharacterOptionalAbility
    {
        [Title("重力相关")] [SerializeField] private bool disableGravity = false;

        [Title("检测相关")] [SerializeField, MinValue(0f)]
        private float slopeAngleThreshold = 5f;

        [HideIf("disableGravity")] [SerializeField, MinValue(0f)] [InfoBox("激活重力且在空中时会检测是否处于其他角色头顶，是则会提供向上速度二段跳避免落在头顶")]
        private float airborneJumpInitialSpeed = 7f;

        [SerializeField, MinValue(0f)] [InfoBox("在地面检测空中的连续检测时间，默认为0，即当帧检测当帧转换")]
        private float toAirborneTime = 0f;

        [SerializeField, MinValue(0f)] [InfoBox("在空中检测地面的连续检测时间，默认为0，即当帧检测当帧转换")]
        private float toGroundTime = 0f;

        [HideIf("disableGravity")] [Title("坠落相关")] [SerializeField]
        private float extraFallSpeed = 10f;

        [HideIf("disableGravity")] [SerializeField]
        private bool openFallTimeoutDead = true;

        private bool _openFallTimeoutDeadRuntime = true; // 运行时动态开启/关闭坠落死亡
        public bool FallTimeoutDead => openFallTimeoutDead && _openFallTimeoutDeadRuntime && !disableGravity;

        [ShowIf("@openFallTimeoutDead && !disableGravity", true, true), SerializeField]
        private float fallTimeout = 2f;

        [Title("调试")] [SerializeField] private bool debug;
        [SerializeField] private bool debugAirborneJump;

        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public bool StandOnGround { private set; get; }

        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public UnityEngine.Collider GroundCollider { private set; get; }

        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public Vector3 GroundNormal { private set; get; }

        /// <summary>
        /// 这里规定斜坡属于地面的一种类型，具体根据底部碰撞体的法线向量夹角决定
        /// </summary>
        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public bool StandOnSlope { private set; get; }

        /// <summary>
        /// 上一次掉落高度，如果此时正在空中则会清除上次数据
        /// </summary>
        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public float AirborneDropHeight { private set; get; }

        /// <summary>
        /// 空中/地面状态时间
        /// </summary>
        [BoxGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public float Time { private set; get; }

        [Inject] private DamageManager _damageManager;
        [Inject] private GameManager _gameManager;
        [Inject] private CollideDetectionManager _collideDetectionManager;

        private float _peakPositionY; // 当前空中流程的最高点位置 
        private float _fallTime; // 掉落时间

        private float AirborneCheckRadius => Owner.CharacterController.radius * 0.8f;
        private float OtherHeadCheckRadius => Owner.CharacterController.radius;

        protected override void OnInit()
        {
            base.OnInit();
            Owner.Parameters.touchGround = true;
            Owner.Parameters.verticalSpeed = 0f;

            StandOnGround = true;
            GroundCollider = null;
            StandOnSlope = false;
            GroundNormal = Vector3.zero;
            AirborneDropHeight = 0f;
            Time = 0f;
        }

        /// <summary>
        /// 每个渲染帧检查玩家是否处于空中以及计算竖直速度
        /// </summary>
        /// <param name="deltaTime"></param>
        public void UpdateGravity(float deltaTime)
        {
            CheckAirborneOrGround();
            CalculateVerticalSpeed();

            return;

            void CheckAirborneOrGround()
            {
                // 清空帧数据
                var touchGroundLastTick = StandOnGround;
                StandOnGround = true;
                GroundCollider = null;
                StandOnSlope = false;
                GroundNormal = Vector3.zero;

                // 根据保留参数检测对应值
                if (touchGroundLastTick) // 处于地面
                {
                    // 如果竖直速度大于0则说明角色目前正在跳跃中，虽然接触地面，但仍视为空中
                    if (Owner.Parameters.verticalSpeed > 0)
                    {
                        StandOnGround = false;
                        Time = toAirborneTime;
                        _peakPositionY = Owner.Parameters.position.y;
                        if (debug)
                        {
                            DebugUtil.LogCyan(
                                $"角色({Owner.Parameters.DebugName})处于空中：由竖直速度{Owner.Parameters.verticalSpeed}得出");
                        }
                    }
                    else
                    {
                        // 先检测是否站在地面上，这里用圆球碰撞检测，从角色上方到脚底检测，适合地面高精度判断
                        Physics.SphereCast(
                            Owner.Parameters.position + Vector3.up * AirborneCheckRadius,
                            AirborneCheckRadius,
                            Vector3.down,
                            out var hit,
                            2 * Owner.CharacterController.skinWidth,
                            GlobalRuleSingletonConfigSO.Instance.groundLayer
                        );
                        StandOnGround = hit.collider != null;
                        GroundCollider = StandOnGround ? hit.collider : null;
                        GroundNormal = StandOnGround ? hit.normal : Vector3.zero;

                        // 根据地面和空中执行不同逻辑
                        if (StandOnGround)
                        {
                            // 处于地面则继续判断是否处于斜坡上，根据法线与垂直向上向量夹角判断当前地面是否倾斜
                            var angle = Vector3.Angle(hit.normal, Vector3.up);
                            StandOnSlope = angle >= slopeAngleThreshold;
                            Time += deltaTime;
                            if (debug)
                            {
                                var text = StandOnSlope ? "斜坡" : "地面";
                                DebugUtil.LogCyan(
                                    $"角色({Owner.Parameters.DebugName})处于{text}({hit.collider.name})：由地面碰撞检测得出，持续时间为{Time}秒");
                            }
                        }
                        else
                        {
                            // 如果不处于地面，就认为处于空中
                            Time = 0f;
                            _peakPositionY = Owner.Parameters.position.y;
                            if (debug)
                            {
                                DebugUtil.LogCyan(
                                    $"角色({Owner.Parameters.DebugName})处于空中：由地面碰撞检测得出");
                            }
                        }
                    }
                }
                else // 处于空中
                {
                    // 记录本次空中的最高高度位置
                    if (Owner.Parameters.verticalSpeed > 0f)
                    {
                        _peakPositionY = Owner.Parameters.position.y;
                    }
                    else
                    {
                        _peakPositionY = Mathf.Max(_peakPositionY, Owner.Parameters.position.y);
                    }

                    // 如果空中速度大于0，认为还在上升阶段，不用检测此时是否接触地面
                    if (Owner.Parameters.verticalSpeed > 0)
                    {
                        StandOnGround = false;
                        Time += deltaTime;
                        if (debug)
                        {
                            DebugUtil.LogCyan(
                                $"角色({Owner.Parameters.DebugName})处于空中：由竖直速度{Owner.Parameters.verticalSpeed}得出，持续时间为{Time}秒");
                        }
                    }
                    else
                    {
                        // 检测是否接触地面，这里用圆球碰撞检测，从角色上方到脚底皮肤宽度外加一段角色控制器步高，适合空中提前感知底部信息，提前进行动作变化
                        Physics.SphereCast(
                            Owner.Parameters.position + Vector3.up * AirborneCheckRadius,
                            AirborneCheckRadius,
                            Vector3.down,
                            out var hit,
                            2 * Owner.CharacterController.skinWidth + Owner.CharacterController.stepOffset,
                            GlobalRuleSingletonConfigSO.Instance.groundLayer
                        );
                        StandOnGround = hit.collider != null;
                        GroundCollider = StandOnGround ? hit.collider : null;

                        // 根据地面和空中执行不同逻辑
                        if (!StandOnGround)
                        {
                            // 处于空中就不再检测
                            Time += deltaTime;
                            if (debug)
                            {
                                DebugUtil.LogCyan($"角色({Owner.Parameters.DebugName})处于空中：由地面碰撞检测得出，持续时间为{Time}秒");
                            }
                        }
                        else
                        {
                            // 处于地面则根据法线与垂直向上向量夹角判断当前地面是否倾斜
                            var angle = Vector3.Angle(hit.normal, Vector3.up);
                            StandOnSlope = angle >= slopeAngleThreshold;
                            Time = toGroundTime;
                            if (debug)
                            {
                                var text = StandOnSlope ? "斜坡" : "地面";
                                DebugUtil.LogCyan(
                                    $"角色({Owner.Parameters.DebugName})处于{text}({hit.collider.name})：由地面碰撞检测得出");
                            }
                        }
                    }
                }

                // 根据检测结果更新角色参数
                if (!Owner.Parameters.touchGround && StandOnGround && Time >= toGroundTime)
                {
                    AirborneDropHeight = Mathf.Max(_peakPositionY - Owner.Parameters.position.y, 0f);
                    if (debug)
                    {
                        DebugUtil.LogCyan($"角色({Owner.Parameters.DebugName})掉落高度：{AirborneDropHeight}");
                    }

                    Owner.Parameters.touchGround = true;
                }

                if (Owner.Parameters.touchGround && !StandOnGround && Time >= toAirborneTime)
                {
                    Owner.Parameters.touchGround = false;
                }
            }

            void CalculateVerticalSpeed()
            {
                if (disableGravity)
                {
                    Owner.Parameters.verticalSpeed = 0f;
                    return;
                }

                if (StandOnGround)
                {
                    // 处于地面就统一设置为重力速度
                    Owner.Parameters.verticalSpeed = -GlobalRuleSingletonConfigSO.Instance.gravity;
                }
                else
                {
                    // 处于空中就计算下落速度
                    if (Owner.Parameters.verticalSpeed < 0) // 判断当前角色在空中是否处于下落阶段，是则额外加上下落速度
                    {
                        Owner.Parameters.verticalSpeed -=
                            (GlobalRuleSingletonConfigSO.Instance.gravity + extraFallSpeed) * deltaTime;
                    }
                    else // 上升阶段仅加上重力
                    {
                        Owner.Parameters.verticalSpeed -= GlobalRuleSingletonConfigSO.Instance.gravity * deltaTime;
                    }
                }
            }
        }

        /// <summary>
        /// 每个渲染帧延后执行竖直移动、解决角色竖直碰撞以及处理坠落死亡
        /// </summary>
        /// <param name="deltaTime"></param>
        public void LateUpdateGravity(float deltaTime)
        {
            ResolveCharacterCollision();
            MoveByGravity();
            HandleFallDead();

            return;

            void ResolveCharacterCollision()
            {
                // 重力失效则不参与角色碰撞的处理
                if (disableGravity)
                {
                    return;
                }

                // 判断当前在空中是否发生碰撞，是则重置其速度，保证其能够再次跳跃，而不会卡在其他角色身上
                if (CheckWhetherCollidedInAirborne(out var other))
                {
                    if (debugAirborneJump)
                    {
                        DebugUtil.LogCyan(
                            $"角色({Owner.Parameters.DebugName})处于其他角色({other.Parameters.DebugName})头顶");
                    }

                    var target = other;
                    Owner.Parameters.verticalSpeed =
                        Mathf.Max(Owner.Parameters.verticalSpeed, airborneJumpInitialSpeed);
                    // 为了防止跳跃时只是竖直跳跃没有任何水平运动，这里给角色添加持续的水平运动
                    var direction = target!.transform.position - Owner.Parameters.position;
                    var movement = Vector3.Dot(Owner.transform.forward, direction) > 0
                        ? -Owner.transform.forward
                        : Owner.transform.forward;
                    Owner.MovementAbility.ContinuousMove(1f, movement, false);
                }
            }

            bool CheckWhetherCollidedInAirborne(out CharacterObject other)
            {
                return _collideDetectionManager.HasCollidedCharacterInAirborne(Owner, out other);
            }

            // bool CheckWhetherIsOnOthersHead(out CharacterObject other)
            // {
            //     other = null;
            //     // 处于地面或者未下降则不继续检测
            //     if (Owner.Parameters.touchGround || Owner.Parameters.verticalSpeed > 0f)
            //     {
            //         return false;
            //     }
            //
            //     Physics.SphereCast(
            //         Owner.Parameters.position + Vector3.up * OtherHeadCheckRadius,
            //         OtherHeadCheckRadius,
            //         Vector3.down,
            //         out var hit,
            //         2 * Owner.CharacterController.skinWidth,
            //         GlobalRuleSingletonConfigSO.Instance.OwnerPhysicsLayer
            //     );
            //     if (hit.collider && hit.collider.gameObject != Owner.gameObject)
            //     {
            //         other = hit.collider.gameObject.GetComponent<CharacterObject>();
            //         return other != null;
            //     }
            //
            //     return false;
            // }
            //
            // bool CheckWhetherIsCollideWithOthersWhenFallingDown(out CharacterObject other)
            // {
            //     other = null;
            //     // 处于地面或者未下降则不继续检测
            //     if (Owner.Parameters.touchGround || Owner.Parameters.verticalSpeed > 0f)
            //     {
            //         return false;
            //     }
            //
            //     var selfCenter = Owner.Parameters.position + (Owner.CharacterController?.center ?? Vector3.zero);
            //     var selfTop = Owner.Parameters.position + (Owner.CharacterController?.height ?? 0f) * Vector3.up;
            //     // 检测移动后自身是否会进入其他角色的碰撞范围(X、Z轴范围），是则比较高度
            //     foreach (var target in _gameManager.Characters)
            //     {
            //         if (target == Owner)
            //         {
            //             continue;
            //         }
            //
            //         var targetCenter = target.Parameters.position +
            //                            (target.CharacterController?.center ?? Vector3.zero);
            //         var targetTop = target.Parameters.position + (target.CharacterController?.height ?? 0f) * Vector3.up;
            //         var targetRadius = target.CharacterController?.radius ?? 0f;
            //         
            //         // 如果发生碰撞，则继续检测
            //         if (_collideDetectionManager.IsTwoCharactersHasCollision(Owner, target))
            //         {
            //             // 精确检测角色是否高于目标角色
            //             if (Owner.Parameters.position.y > target.Parameters.position.y &&
            //                 Owner.Parameters.position.y <= targetTop.y + 0.1f)
            //             {
            //                 other = target;
            //                 return true;
            //             }
            //         }
            //         
            //         // 判断是否进入其他角色XZ轴的碰撞范围
            //         if (MathUtil.IsLessThanDistance(selfCenter, targetCenter,
            //                 targetRadius + GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
            //                 MathUtil.TwoDimensionAxisType.XZ))
            //         {
            //             // 如果当前角色脚底处于特定高度，则认为是发生碰撞
            //             if (Owner.Parameters.position.y > target.Parameters.position.y &&
            //                 Owner.Parameters.position.y <= targetTop.y + 0.1f)
            //             {
            //                 other = target;
            //                 return true;
            //             }
            //         }
            //     }
            //
            //     return false;
            // }

            void MoveByGravity()
            {
                // 判断是否处于斜坡，处于斜坡则不考虑竖直移动，默认无论空中还是地面都会执行竖直移动
                // if (!StandOnSlope && Owner.Parameters.verticalSpeed != 0f)
                if (!StandOnSlope)
                {
                    var verticalMovement = Vector3.up * Owner.Parameters.verticalSpeed * deltaTime;
                    if (debug)
                    {
                        DebugUtil.LogCyan(
                            $"角色({Owner.Parameters.DebugName})在竖直方向速度为{Owner.Parameters.verticalSpeed}，移动了{verticalMovement}距离");
                    }

                    Owner.MovementAbility?.Move(verticalMovement, false);
                }
                else
                {
                    if (debug)
                    {
                        DebugUtil.LogCyan(
                            $"角色({Owner.Parameters.DebugName})在斜坡上，不参与竖直方向移动");
                    }
                }
            }

            void HandleFallDead()
            {
                // 过滤未开启坠落死亡
                if (!FallTimeoutDead)
                {
                    _fallTime = 0f;
                    return;
                }

                // 记录坠落时间
                if (!StandOnGround && Owner.Parameters.verticalSpeed < 0)
                {
                    _fallTime += deltaTime;
                }
                else
                {
                    _fallTime = 0f;
                }

                // 如果坠落时间超时，则判定为角色死亡
                if (FallTimeoutDead && _fallTime >= fallTimeout && !Owner.Parameters.dead)
                {
                    if (debug)
                    {
                        DebugUtil.LogCyan($"角色({Owner.Parameters.DebugName})因坠落超时死亡");
                    }

                    // 添加坠落伤害，走实际伤害流程
                    _damageManager.AddDamage(
                        _gameManager.God,
                        Owner,
                        DamageEnvironmentMethod.Fall,
                        DamageType.TrueDamage,
                        new DamageValue
                        {
                            physics = Owner.Parameters.property.maxHp,
                        },
                        DamageResourceMultiplier.Hp,
                        0f,
                        Owner.transform.up,
                        true
                    );

                    // 清空坠落时间
                    _fallTime = 0f;
                }
            }
        }

        public void SetGravityEnable(bool enable)
        {
            disableGravity = !enable;
        }

        public void OpenFallTimeoutDead()
        {
            _openFallTimeoutDeadRuntime = true;
        }

        public void CloseFallTimeoutDead()
        {
            _openFallTimeoutDeadRuntime = false;
        }

        public Vector3 ProcessSlopeMovement(Vector3 movement)
        {
            return StandOnSlope
                ? Vector3.ProjectOnPlane(movement, GroundNormal)
                : movement;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Owner)
            {
                return;
            }

            // 绘制大致检测范围
            Gizmos.color = Color.magenta;
            if (Owner.Parameters.touchGround)
            {
                DrawGroundCheck();
            }
            else
            {
                DrawAirborneCheck();
            }

            // 绘制法线向量
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Owner.Parameters.position, GroundNormal);

            return;

            void DrawGroundCheck()
            {
                Gizmos.DrawWireCube(
                    (2 * Owner.CharacterController.skinWidth) / 2f * Vector3.down,
                    new Vector3(2 * AirborneCheckRadius,
                        2 * Owner.CharacterController.skinWidth,
                        2 * AirborneCheckRadius)
                );
            }

            void DrawAirborneCheck()
            {
                Gizmos.DrawWireCube(
                    Owner.Parameters.position +
                    (2 * Owner.CharacterController.skinWidth + Owner.CharacterController.stepOffset) / 2f *
                    Vector3.down,
                    new Vector3(2 * AirborneCheckRadius,
                        2 * Owner.CharacterController.skinWidth + Owner.CharacterController.stepOffset,
                        2 * AirborneCheckRadius)
                );
            }
        }
    }
}