using System;
using System.Linq;
using Camera;
using Character;
using Common;
using Damage.Data;
using Events;
using Events.Data;
using Features.Game.Data;
using Features.Game.UI;
using Features.Game.UI.BattleCommand;
using Features.Game.UI.Dialogue;
using Features.Game.UI.Map;
using Features.Game.UI.Notification;
using Features.Game.UI.Trade;
using Framework.Common.Timeline;
using Framework.Common.UI.Panel;
using Framework.Common.UI.Toast;
using Framework.Core.Lifecycle;
using Inputs;
using Player;
using Player.Ability;
using Quest;
using Skill.Runtime;
using Trade;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game
{
    /// <summary>
    /// 游戏业务的玩家UI控制器，负责玩家强相关的业务，比如玩家指令、玩家资源、对话框等
    /// </summary>
    public class GamePlayerUIController : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;
        [Inject] private CameraModel _cameraModel;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private TradeManager _tradeManager;
        [Inject] private QuestManager _questManager;
        [Inject] IObjectResolver _objectResolver;

        private PlayerCharacterObject _player;

        private void Awake()
        {
            // 监听UI数据变化
            _gameUIModel.GetSystemCommandUI().Observe(gameObject.GetMonoLifecycle(), OnSystemCommandUIDataChanged);
            _gameUIModel.GetCommonCommandUI().Observe(gameObject.GetMonoLifecycle(), OnCommonCommandUIDataChanged);
            _gameUIModel.GetComboCommandUI().Observe(gameObject.GetMonoLifecycle(), OnComboCommandUIDataChanged);
            _gameUIModel.GetBattleCommandUI().Observe(gameObject.GetMonoLifecycle(), OnBattleCommandUIDataChanged);
            _gameUIModel.GetSkillCommandUI().Observe(gameObject.GetMonoLifecycle(), OnSkillCommandUIDataChanged);
            _gameUIModel.GetPlayerResourceUI().Observe(gameObject.GetMonoLifecycle(), OnPlayerResourceUIDataChanged);
            _gameUIModel.GetMiniMapUI().Observe(gameObject.GetMonoLifecycle(), OnMiniMapUIDataChanged);
            _gameUIModel.GetDialogueUI().Observe(gameObject.GetMonoLifecycle(), OnDialogueUIDataChanged);
            _gameUIModel.GetTradeUI().Observe(gameObject.GetMonoLifecycle(), OnTradeUIDataChanged);
            _gameUIModel.GetNotificationUI().Observe(gameObject.GetMonoLifecycle(), OnNotificationUIDataChanged);
            _gameUIModel.GetTipUI().Observe(gameObject.GetMonoLifecycle(), OnTipUIDataChanged);
            _gameUIModel.IsBattleCommandExpanding().Observe(gameObject.GetMonoLifecycle(), OnBattleCommandExpanding);
            _gameUIModel.IsDialogueShowing().Observe(gameObject.GetMonoLifecycle(), OnDialogueShowing);

            // 监听交易开始和结束
            _tradeManager.OnTradeStarted += HandleTradeStarted;
            _tradeManager.OnTradeFinished += HandleTradeFinished;

            // 监听任务数据变化
            _questManager.OnQuestReceived += HandleQuestReceived;
            _questManager.OnQuestSubmitted += HandleQuestSubmitted;

            // 监听技能释放和结束事件
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);

            // 监听玩家角色创建和销毁
            _gameManager.OnPlayerCreated += OnPlayerCreated;
            _gameManager.OnPlayerDestroyed += OnPlayerDestroyed;

            // 监听角色加入战斗和退出战斗事件以及战斗等级提升事件
            _battleManager.OnBattleLevelUpgraded += OnPlayerBattleLevelUpgraded;
            _battleManager.OnCharacterJoinBattle += OnPlayerJoinBattle;
            _battleManager.OnCharacterExitBattle += OnPlayerExitBattle;
        }

        private void OnDestroy()
        {
            // 监听交易开始和结束
            _tradeManager.OnTradeStarted -= HandleTradeStarted;
            _tradeManager.OnTradeFinished -= HandleTradeFinished;

            // 取消监听任务数据变化
            _questManager.OnQuestReceived -= HandleQuestReceived;
            _questManager.OnQuestSubmitted -= HandleQuestSubmitted;

            // 取消监听技能释放和结束事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);

            // 取消监听玩家角色创建和销毁
            _gameManager.OnPlayerCreated -= OnPlayerCreated;
            _gameManager.OnPlayerDestroyed -= OnPlayerDestroyed;
            if (_gameManager.Player)
            {
                OnPlayerDestroyed(_gameManager.Player);
            }

            // 取消监听角色加入战斗和退出战斗事件以及战斗等级提升事件
            _battleManager.OnBattleLevelUpgraded -= OnPlayerBattleLevelUpgraded;
            _battleManager.OnCharacterJoinBattle -= OnPlayerJoinBattle;
            _battleManager.OnCharacterExitBattle -= OnPlayerExitBattle;
        }

        private void OnSystemCommandUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameSystemCommandPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameSystemCommandPanel>();
            }
        }

        private void OnCommonCommandUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameCommonCommandPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameCommonCommandPanel>();
            }
        }

        private void OnComboCommandUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameComboCommandPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameComboCommandPanel>();
            }
        }

        private void OnBattleCommandUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameBattleCommandPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameBattleCommandPanel>();
            }
        }

        private void OnSkillCommandUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameSkillCommandPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameSkillCommandPanel>();
            }
        }

        private void OnPlayerResourceUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GamePlayerResourcePanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GamePlayerResourcePanel>();
            }
        }

        private void OnMiniMapUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameMiniMapPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameMiniMapPanel>();
            }
        }

        private void OnDialogueUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameDialoguePanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameDialoguePanel>();
            }
        }

        private void OnTradeUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameTradePanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameTradePanel>();
            }
        }

        private void OnNotificationUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameNotificationPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameNotificationPanel>();
            }
        }

        private void OnTipUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameTipPanel>(
                    UGUIPanelLayer.Middle,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    isFade: false,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameTipPanel>();
            }
        }

        private void OnBattleCommandExpanding(bool expanding)
        {
            if (expanding)
            {
                _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Hide());
                _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Hide());
                _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
                _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
            }
            else
            {
                _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Show());
                _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Show());
                _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
                _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
            }
        }

        private void OnDialogueShowing(bool showing)
        {
            if (showing)
            {
                _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Hide());
                _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Hide());
                _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
                _gameUIModel.PlayerResourceUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
                _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
            }
            else
            {
                _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Show());
                _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Show());
                _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
                _gameUIModel.PlayerResourceUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
                _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
            }
        }

        private void HandleTradeStarted(Trade.Runtime.Trade trade)
        {
            // 如果交易有一方涉及到玩家，则显示交易UI
            if (trade.ContainsCharacter(_gameManager.Player))
            {
                _gameUIModel.TradeUI.SetValue(_gameUIModel.TradeUI.Value.Open(trade));
            }
        }

        private void HandleTradeFinished(Trade.Runtime.Trade trade)
        {
            // 如果交易有一方涉及到玩家，则关闭交易UI
            if (trade.ContainsCharacter(_gameManager.Player))
            {
                _gameUIModel.TradeUI.SetValue(_gameUIModel.TradeUI.Value.Close());
            }
        }

        private void HandleQuestReceived(Quest.Runtime.Quest quest, bool mute)
        {
            if (!mute)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(GameEvents.Tip,
                    new TipEventParameter
                    {
                        Tip = "接受任务"
                    });
            }
        }

        private void HandleQuestSubmitted(Quest.Runtime.Quest quest, bool mute)
        {
            if (!mute)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(GameEvents.Tip,
                    new TipEventParameter
                    {
                        Tip = "完成任务"
                    });
            }
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Hide());
            _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
            _gameUIModel.BattleCommandUI.SetValue(_gameUIModel.BattleCommandUI.Value.Hide());
            _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Hide());
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Show());
            _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
            _gameUIModel.BattleCommandUI.SetValue(_gameUIModel.BattleCommandUI.Value.Show());
            _gameUIModel.MiniMapUI.SetValue(_gameUIModel.ComboCommandUI.Value.Show());
        }

        private void OnPlayerCreated(PlayerCharacterObject player)
        {
            // 玩家创建后马上展示玩家相关UI
            _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Open());
            _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Open());
            _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Open());
            _gameUIModel.SkillCommandUI.SetValue(_gameUIModel.SkillCommandUI.Value.Open());
            _gameUIModel.PlayerResourceUI.SetValue(_gameUIModel.PlayerResourceUI.Value.Open());
            _gameUIModel.MiniMapUI.SetValue(_gameUIModel.MiniMapUI.Value.Open());
            _gameUIModel.DialogueUI.SetValue(_gameUIModel.DialogueUI.Value.Open());
            _gameUIModel.NotificationUI.SetValue(_gameUIModel.NotificationUI.Value.Open());
            _gameUIModel.TipUI.SetValue(_gameUIModel.TipUI.Value.Open());

            // 监听玩家状态事件
            player.StateAbility.OnCharacterKilled += OnPlayerKilled;
            player.StateAbility.OnCharacterRespawned += OnPlayerRespawned;

            // 监听玩家战斗事件
            if (player.BattleAbility is PlayerBattleAbility playerBattleAbility)
            {
                playerBattleAbility.OnPlayerEnterBattle += OnPlayerEnterBattle;
                playerBattleAbility.OnPlayerTryToEscapeBattle += OnPlayerTryToEscapeBattle;
                playerBattleAbility.OnPlayerEscapeBattle += OnPlayerEscapeBattle;
                playerBattleAbility.OnPlayerFinishBattle += OnPlayerFinishBattle;
            }
        }

        private void OnPlayerDestroyed(PlayerCharacterObject player)
        {
            // 取消监听玩家战斗事件
            if (player.BattleAbility is PlayerBattleAbility playerBattleAbility)
            {
                playerBattleAbility.OnPlayerEnterBattle -= OnPlayerEnterBattle;
                playerBattleAbility.OnPlayerTryToEscapeBattle -= OnPlayerTryToEscapeBattle;
                playerBattleAbility.OnPlayerEscapeBattle -= OnPlayerEscapeBattle;
                playerBattleAbility.OnPlayerFinishBattle -= OnPlayerFinishBattle;
            }

            // 取消监听玩家状态事件
            player.StateAbility.OnCharacterKilled -= OnPlayerKilled;
            player.StateAbility.OnCharacterRespawned -= OnPlayerRespawned;

            // 玩家销毁后马上关闭玩家相关UI
            _gameUIModel.SystemCommandUI.SetValue(_gameUIModel.SystemCommandUI.Value.Close());
            _gameUIModel.CommonCommandUI.SetValue(_gameUIModel.CommonCommandUI.Value.Close());
            _gameUIModel.ComboCommandUI.SetValue(_gameUIModel.ComboCommandUI.Value.Close());
            _gameUIModel.SkillCommandUI.SetValue(_gameUIModel.SkillCommandUI.Value.Close());
            _gameUIModel.PlayerResourceUI.SetValue(_gameUIModel.PlayerResourceUI.Value.Close());
            _gameUIModel.MiniMapUI.SetValue(_gameUIModel.MiniMapUI.Value.Close());
            _gameUIModel.DialogueUI.SetValue(_gameUIModel.DialogueUI.Value.Close());
            _gameUIModel.NotificationUI.SetValue(_gameUIModel.NotificationUI.Value.Close());
            _gameUIModel.TipUI.SetValue(_gameUIModel.TipUI.Value.Close());
        }

        private void OnPlayerBattleLevelUpgraded(BattleInfo battleInfo)
        {
            // 发送Boss战斗提示事件
            if (battleInfo.level == BattleLevel.Hard &&
                battleInfo.totalCharacters.Any(character => character == _gameManager.Player))
            {
                GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(GameEvents.Tip,
                    new TipEventParameter
                    {
                        Tip = "进入Boss战斗"
                    });
            }
        }

        private void OnPlayerJoinBattle(BattleInfo battleInfo, CharacterObject character)
        {
            // 搜索该场战斗是否为玩家参与的战斗，不是则直接返回
            if (!_gameManager.Player || character != _gameManager.Player)
            {
                return;
            }

            _gameUIModel.BattleCommandUI.SetValue(_gameUIModel.BattleCommandUI.Value.Open().ToVisible());
            // 发送进战提示事件
            GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(GameEvents.Tip,
                new TipEventParameter
                {
                    Tip = battleInfo.level == BattleLevel.Hard ? "进入Boss战斗" : "进入战斗",
                });
        }

        private void OnPlayerExitBattle(BattleInfo battleInfo, CharacterObject character)
        {
            // 搜索该场战斗是否为玩家参与的战斗，不是则直接返回
            if (!_gameManager.Player || character != _gameManager.Player)
            {
                return;
            }

            _gameUIModel.BattleCommandUI.SetValue(_gameUIModel.BattleCommandUI.Value.Close().ToVisible());
        }

        private void OnPlayerKilled(DamageInfo? damageInfo)
        {
        }

        private void OnPlayerRespawned(DamageInfo? damageInfo)
        {
        }

        private void OnPlayerEnterBattle(BattleInfo battleInfo)
        {
        }

        private void OnPlayerTryToEscapeBattle(BattleInfo battleInfo, float escapeTime)
        {
            if (escapeTime <= 0)
            {
                return;
            }

            Toast.Instance.Show($"正在逃离战斗，请在{escapeTime.ToString("F1")}秒内回到战斗区域", duration: 0.5f, realTime: false,
                location: ToastLocation.Center);
        }

        private void OnPlayerEscapeBattle(BattleInfo battleInfo)
        {
        }

        private void OnPlayerFinishBattle(BattleInfo battleInfo)
        {
        }
    }
}