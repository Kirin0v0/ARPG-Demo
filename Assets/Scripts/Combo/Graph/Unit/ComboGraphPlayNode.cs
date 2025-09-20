using System;
using System.Collections.Generic;
using Combo.Blackboard;
using Common;
using Damage;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Core.Attribute;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Combo.Graph.Unit
{
    public class ComboGraphPlayNode : ComboGraphNode
    {
        [Inject] private GameManager _gameManager;
        [Inject] private DamageManager _damageManager;
        [Inject] private AlgorithmManager _algorithmManager;

        [LabelText("节点名称"), InlineButton("SynchronizeNameWithCombo", "同步连招名称")] public string nodeName;

        [Title("连招配置")] public ComboConfig comboConfig;

        [Title("转向配置")] [LabelText("转向策略")] [SerializeField]
        private ComboTurnStrategy comboTurnStrategy = ComboTurnStrategy.TurnToSinglePlayerInput;

        [HideIf("comboTurnStrategy", ComboTurnStrategy.None)] [LabelText("转向阶段")] [SerializeField]
        private ComboTurnStage comboTurnStage = ComboTurnStage.Anticipation;

        [ShowIf("comboTurnStrategy", ComboTurnStrategy.TurnToSinglePlayerInput)] [LabelText("转向最大夹角")] [SerializeField]
        private float comboTurnMaxAngle = 45f;

        [ShowIf("comboTurnStrategy", ComboTurnStrategy.TurnToContinuousPlayerInput)] [LabelText("转向速度")] [SerializeField]
        private float comboTurnSpeed = 5f;

        [HideInInspector] public ComboBlackboard blackboard;

        [Title("调试")] [SerializeField] public bool debug;

        public ComboStage Stage => _comboPlayer?.Stage ?? ComboStage.Idle;

        private Quaternion _comboTurnInitialDirection;
        private Quaternion _comboTurnTargetDirection;

        private float _tickTime;
        private PlayerCharacterObject _playerCharacter;
        private IComboPlay _comboPlayer;
        private readonly List<ComboTip> _comboTips = new();

        protected override void OnEnter(IComboGraphNode from, ComboGraphTransition transition,
            ComboGraphParameters parameters)
        {
            base.OnEnter(from, transition, parameters);

            _tickTime = 0f;
            _playerCharacter = parameters.playerCharacter;

            // 初始化连招播放器并开始连招
            _comboPlayer = new ComboPlayer(
                comboConfig,
                parameters.playerCharacter,
                _gameManager,
                _damageManager,
                _algorithmManager,
                blackboard ? blackboard.AllowCollide : null
            );
            _comboPlayer.OnStageChanged += HandleComboStageChanged;

            // 记录当前连招的提示
            if (Graph)
            {
                _comboTips.AddRange(Graph.GetNextComboTips(this));
            }
            else
            {
                _comboTips.Clear();
            }

            // 开始连招
            _comboPlayer.Start();
            // 执行连招播放回调
            parameters.onComboPlay?.Invoke(comboConfig, _comboTips);
        }

        protected override void OnTick(float deltaTime, ComboGraphParameters parameters)
        {
            base.OnTick(deltaTime, parameters);

            // 执行连招每帧逻辑
            _tickTime += deltaTime;
            _comboPlayer.Tick(deltaTime);

            // 处理指定阶段的连招转向
            switch (Stage)
            {
                case ComboStage.Anticipation when comboTurnStage == ComboTurnStage.Anticipation:
                {
                    HandlePlayerCharacterTurn(deltaTime);
                }
                    break;
                case ComboStage.Judgment when comboTurnStage == ComboTurnStage.Judgment:
                {
                    HandlePlayerCharacterTurn(deltaTime);
                }
                    break;
                case ComboStage.Recovery when comboTurnStage == ComboTurnStage.Recovery:
                {
                    HandlePlayerCharacterTurn(deltaTime);
                }
                    break;
            }

            if (debug)
            {
                DebugUtil.LogGreen($"连招播放节点({nodeName})当前播放累计时间: {_tickTime}");
            }
        }

        protected override void OnExit(IComboGraphNode to, ComboGraphTransition transition,
            ComboGraphParameters parameters)
        {
            base.OnExit(to, transition, parameters);
            _comboPlayer.Stop();
            _comboPlayer.OnStageChanged -= HandleComboStageChanged;
        }

        protected override void OnAbort(ComboGraphParameters parameters)
        {
            base.OnAbort(parameters);
            _comboPlayer.Stop();
            _comboPlayer.OnStageChanged -= HandleComboStageChanged;
        }

        private void HandleComboStageChanged(ComboStage stage)
        {
            switch (stage)
            {
                case ComboStage.Idle:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Idle");
                    }

                    break;
                case ComboStage.Start:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Start");
                    }

                    break;
                case ComboStage.Anticipation:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Anticipation");
                    }

                    if (comboTurnStage == ComboTurnStage.Anticipation)
                    {
                        _comboTurnInitialDirection = _playerCharacter.transform.rotation;
                        // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
                        if (_playerCharacter.PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
                        {
                            _comboTurnTargetDirection = _playerCharacter.transform.rotation;
                        }
                        else
                        {
                            _comboTurnTargetDirection =
                                Quaternion.LookRotation(_playerCharacter.PlayerParameters.playerInputMovementInFrame);
                        }
                    }

                    break;
                case ComboStage.Judgment:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Judgment");
                    }

                    if (comboTurnStage == ComboTurnStage.Judgment)
                    {
                        _comboTurnInitialDirection = _playerCharacter.transform.rotation;
                        // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
                        if (_playerCharacter.PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
                        {
                            _comboTurnTargetDirection = _playerCharacter.transform.rotation;
                        }
                        else
                        {
                            _comboTurnTargetDirection =
                                Quaternion.LookRotation(_playerCharacter.PlayerParameters.playerInputMovementInFrame);
                        }
                    }

                    break;
                case ComboStage.Recovery:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Recovery");
                    }

                    if (comboTurnStage == ComboTurnStage.Recovery)
                    {
                        _comboTurnInitialDirection = _playerCharacter.transform.rotation;
                        // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
                        if (_playerCharacter.PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
                        {
                            _comboTurnTargetDirection = _playerCharacter.transform.rotation;
                        }
                        else
                        {
                            _comboTurnTargetDirection =
                                Quaternion.LookRotation(_playerCharacter.PlayerParameters.playerInputMovementInFrame);
                        }
                    }

                    break;
                case ComboStage.End:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: End");
                    }

                    break;
                case ComboStage.Stop:
                    if (debug)
                    {
                        DebugUtil.LogGreen($"连招播放节点({nodeName})阶段: Stop");
                    }

                    break;
            }

            // 在判定阶段采用预输入，判定阶段后停止预输入
            if (stage == ComboStage.Judgment)
            {
                _playerCharacter.Brain.StartInputBuffer();
            }
            else if (stage > ComboStage.Judgment)
            {
                _playerCharacter.Brain.StopInputBuffer();
            }
        }

        private void HandlePlayerCharacterTurn(float deltaTime)
        {
            switch (comboTurnStrategy)
            {
                case ComboTurnStrategy.TurnToSinglePlayerInput:
                {
                    // 根据最大夹角限制输入方向
                    var angle = Quaternion.Angle(_comboTurnInitialDirection, _comboTurnTargetDirection);
                    if (angle > comboTurnMaxAngle)
                    {
                        _comboTurnTargetDirection = Quaternion.RotateTowards(
                            _comboTurnInitialDirection,
                            _comboTurnTargetDirection,
                            comboTurnMaxAngle
                        );
                    }
                    // 计算整个阶段时间
                    var comboTurnInitialTime = comboTurnStage switch
                    {
                        ComboTurnStage.Anticipation => comboConfig.actionClip.process.anticipationTime,
                        ComboTurnStage.Judgment => comboConfig.actionClip.process.judgmentTime,
                        ComboTurnStage.Recovery => comboConfig.actionClip.process.recoveryTime,
                    };
                    var comboTurnEndTime = comboTurnStage switch
                    {
                        ComboTurnStage.Anticipation => Mathf.Min(comboConfig.actionClip.process.anticipationTime + 0.1f,
                            comboConfig.actionClip.process.judgmentTime),
                        ComboTurnStage.Judgment => Mathf.Min(comboConfig.actionClip.process.judgmentTime + 0.1f,
                            comboConfig.actionClip.process.recoveryTime),
                        ComboTurnStage.Recovery => Mathf.Min(comboConfig.actionClip.process.recoveryTime + 0.1f,
                            comboConfig.actionClip.duration),
                    };
                    // 将玩家向单次输入方向插值转向
                    _playerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
                        _comboTurnInitialDirection,
                        _comboTurnTargetDirection,
                        Mathf.Clamp01((_tickTime - comboTurnInitialTime) / (comboTurnEndTime - comboTurnInitialTime))
                    ));
                }
                    break;
                case ComboTurnStrategy.TurnToContinuousPlayerInput:
                {
                    // 每帧都重新获取玩家输入，实现持续性跟随玩家输入转向
                    _comboTurnInitialDirection = _playerCharacter.transform.rotation;
                    // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
                    if (_playerCharacter.PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
                    {
                        _comboTurnTargetDirection = _playerCharacter.transform.rotation;
                    }
                    else
                    {
                        _comboTurnTargetDirection =
                            Quaternion.LookRotation(_playerCharacter.PlayerParameters.playerInputMovementInFrame);
                    }

                    // 将玩家持续向输入方向插值转向
                    _playerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
                        _comboTurnInitialDirection,
                        _comboTurnTargetDirection,
                        comboTurnSpeed * deltaTime
                    ));
                }
                    break;
            }
        }

        private void SynchronizeNameWithCombo()
        {
            if (comboConfig)
            {
                nodeName = comboConfig.Name;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (String.IsNullOrEmpty(nodeName))
            {
                nodeName = comboConfig?.Name ?? "Undefined";
            }

            // 保证文件名称与节点名称一致
            if (name != nodeName)
            {
                name = nodeName;
            }
        }
#endif
    }
}