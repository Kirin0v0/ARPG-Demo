using System.Collections.Generic;
using System.Linq;
using Animancer;
using Combo;
using Combo.Graph;
using Framework.Common.Blackboard;
using Framework.Common.StateMachine;
using Humanoid;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Player.StateMachine.Defence;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Player.StateMachine.Attack
{
    public class PlayerAttackState : PlayerState, IPlayerStateWeapon, IPlayerStateLocomotion
    {
        [Title("连招图执行器")] [SerializeField] private ComboGraphExecutor comboGraphExecutor;

        [Title("空中参数")] [SerializeField] protected StringAsset forwardSpeedParameter;
        [SerializeField] protected StringAsset lateralSpeedParameter;
        [SerializeField] private float maxVerticalSpeedParameter = 2;
        [SerializeField] private float minVerticalSpeedParameter = -2;
        [SerializeField] private StringAsset verticalSpeedParameter;
        [SerializeField] private float extraFallSpeed = 10f;

        [Title("状态切换")] [SerializeField] private string equipStateName;
        [SerializeField] private string jumpStateName;
        [SerializeField] private string evadeStateName;
        [SerializeField] private string defenceStateName;
        [SerializeField] private float attackCooldown = 0.1f;

        [Title("调试")] [SerializeField] private bool debug;

        private Vector3 _horizontalPlaneSpeedBeforeFall;

        private readonly HashSet<PlayerStateTransition> _comboTransitions = new();

        private float _lastAttackTime = 0f;

        private bool NeedToSwitchToEquipState => !PlayerCharacter.WeaponAbility.AggressiveWeaponSlot.Equipped;

        public bool OnlyWeapon => false;
        public bool OnlyNoWeapon => false;

        public bool ForwardLocomotion => NeedToSwitchToEquipState;
        public bool LateralLocomotion => NeedToSwitchToEquipState;

        private ComboGraphParameters Parameters => new ComboGraphParameters
        {
            playerCharacter = PlayerCharacter,
            onComboPlay = HandleComboPlay,
        };

        protected override void OnInit()
        {
            base.OnInit();
            if (PlayerCharacter.WeaponAbility)
            {
                PlayerCharacter.WeaponAbility.OnWeaponBarChanged += HandleWeaponBarChanged;
            }
        }

        public override bool AllowEnter(IState currentState)
        {
            if (!base.AllowEnter(currentState))
            {
                return false;
            }

            // 过滤无攻击武器或攻击武器无配置连招图
            if (PlayerCharacter.WeaponAbility.AggressiveWeaponSlot == null ||
                !comboGraphExecutor || !comboGraphExecutor.Graph)
            {
                return false;
            }

            // 过滤在攻击武器装备时不满足进入连招图的条件
            if (PlayerCharacter.WeaponAbility.AggressiveWeaponSlot.Equipped)
            {
                // 更新图黑板
                UpdateBlackboard(comboGraphExecutor.Graph.blackboard);
                // 判断是否可以进入图
                var allowEnter = AllowEnterGraph(comboGraphExecutor.Graph);
                // 复位图黑板
                ResetBlackboard(comboGraphExecutor.Graph.blackboard);

                if (!allowEnter)
                {
                    return false;
                }
            }

            return true;

            bool AllowEnterGraph(ComboGraph graph)
            {
                if (!graph.entry || Time.time - _lastAttackTime < attackCooldown)
                {
                    return false;
                }

                var entryTransitions = graph.transitions.Where(transition => transition.from == graph.entry)
                    .ToList();
                var transition = entryTransitions.Find(transition => transition.Transit());
                return transition != null;
            }
        }

        protected override List<PlayerStateTransition> GetAllowEnterTransitions(IState currentState)
        {
            // 如果装备了攻击武器且连招图存在就允许获取过渡，每次获取过渡都要重新获取一次入口连招并展示其过渡条件
            if (PlayerCharacter.WeaponAbility.AggressiveWeaponSlot is { Equipped: true } &&
                comboGraphExecutor.Graph)
            {
                toShowGotoTransitions.Clear();
                // 更新图黑板
                UpdateBlackboard(comboGraphExecutor.Graph.blackboard);
                // 获取当前黑板下的过渡条件
                toShowGotoTransitions.AddRange(
                    ComboTipList2PlayerStateTransitionList(comboGraphExecutor.Graph.GetEntryTips(true))
                );
                // 复位图黑板
                ResetBlackboard(comboGraphExecutor.Graph.blackboard);
                return toShowGotoTransitions;
            }

            return new List<PlayerStateTransition>();
        }

        protected override void OnEnter(IState previousState)
        {
            base.OnEnter(previousState);

            // 如果进入时处于空中，计算下落水平位移速度
            if (PlayerCharacter.Parameters.Airborne)
            {
                var forwardsSpeed =
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter);
                var lateralSpeed =
                    PlayerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(lateralSpeedParameter);
                _horizontalPlaneSpeedBeforeFall =
                    PlayerCharacter.transform.TransformVector(new Vector3(lateralSpeed, 0, forwardsSpeed));
            }
            else
            {
                _horizontalPlaneSpeedBeforeFall = Vector3.zero;
            }

            // 进入攻击时设置标识符
            PlayerCharacter.PlayerParameters.inAttack = true;

            // 如果没装备武器就直接切换到武器装备状态去装备武器
            if (NeedToSwitchToEquipState)
            {
                if (Parent.SwitchState(equipStateName, true))
                {
                    return;
                }

                Parent.SwitchToDefault();
                return;
            }

            // 初始化当前连招的过渡
            _comboTransitions.Clear();
            // 进入时立刻更新图黑板
            comboGraphExecutor.UseBlackboard(UpdateBlackboard);
            // 进入时立刻执行连招图
            ExecuteComboGraph(0);
            // 执行后立刻将闪避攻击倒计时设置为零，并清除黑板数据
            PlayerCharacter.PlayerParameters.evadeAttackCountdown = 0f;
            comboGraphExecutor.graphTemplate.blackboard.SetBoolParameter("evadeToAttack", false);
        }

        protected override void OnRenderTick(float deltaTime)
        {
            base.OnRenderTick(deltaTime);
            // 空中卸下则需要与动画同步竖直速度
            PlayerCharacter.Parameters.verticalSpeed -= extraFallSpeed * deltaTime;
            if (PlayerCharacter.Parameters.Airborne)
            {
                // 设置竖直速度
                PlayerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
                    Mathf.Clamp(PlayerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
                        maxVerticalSpeedParameter));
            }

            // 每帧更新图黑板
            comboGraphExecutor.UseBlackboard(UpdateBlackboard);
            // 执行连招图
            ExecuteComboGraph(deltaTime);
        }

        protected override void OnLogicTick(float fixedDeltaTime)
        {
            base.OnLogicTick(fixedDeltaTime);
            // 更新当前连招的过渡提示
            _comboTransitions.ForEach(ShowTransition);
            // 记录上次攻击时间
            _lastAttackTime = Time.time;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (debug)
            {
                var guiStyle = new GUIStyle();
                guiStyle.fontSize = 34;
                GUI.Label(new Rect(0, 0, 100, 100), "攻击状态", guiStyle);
            }
        }

        protected override void OnExit(IState nextState)
        {
            base.OnExit(nextState);

            // 清除动作层动画
            PlayerCharacter.AnimationAbility?.ClearAction();

            // 清除当前连招的过渡
            _comboTransitions.Clear();
            // 退出时打断执行器，避免执行器正在执行时被强制切换状态
            comboGraphExecutor.Abort(Parameters);

            // 退出攻击状态时重新设置姿势，本质上是重新设置动画过渡库
            PlayerCharacter.SetPose(PlayerCharacter.HumanoidParameters.pose);

            // 退出攻击时设置标识符
            PlayerCharacter.PlayerParameters.inAttack = false;
        }

        protected override void OnClear()
        {
            base.OnClear();
            if (PlayerCharacter.WeaponAbility)
            {
                PlayerCharacter.WeaponAbility.OnWeaponBarChanged -= HandleWeaponBarChanged;
            }
        }

        public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            if (PlayerCharacter.Parameters.Airborne)
            {
                return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime,
                    animator.deltaRotation,
                    true);
            }

            return base.CalculateRootMotionDelta(animator);
        }

        private void HandleWeaponBarChanged(HumanoidCharacterObject character)
        {
            if (character.WeaponAbility)
            {
                // 获取最新的攻击连招图
                var newComboGraph = character.WeaponAbility.AggressiveWeaponSlot?.Data.Attack.attackAbility.comboGraph;
                // 判断与先前执行器的连招图原型是否一致，不一致则重新执行替换流程
                if (newComboGraph != comboGraphExecutor.graphTemplate)
                {
                    // 打断执行器
                    comboGraphExecutor.Abort(Parameters);
                    // 销毁执行器
                    comboGraphExecutor.Destroy();
                    // 设置连招图执行器的图文件
                    comboGraphExecutor.graphTemplate = newComboGraph;
                    // 重新初始化执行器
                    comboGraphExecutor.Init();
                }
            }
        }

        private void HandleComboPlay(ComboConfig comboConfig, List<ComboTip> comboTips)
        {
            ShowStateName(comboConfig.Name);
            _comboTransitions.Clear();
            _comboTransitions.AddRange(ComboTipList2PlayerStateTransitionList(comboTips));
        }

        private void UpdateBlackboard(Blackboard blackboard)
        {
            blackboard.SetBoolParameter("commonAttack",
                PlayerCharacter.PlayerParameters.isAttackInFrame);
            blackboard.SetBoolParameter("heavyAttack",
                PlayerCharacter.PlayerParameters.isHeavyAttackInFrame);
            blackboard.SetBoolParameter("airborne",
                PlayerCharacter.Parameters.Airborne);
            blackboard.SetBoolParameter("hit", false);
            blackboard.SetBoolParameter("evadeToAttack",
                PlayerCharacter.PlayerParameters.evadeAttackCountdown > 0);
            blackboard.SetBoolParameter("toJump",
                PlayerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame);
            blackboard.SetBoolParameter("toEvade",
                PlayerCharacter.PlayerParameters.isEvadeInFrame);
            blackboard.SetBoolParameter("toDefend",
                PlayerCharacter.PlayerParameters.isEnterDefendInFrame);
        }

        private void ResetBlackboard(Blackboard blackboard)
        {
            blackboard.SetBoolParameter("commonAttack", false);
            blackboard.SetBoolParameter("heavyAttack", false);
            blackboard.SetBoolParameter("airborne", false);
            blackboard.SetBoolParameter("hit", false);
            blackboard.SetBoolParameter("evadeToAttack", false);
            blackboard.SetBoolParameter("toJump", false);
            blackboard.SetBoolParameter("toEvade", false);
            blackboard.SetBoolParameter("toDefend", false);
        }

        private void ExecuteComboGraph(float deltaTime)
        {
            // 执行连招图帧函数
            var state = comboGraphExecutor.Tick(deltaTime, Parameters);
            // 仅在连招执行完毕时才切换状态
            if (state == ComboGraphState.Finish)
            {
                if (PlayerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame)
                {
                    if (Parent.SwitchState(jumpStateName, true))
                    {
                        return;
                    }
                }

                if (PlayerCharacter.PlayerParameters.isEvadeInFrame)
                {
                    if (Parent.SwitchState(evadeStateName, true))
                    {
                        return;
                    }
                }

                if (PlayerCharacter.PlayerParameters.isEnterDefendInFrame)
                {
                    if (Parent.SwitchState(defenceStateName, true))
                    {
                        return;
                    }
                }

                Parent.SwitchToDefault();
            }
        }

        private List<PlayerStateTransition> ComboTipList2PlayerStateTransitionList(List<ComboTip> comboTips)
        {
            var playerStateTransitions = new List<PlayerStateTransition>();
            comboTips.ForEach(entryTip =>
            {
                var transition = new PlayerStateTransition();
                transition.name = entryTip.ComboConfig.Name;
                transition.operatorTips = entryTip.OperatorTips.ConvertAll(operatorTip =>
                {
                    var transitionOperatorTip = new PlayerStateTransitionOperatorTip();
                    transitionOperatorTip.operatorType = operatorTip.OperatorType switch
                    {
                        BlackboardConditionOperatorType.And => PlayerStateTransitionOperatorType.And,
                        BlackboardConditionOperatorType.Or => PlayerStateTransitionOperatorType.Or,
                        _ => PlayerStateTransitionOperatorType.And,
                    };
                    transitionOperatorTip.tips = operatorTip.Tips.ConvertAll(tip =>
                    {
                        var transitionTip = new PlayerStateTransitionTip
                        {
                            deviceType = tip.deviceType,
                            prefixText = tip.prefixText,
                            image = tip.image,
                            suffixText = tip.suffixText
                        };
                        return transitionTip;
                    });
                    return transitionOperatorTip;
                });
                transition.commonTransition = false;
                playerStateTransitions.Add(transition);
            });
            return playerStateTransitions;
        }
    }

//     public class PlayerActionAttackState : PlayerActionState
//     {
//         [Title("连招树")] [SerializeField] private ComboTreeExecutor comboTreeExecutor;
//
//         [Title("空中参数")] [SerializeField] protected StringAsset forwardSpeedParameter;
//         [SerializeField] protected StringAsset lateralSpeedParameter;
//         [SerializeField] private float maxVerticalSpeedParameter = 2;
//         [SerializeField] private float minVerticalSpeedParameter = -2;
//         [SerializeField] private StringAsset verticalSpeedParameter;
//
//         [Title("状态切换")] [SerializeField] private PlayerActionEquipState equipState;
//         [SerializeField] private PlayerActionJumpState jumpState;
//         [SerializeField] private PlayerActionEvadeState evadeState;
//         [SerializeField] private PlayerDefenceStartState defenceStartState;
//
//         [Title("调试")] [SerializeField] private bool debug;
//         [SerializeField] private bool pauseApplicationWhenStart;
//
//         private Vector3 _horizontalPlaneSpeedBeforeFall;
//
//         public override bool AllowEnter(IState currentState)
//         {
//             if (!base.AllowEnter(currentState))
//             {
//                 return false;
//             }
//
//             // 过滤无武器能力
//             if (!playerCharacter.WeaponAbility)
//             {
//                 return false;
//             }
//
//             // 过滤在武器装备后仍然没有攻击武器
//             if (playerCharacter.HumanoidParameters.WeaponEquipped &&
//                 !playerCharacter.WeaponAbility.aggressiveWeapon)
//             {
//                 return false;
//             }
//
//             // 过滤未装备武器且无攻击武器或无配置连招树
//             if (!playerCharacter.HumanoidParameters.WeaponEquipped &&
//                 (!playerCharacter.WeaponAbility.weaponSlots.Any(weaponObject =>
//                      weaponObject.Data.Type.supportAttack || !weaponObject.Data.Type.attackComboTree) ||
//                  !comboTreeExecutor))
//             {
//                 return false;
//             }
//
//             // 过滤在攻击武器装备后时连招树没有对应的入口
//             if (playerCharacter.HumanoidParameters.WeaponEquipped && playerCharacter.WeaponAbility.aggressiveWeapon)
//             {
//                 // 初始化连招树执行器
//                 InitComboTreeExecute(playerCharacter.WeaponAbility.aggressiveWeapon.Data.Type.attackComboTree);
//
//                 // 更新树黑板
//                 UpdateBlackboard(comboTreeExecutor.tree.blackboard);
//
//                 // 判断是否可以执行连招树
//                 var allowExecute = comboTreeExecutor.AllowExecute();
//                 if (!allowExecute)
//                 {
//                     return false;
//                 }
//             }
//
//             return true;
//         }
//
//         public override List<PlayerStateTransition> GetTransitions(IState currentState)
//         {
//             // 如果当前装备攻击武器就允许获取过渡，每次获取过渡都要重新获取一次入口连招
//             if (playerCharacter.HumanoidParameters.WeaponEquipped && playerCharacter.WeaponAbility.aggressiveWeapon)
//             {
//                 transitions.Clear();
//                 var comboTree = playerCharacter.WeaponAbility.aggressiveWeapon.Data.Type.attackComboTree.Clone();
//                 UpdateBlackboard(comboTree.blackboard);
//                 transitions.AddRange(ComboTipList2PlayerStateTransitionList((comboTree as ComboTree)?.GetEntryTips(true))
//                 );
//                 return transitions;
//             }
//
//             return new List<PlayerStateTransition>();
//         }
//
//         protected override void OnEnter(IState previousState)
//         {
//             base.OnEnter(previousState);
//
//             // 如果没装备武器就直接切换到武器装备状态去装备武器
//             if (!playerCharacter.HumanoidParameters.WeaponEquipped)
//             {
//                 if (Parent.SwitchState(equipState, true))
//                 {
//                     return;
//                 }
//
//                 Parent.SwitchToDefault();
//                 return;
//             }
//
//             // 如果进入时处于空中，计算下落水平位移速度
//             if (playerCharacter.Parameters.airborne)
//             {
//                 var forwardsSpeed = playerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(forwardSpeedParameter);
//                 var lateralSpeed = playerCharacter.AnimationAbility.Animancer.Parameters.GetFloat(lateralSpeedParameter);
//                 _horizontalPlaneSpeedBeforeFall =
//                     playerCharacter.transform.TransformVector(new Vector3(lateralSpeed, 0, forwardsSpeed));
//             }
//             else
//             {
//                 _horizontalPlaneSpeedBeforeFall = Vector3.zero;
//             }
//
//             // 初始化连招树执行器
//             InitComboTreeExecute(playerCharacter.WeaponAbility.aggressiveWeapon.Data.Type.attackComboTree);
//
//             // 更新树黑板
//             UpdateBlackboard(comboTreeExecutor.tree.blackboard);
//
//             // 进入后即执行连招树第一帧
//             ExecuteComboTree();
//
// #if UNITY_EDITOR
//
//             if (pauseApplicationWhenStart)
//             {
//                 EditorApplication.isPaused = true;
//             }
//
// #endif
//
//             // 执行后立刻将闪避攻击倒计时设置为零，并清除黑板数据
//             playerCharacter.PlayerParameters.evadeAttackCountdown = 0f;
//             comboTreeExecutor.tree.blackboard.SetBoolParameter("evadeToAttack", false);
//         }
//
//         protected override void OnTick(float deltaTime)
//         {
//             base.OnTick(deltaTime);
//             // 空中卸下则需要同步竖直速度
//             if (playerCharacter.Parameters.airborne)
//             {
//                 // 设置竖直速度
//                 playerCharacter.AnimationAbility.Animancer.Parameters.SetValue(verticalSpeedParameter,
//                     Mathf.Clamp(playerCharacter.Parameters.verticalSpeed, minVerticalSpeedParameter,
//                         maxVerticalSpeedParameter));
//             }
//
//             // 每帧更新树黑板
//             UpdateBlackboard(comboTreeExecutor.tree.blackboard);
//
//             // 执行连招树
//             ExecuteComboTree();
//         }
//
//         public override void ShowStateGUI()
//         {
//             base.ShowStateGUI();
//
//             if (debug)
//             {
//                 var guiStyle = new GUIStyle();
//                 guiStyle.fontSize = 34;
//                 GUI.Label(new Rect(0, 0, 100, 100), "攻击状态", guiStyle);
//             }
//         }
//
//         protected override void OnExit(IState nextState)
//         {
//             base.OnExit(nextState);
//
//             // 退出攻击状态时重新设置姿势，本质上是重新设置动画过渡库
//             playerCharacter.SetPose(playerCharacter.HumanoidParameters.pose);
//         }
//
//         public override (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
//             CalculateRootMotionDelta(Animator animator)
//         {
//             if (playerCharacter.Parameters.airborne)
//             {
//                 return (animator.deltaPosition + _horizontalPlaneSpeedBeforeFall * DeltaTime,
//                     animator.deltaRotation,
//                     true);
//             }
//
//             return base.CalculateRootMotionDelta(animator);
//         }
//
//         public void HandleComboPlay(ComboConfig comboConfig, List<ComboTip> comboTips)
//         {
//             ShowStateName(comboConfig.Name);
//             ComboTipList2PlayerStateTransitionList(comboTips).ForEach(ShowTransition);
//         }
//
//         private void InitComboTreeExecute(ComboTree comboTree)
//         {
//             // 设置连招树执行器的树文件并初始化
//             comboTreeExecutor.tree = comboTree;
//             comboTreeExecutor.Init();
//         }
//
//         private void UpdateBlackboard(Blackboard blackboard)
//         {
//             blackboard.SetBoolParameter("jump",
//                 playerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame);
//             blackboard.SetBoolParameter("evade",
//                 playerCharacter.PlayerParameters.isEvadeInFrame);
//             blackboard.SetBoolParameter("defend",
//                 playerCharacter.PlayerParameters.isDefendInFrame);
//             blackboard.SetBoolParameter("commonAttack",
//                 playerCharacter.PlayerParameters.isAttackInFrame);
//             blackboard.SetBoolParameter("heavyAttack",
//                 playerCharacter.PlayerParameters.isHeavyAttackInFrame);
//             blackboard.SetBoolParameter("hit", false);
//             blackboard.SetBoolParameter("airborne",
//                 playerCharacter.Parameters.airborne);
//             blackboard.SetBoolParameter("evadeToAttack",
//                 playerCharacter.PlayerParameters.evadeAttackCountdown > 0);
//         }
//
//         private void ExecuteComboTree()
//         {
//             // 判断树执行情况并切换状态
//             var nodeState = comboTreeExecutor.Tick(DeltaTime);
//             if (nodeState == NodeState.Success || nodeState == NodeState.Failure)
//             {
//                 // DebugUtil.LogOrange($"连招树执行完毕，状态为: {nodeState}");
//                 if (playerCharacter.PlayerParameters.isJumpOrVaultOrClimbInFrame)
//                 {
//                     if (Parent.SwitchState(jumpState, true))
//                     {
//                         return;
//                     }
//                 }
//
//                 if (playerCharacter.PlayerParameters.isEvadeInFrame)
//                 {
//                     if (Parent.SwitchState(evadeState, true))
//                     {
//                         return;
//                     }
//                 }
//
//                 if (playerCharacter.PlayerParameters.isDefendInFrame)
//                 {
//                     if (Parent.SwitchState(defenceStartState, true))
//                     {
//                         return;
//                     }
//                 }
//
//                 Parent.SwitchToDefault();
//             }
//         }
//
//         private List<PlayerStateTransition> ComboTipList2PlayerStateTransitionList(List<ComboTip> comboTips)
//         {
//             var playerStateTransitions = new List<PlayerStateTransition>();
//             comboTips.ForEach(entryTip =>
//             {
//                 var transition = new PlayerStateTransition();
//                 transition.name = entryTip.ComboConfig.name;
//                 transition.operatorTips = entryTip.OperatorTips.ConvertAll(operatorTip =>
//                 {
//                     var transitionOperatorTip = new PlayerStateTransitionOperatorTip();
//                     transitionOperatorTip.operatorType = operatorTip.OperatorType switch
//                     {
//                         BlackboardConditionOperatorType.And => PlayerStateTransitionOperatorType.And,
//                         BlackboardConditionOperatorType.Or => PlayerStateTransitionOperatorType.Or,
//                         _ => PlayerStateTransitionOperatorType.And,
//                     };
//                     transitionOperatorTip.tips = operatorTip.Tips.ConvertAll(tip =>
//                     {
//                         var transitionTip = new PlayerStateTransitionTip();
//                         transitionTip.deviceType = tip.deviceType;
//                         transitionTip.text = tip.tipText;
//                         transitionTip.icon = tip.tipImage;
//                         return transitionTip;
//                     });
//                     return transitionOperatorTip;
//                 });
//                 playerStateTransitions.Add(transition);
//             });
//             return playerStateTransitions;
//         }
//     }
}