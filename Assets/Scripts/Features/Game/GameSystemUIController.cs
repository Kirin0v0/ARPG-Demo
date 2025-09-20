using System;
using System.Collections;
using Common;
using Damage.Data;
using Events;
using Events.Data;
using Features.Game.Data;
using Features.Game.UI;
using Features.Game.UI.Archive;
using Features.Game.UI.Character;
using Features.Game.UI.Map;
using Features.Game.UI.Package;
using Features.Game.UI.Quest;
using Features.Main.Archive;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Core.Lifecycle;
using Player;
using UnityEngine;
using VContainer;

namespace Features.Game
{
    /// <summary>
    /// 游戏业务的系统UI控制器，负责系统强相关的业务，比如加载、设置、退出等UI
    /// </summary>
    public class GameSystemUIController : MonoBehaviour
    {
        [SerializeField] private float showDeathDelay = 2f;

        [Inject] private GameManager _gameManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] IObjectResolver _objectResolver;

        private void Awake()
        {
            // 监听UI数据变化
            _gameUIModel.GetLoadingUI().Observe(gameObject.GetMonoLifecycle(), OnLoadingUIDataChanged);
            _gameUIModel.GetCutsceneUI().Observe(gameObject.GetMonoLifecycle(), OnCutsceneUIDataChanged);
            _gameUIModel.GetMenuUI().Observe(gameObject.GetMonoLifecycle(), OnMenuUIDataChanged);
            _gameUIModel.GetMapUI().Observe(gameObject.GetMonoLifecycle(), OnMapUIDataChanged);
            _gameUIModel.GetCharacterUI().Observe(gameObject.GetMonoLifecycle(), OnCharacterUIDataChanged);
            _gameUIModel.GetPackageUI().Observe(gameObject.GetMonoLifecycle(), OnPackageUIDataChanged);
            _gameUIModel.GetQuestUI().Observe(gameObject.GetMonoLifecycle(), OnQuestUIDataChanged);
            _gameUIModel.GetArchiveUI().Observe(gameObject.GetMonoLifecycle(), OnArchiveUIDataChanged);
            _gameUIModel.GetDeathUI().Observe(gameObject.GetMonoLifecycle(), OnDeathUIDataChanged);

            // 监听玩家角色创建和销毁
            _gameManager.OnPlayerCreated += OnPlayerCreated;
            _gameManager.OnPlayerDestroyed += OnPlayerDestroyed;

            // 监听过场事件
            GameApplication.Instance.EventCenter.AddEventListener<CutsceneEventParameter>(GameEvents.Cutscene,
                HandleCutsceneEvent);
        }

        private void OnDestroy()
        {
            // 取消监听玩家角色创建和销毁
            _gameManager.OnPlayerCreated -= OnPlayerCreated;
            _gameManager.OnPlayerDestroyed -= OnPlayerDestroyed;
            if (_gameManager.Player)
            {
                OnPlayerDestroyed(_gameManager.Player);
            }

            // 取消监听过场事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<CutsceneEventParameter>(GameEvents.Cutscene,
                HandleCutsceneEvent);
        }

        private void OnLoadingUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameLoadingPanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    panel =>
                    {
                        _panelManager.InvisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                        _gameUIModel.ToHidePlaceholderUI.SetValue(true);
                    },
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameLoadingPanel>(() =>
                {
                    _panelManager.VisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                });
            }
        }

        private void OnCutsceneUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameCutscenePanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    panel =>
                    {
                        _panelManager.InvisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                    },
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameCutscenePanel>(() =>
                {
                    _panelManager.VisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                });
            }
        }

        private void OnMenuUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameMenuPanel>(
                    UGUIPanelLayer.Top,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameMenuPanel>();
            }
        }

        private void OnMapUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameMapPanel>(
                    UGUIPanelLayer.Top,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameMapPanel>();
            }
        }

        private void OnCharacterUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameCharacterPanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    panel =>
                    {
                        _panelManager.InvisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                    },
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameCharacterPanel>(() =>
                {
                    _panelManager.VisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                });
            }
        }

        private void OnPackageUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GamePackagePanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    panel =>
                    {
                        _panelManager.InvisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                    },
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GamePackagePanel>(() =>
                {
                    _panelManager.VisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                });
            }
        }

        private void OnQuestUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameQuestPanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    panel =>
                    {
                        _panelManager.InvisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                    },
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameQuestPanel>(() =>
                {
                    _panelManager.VisibleUntilSpecifiedLayer(UGUIPanelLayer.System);
                });
            }
        }

        private void OnArchiveUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameArchivePanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameArchivePanel>();
            }
        }

        private void OnDeathUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                _panelManager.Show<GameDeathPanel>(
                    UGUIPanelLayer.System,
                    panel => { _objectResolver.Inject(panel); },
                    null,
                    payload: data.Payload
                );
            }
            else
            {
                _panelManager.Hide<GameDeathPanel>();
            }
        }

        private void OnPlayerCreated(PlayerCharacterObject player)
        {
            StopAllCoroutines();
            _gameUIModel.MenuUI.SetValue(_gameUIModel.MenuUI.Value.Close());
            _gameUIModel.MapUI.SetValue(_gameUIModel.MapUI.Value.Close());
            _gameUIModel.CharacterUI.SetValue(_gameUIModel.CharacterUI.Value.Close());
            _gameUIModel.PackageUI.SetValue(_gameUIModel.PackageUI.Value.Close());
            _gameUIModel.QuestUI.SetValue(_gameUIModel.QuestUI.Value.Close());
            _gameUIModel.ArchiveUI.SetValue(_gameUIModel.ArchiveUI.Value.Close());
            _gameUIModel.DeathUI.SetValue(_gameUIModel.DeathUI.Value.Close());
            player.StateAbility.OnCharacterKilled += HandlePlayerKilled;
            player.StateAbility.OnCharacterRespawned += HandlePlayerRespawned;
        }

        private void OnPlayerDestroyed(PlayerCharacterObject player)
        {
            player.StateAbility.OnCharacterKilled -= HandlePlayerKilled;
            player.StateAbility.OnCharacterRespawned -= HandlePlayerRespawned;
        }

        private void HandleCutsceneEvent(CutsceneEventParameter parameter)
        {
            _gameUIModel.CutsceneUI.SetValue(_gameUIModel.CutsceneUI.Value.Open(new GameCutsceneUIData
            {
                Title = parameter.Title,
                Audio = parameter.Audio,
                Duration = parameter.Duration,
                OnFinished = parameter.OnFinished,
            }));
        }

        private void HandlePlayerKilled(DamageInfo? damageInfo)
        {
            StartCoroutine(DelayShowDeathUI(damageInfo));
        }

        private void HandlePlayerRespawned(DamageInfo? damageInfo)
        {
            _gameUIModel.DeathUI.SetValue(_gameUIModel.DeathUI.Value.Close());
        }

        private IEnumerator DelayShowDeathUI(DamageInfo? damageInfo)
        {
            yield return new WaitForSeconds(showDeathDelay);
            if (!_gameManager.Player || !_gameManager.Player.Parameters.dead)
            {
                yield break;
            }

            if (damageInfo != null)
            {
                _gameUIModel.DeathUI.SetValue(_gameUIModel.DeathUI.Value.Open(new GameDeathUIData
                {
                    Message = damageInfo.Value.Method switch
                    {
                        DamageBuffMethod damageBuffMethod => $"因Buff({damageBuffMethod.Name})死亡",
                        DamageComboMethod damageComboMethod => $"因招式({damageComboMethod.Name})死亡",
                        DamageEnvironmentMethod damageEnvironmentMethod => damageEnvironmentMethod.Type switch
                        {
                            DamageEnvironmentType.Fall => "因坠落死亡",
                            _ => "因环境伤害死亡",
                        },
                        DamageSkillMethod damageSkillMethod => $"因技能({damageSkillMethod.Name})死亡",
                        _ => "因未知原因死亡",
                    }
                }));
            }
            else
            {
                _gameUIModel.DeathUI.SetValue(_gameUIModel.DeathUI.Value.Open(new GameDeathUIData
                {
                    Message = "因未知原因死亡"
                }));
            }
        }
    }
}