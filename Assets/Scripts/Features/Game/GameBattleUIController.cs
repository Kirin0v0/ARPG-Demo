using System;
using System.Collections.Generic;
using System.Linq;
using Camera;
using Camera.Data;
using Character;
using Common;
using Dialogue;
using Events;
using Features.Game.UI;
using Features.Game.UI.CharacterInfo;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.Timeline;
using Framework.Common.UI.Panel;
using Framework.Common.Util;
using Framework.Core.Lifecycle;
using Inputs;
using Map;
using Player;
using Player.Ability;
using Sirenix.Utilities;
using Skill;
using Skill.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Game
{
    /// <summary>
    /// 游戏业务的战斗UI控制器，负责战斗强相关的业务，比如未战斗的敌人资源UI、锁定/可选图标展示、战斗时全部角色资源UI等
    /// </summary>
    public class GameBattleUIController : MonoBehaviour
    {
        private enum CharacterInfoPanelType
        {
            Player,
            Ally,
            Neutral,
            Monster,
            Elite,
            Boss,
        }

        private class CharacterInfoPanel
        {
            public CharacterInfoPanelType Type;
            public GameObject Panel;
        }

        [SerializeField] private Transform worldSpaceCanvas;
        [SerializeField] private Transform screenSpaceCameraCanvas;

        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;
        [Inject] private CameraModel _cameraModel;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private IGameModel _gameModel;
        [Inject] private IGameUIModel _gameUIModel;
        [Inject] IObjectResolver _objectResolver;

        private PlayerCharacterObject _player;

        // 战斗图标统一使用对象池进行管理
        private ObjectPool<GameObject> _imgLockTargetPool;
        private ObjectPool<GameObject> _imgSelectableTargetPool;
        private ObjectPool<GameObject> _imgSelectedTargetPool;

        // 记录锁定模板列表
        private readonly Dictionary<CharacterObject, GameObject> _imgLockTargets = new();

        // 记录本次选择的目标列表和目标对应的图标物体
        private readonly Dictionary<CharacterObject, GameObject> _imgSelectionTargets = new();
        private CharacterObject _selectedTarget;

        // 全屏特效UI
        private GameFullScreenEffectPanel _fullScreenEffectPanel;

        // 当前正在展示的角色信息表盘和各种类型的对象池
        private readonly Dictionary<CharacterObject, CharacterInfoPanel> _showingCharacterInfoPanels = new();
        private ObjectPool<GamePlayerInfoPanel> _playerInfoPanelPool;
        private ObjectPool<GameAllyInfoPanel> _allyInfoPanelPool;
        private ObjectPool<GameEnemyInfoPanel> _enemyInfoPanelPool;
        private ObjectPool<GameEnemyEliteInfoPanel> _enemyEliteInfoPanelPool;
        private ObjectPool<GameEnemyBossInfoPanel> _enemyBossInfoPanelPool;

        private void Awake()
        {
            // 监听锁定/解锁目标事件
            _cameraModel.GetLock().Observe(gameObject.GetMonoLifecycle(), HandleCameraLockOrUnlock);

            // 监听魔女时间是否激活
            _gameModel.IsPlayerWitchTimeActive()
                .Observe(gameObject.GetMonoLifecycle(), HandlePlayerWitchTimeWhetherActive);

            // 监听选择目标相关事件
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject[]>(
                GameEvents.StartTargetSelection, OnStartTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(GameEvents.SelectTarget,
                OnSelectTarget);
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            // 监听角色加入战斗和退出战斗事件
            _battleManager.OnCharacterJoinBattle += OnCharacterJoinBattle;
            _battleManager.OnCharacterExitBattle += OnCharacterExitBattle;

            // 监听技能释放开始和结束事件
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(
                GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);

            // 监听地图切换事件
            _mapManager.BeforeMapLoad += RecycleUI;

            #region 加载目标图标UI资源

            GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>("ImgLockTarget", handle =>
            {
                _imgLockTargetPool = new ObjectPool<GameObject>(
                    createFunction: () =>
                    {
                        var prefab = Instantiate(handle, worldSpaceCanvas, false);
                        prefab.AddComponent<FollowAndFacingCameraBehaviour>();
                        return prefab;
                    },
                    destroyFunction: (prefab) => { GameObject.Destroy(prefab); }
                );
            });

            GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>("ImgSelectedTarget", handle =>
            {
                _imgSelectedTargetPool = new ObjectPool<GameObject>(
                    createFunction: () =>
                    {
                        var prefab = Instantiate(handle, worldSpaceCanvas, false);
                        prefab.AddComponent<FollowAndFacingCameraBehaviour>();
                        return prefab;
                    },
                    destroyFunction: (prefab) => { GameObject.Destroy(prefab); }
                );
            });
            GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>("ImgSelectableTarget", handle =>
            {
                _imgSelectableTargetPool = new ObjectPool<GameObject>(
                    createFunction: () =>
                    {
                        var prefab = Instantiate(handle, worldSpaceCanvas, false);
                        prefab.AddComponent<FollowAndFacingCameraBehaviour>();
                        return prefab;
                    },
                    destroyFunction: (prefab) => { GameObject.Destroy(prefab); }
                );
            });

            #endregion

            #region 加载全屏特效UI

            _panelManager.LoadAlone<GameFullScreenEffectPanel>(panel =>
                {
                    _fullScreenEffectPanel = _objectResolver.Instantiate(panel, screenSpaceCameraCanvas);
                    _fullScreenEffectPanel.gameObject.SetActive(true);
                    if (_gameModel.IsPlayerWitchTimeActive().HasValue() &&
                        _gameModel.IsPlayerWitchTimeActive().Value)
                    {
                        _fullScreenEffectPanel.Show();
                    }
                }
            );

            #endregion

            #region 加载角色信息表盘并创建对象池

            _panelManager.LoadAlone<GameEnemyInfoPanel>(panel =>
            {
                _enemyInfoPanelPool = new ObjectPool<GameEnemyInfoPanel>(
                    createFunction: () =>
                    {
                        var instance = _objectResolver.Instantiate(panel, worldSpaceCanvas);
                        instance.gameObject.SetActive(true);
                        instance.gameObject.AddComponent<FollowAndFacingCameraBehaviour>();
                        return instance;
                    },
                    destroyFunction: panel => { GameObject.Destroy(panel.gameObject); });
            });

            _panelManager.LoadAlone<GameEnemyEliteInfoPanel>(panel =>
            {
                _enemyEliteInfoPanelPool = new ObjectPool<GameEnemyEliteInfoPanel>(
                    createFunction: () =>
                    {
                        var instance = _objectResolver.Instantiate(panel, worldSpaceCanvas);
                        instance.gameObject.SetActive(true);
                        instance.gameObject.AddComponent<FollowAndFacingCameraBehaviour>();
                        return instance;
                    },
                    destroyFunction: panel => { GameObject.Destroy(panel.gameObject); });
            });

            _panelManager.LoadAlone<GameEnemyBossInfoPanel>(panel =>
            {
                _enemyBossInfoPanelPool = new ObjectPool<GameEnemyBossInfoPanel>(
                    createFunction: () =>
                    {
                        var instance = _objectResolver.Instantiate(panel, worldSpaceCanvas);
                        instance.gameObject.SetActive(true);
                        instance.gameObject.AddComponent<FollowAndFacingCameraBehaviour>();
                        return instance;
                    },
                    destroyFunction: panel => { GameObject.Destroy(panel.gameObject); });
            });

            _panelManager.LoadAlone<GamePlayerInfoPanel>(panel =>
            {
                _playerInfoPanelPool = new ObjectPool<GamePlayerInfoPanel>(
                    createFunction: () =>
                    {
                        var instance = _objectResolver.Instantiate(panel, worldSpaceCanvas);
                        instance.gameObject.SetActive(true);
                        instance.gameObject.AddComponent<FollowAndFacingCameraBehaviour>();
                        return instance;
                    },
                    destroyFunction: panel => { GameObject.Destroy(panel.gameObject); });
            });

            _panelManager.LoadAlone<GameAllyInfoPanel>(panel =>
            {
                _allyInfoPanelPool = new ObjectPool<GameAllyInfoPanel>(
                    createFunction: () =>
                    {
                        var instance = _objectResolver.Instantiate(panel, worldSpaceCanvas);
                        instance.gameObject.SetActive(true);
                        instance.gameObject.AddComponent<FollowAndFacingCameraBehaviour>();
                        return instance;
                    },
                    destroyFunction: panel => { GameObject.Destroy(panel.gameObject); });
            });

            #endregion
        }

        private void Update()
        {
            // 每帧遍历场景的全部角色，更新新显示的角色表盘并回收该隐藏的角色表盘
            var toHideCharacterInfoPanels = _showingCharacterInfoPanels.Keys.ToHashSet();
            _gameManager.Characters.ForEach(character =>
            {
                // 不允许展示就跳过
                if (!AllowCharacterShowInfo(character))
                {
                    return;
                }

                toHideCharacterInfoPanels.Remove(character);

                // 如果不存在该角色表盘，就添加到展示列表中，否则判断表盘类型是否改变
                var newestType = character.Parameters.side switch
                {
                    CharacterSide.Player when character == _gameManager.Player => CharacterInfoPanelType.Player,
                    CharacterSide.Player => CharacterInfoPanelType.Ally,
                    CharacterSide.Neutral => CharacterInfoPanelType.Ally,
                    CharacterSide.Enemy when character.HasTag("boss") => CharacterInfoPanelType.Boss,
                    CharacterSide.Enemy when character.HasTag("elite") => CharacterInfoPanelType.Elite,
                    CharacterSide.Enemy => CharacterInfoPanelType.Monster,
                    _ => CharacterInfoPanelType.Monster,
                };
                if (!_showingCharacterInfoPanels.TryGetValue(character, out var panel))
                {
                    var characterInfoPanel = CreateCharacterInfoPanel(character, newestType);
                    if (characterInfoPanel != null)
                    {
                        _showingCharacterInfoPanels.Add(character, characterInfoPanel);
                    }
                }
                else if (panel.Type != newestType)
                {
                    // 表盘类型改变就回收旧有表盘并创建新表盘
                    RecycleCharacterInfoPanel(character, panel);
                    var characterInfoPanel = CreateCharacterInfoPanel(character, newestType);
                    panel.Type = characterInfoPanel.Type;
                    panel.Panel = characterInfoPanel.Panel;
                }
            });

            // 隐藏不显示的角色表盘
            toHideCharacterInfoPanels.ForEach(character =>
            {
                if (_showingCharacterInfoPanels.TryGetValue(character, out var panel))
                {
                    RecycleCharacterInfoPanel(character, panel);
                    _showingCharacterInfoPanels.Remove(character);
                }
            });

            // 最后根据角色距离相机位置对表盘优先级进行排序，保证距离近的角色表盘覆盖展示
            var camera = UnityEngine.Camera.main;
            if (camera)
            {
                var showingCharacters = _showingCharacterInfoPanels.Keys.ToList();
                showingCharacters.Sort((a, b) =>
                {
                    var distanceA = Vector3.Distance(a.Parameters.position, camera.transform.position);
                    var distanceB = Vector3.Distance(b.Parameters.position, camera.transform.position);
                    return distanceA.CompareTo(distanceB);
                });
                showingCharacters.ForEach(character =>
                {
                    if (_showingCharacterInfoPanels.TryGetValue(character, out var panel))
                    {
                        panel.Panel.gameObject.transform.SetAsFirstSibling();
                    }
                });
            }

            // 每帧更新锁定图标的显示优先级
            _imgLockTargets.Values.ForEach(target => target.transform.SetAsLastSibling());

            // 每帧更新可选图标的显示优先级
            _imgSelectionTargets.Values.ForEach(target => target.transform.SetAsLastSibling());

            return;

            bool AllowCharacterShowInfo(CharacterObject character)
            {
                // 场景整体不显示角色信息时不显示角色表盘
                if (!_gameUIModel.AllowCharacterInformationShowing().HasValue() ||
                    !_gameUIModel.AllowCharacterInformationShowing().Value)
                {
                    return false;
                }

                // 角色死亡时不显示角色表盘
                if (character.Parameters.dead)
                {
                    return false;
                }

                // 角色可见时显示角色表盘
                if (character.Parameters.visible)
                {
                    return true;
                }

                // 角色正在玩家活跃的战斗则显示角色表盘
                if (character.Parameters.battleState == CharacterBattleState.Battle &&
                    _battleManager.IsPlayerActiveBattle(character.Parameters.battleId))
                {
                    return true;
                }

                return false;
            }
        }

        private void OnDestroy()
        {
            // 取消监听选择目标相关事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject[]>(
                GameEvents.StartTargetSelection, OnStartTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(GameEvents.SelectTarget,
                OnSelectTarget);
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            // 取消监听角色加入战斗和退出战斗事件
            _battleManager.OnCharacterJoinBattle -= OnCharacterJoinBattle;
            _battleManager.OnCharacterExitBattle -= OnCharacterExitBattle;

            // 取消监听技能释放开始和结束事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);

            // 解除监听地图切换事件
            _mapManager.BeforeMapLoad -= RecycleUI;

            // 销毁UI
            DestroyUI();

            // 卸载UI资源
            GameApplication.Instance?.AddressablesManager.ReleaseAsset<GameObject>("ImgLockTarget");
            GameApplication.Instance?.AddressablesManager.ReleaseAsset<GameObject>("ImgSelectedTarget");
            GameApplication.Instance?.AddressablesManager.ReleaseAsset<GameObject>("ImgSelectableTarget");
        }

        private void RecycleUI()
        {
            _imgLockTargets.ForEach(pair => _imgLockTargetPool.Release(pair.Value, (imgLockTarget) =>
                {
                    var component = imgLockTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = null;
                    imgLockTarget.SetActive(false);
                })
            );
            _imgLockTargets.Clear();

            _imgSelectionTargets.ForEach(pair =>
            {
                if (pair.Key == _selectedTarget)
                {
                    _imgSelectedTargetPool.Release(pair.Value, (imgSelectedTarget) =>
                    {
                        var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectedTarget.SetActive(false);
                    });
                }
                else
                {
                    _imgSelectableTargetPool.Release(pair.Value, (imgSelectableTarget) =>
                    {
                        var component = imgSelectableTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectableTarget.SetActive(false);
                    });
                }
            });
            _imgSelectionTargets.Clear();
            _selectedTarget = null;

            _showingCharacterInfoPanels.ForEach(pair => { RecycleCharacterInfoPanel(pair.Key, pair.Value); });
            _showingCharacterInfoPanels.Clear();
        }

        private void DestroyUI()
        {
            _imgLockTargets.ForEach(pair => GameObject.Destroy(pair.Value));
            _imgLockTargetPool?.Clear();
            _imgLockTargetPool = null;

            _imgSelectionTargets.ForEach(pair => GameObject.Destroy(pair.Value));
            _imgSelectedTargetPool?.Clear();
            _imgSelectableTargetPool?.Clear();
            _imgSelectedTargetPool = null;
            _imgSelectableTargetPool = null;

            _showingCharacterInfoPanels.ForEach(pair => GameObject.Destroy(pair.Value.Panel));
            _showingCharacterInfoPanels.Clear();
            _enemyInfoPanelPool?.Clear();
            _enemyEliteInfoPanelPool?.Clear();
            _enemyBossInfoPanelPool?.Clear();
            _playerInfoPanelPool?.Clear();
            _allyInfoPanelPool?.Clear();
            _enemyInfoPanelPool = null;
            _enemyEliteInfoPanelPool = null;
            _enemyBossInfoPanelPool = null;
            _playerInfoPanelPool = null;
            _allyInfoPanelPool = null;
        }

        private void HandleCameraLockOrUnlock(CameraLockData cameraLockData)
        {
            // 回收图标物体
            _imgLockTargets.ForEach(pair =>
            {
                _imgLockTargetPool.Release(pair.Value, (imgLockTarget) =>
                {
                    var component = imgLockTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = null;
                    imgLockTarget.SetActive(false);
                });
            });
            _imgLockTargets.Clear();
            // 如果是锁定，则创建图标并添加到字典中
            if (cameraLockData.@lock && cameraLockData.lockTarget)
            {
                var instance = _imgLockTargetPool.Get((imgLockTarget) =>
                {
                    imgLockTarget.SetActive(true);
                    var component = imgLockTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = cameraLockData.lockTarget.Visual.Center;
                });
                _imgLockTargets.Add(cameraLockData.lockTarget, instance);
            }
        }

        private void HandlePlayerWitchTimeWhetherActive(bool active)
        {
            if (!_fullScreenEffectPanel)
            {
                return;
            }

            if (active)
            {
                _fullScreenEffectPanel.Show();
            }
            else
            {
                _fullScreenEffectPanel.Hide();
            }
        }

        private void OnStartTargetSelection(CharacterObject[] targets)
        {
            // 创建可选目标图标并添加到字典中
            targets.ForEach(character =>
            {
                var instance = _imgSelectableTargetPool.Get((imgSelectedTarget) =>
                {
                    imgSelectedTarget.SetActive(true);
                    var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = character.Visual.Center;
                });
                _imgSelectionTargets.Add(character, instance);
            });
        }

        private void OnSelectTarget(CharacterObject target)
        {
            // 将先前目标换做可选图标
            if (_selectedTarget && _imgSelectionTargets.TryGetValue(_selectedTarget, out var previousSelectedIcon))
            {
                _imgSelectedTargetPool.Release(previousSelectedIcon, imgSelectedTarget =>
                {
                    var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = null;
                    imgSelectedTarget.SetActive(false);
                });
                _imgSelectionTargets[_selectedTarget] = _imgSelectableTargetPool.Get(imgSelectableTarget =>
                {
                    imgSelectableTarget.SetActive(true);
                    var component = imgSelectableTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = _selectedTarget.Visual.Center;
                });
            }

            _selectedTarget = target;

            // 将当前目标换做选中图标
            if (_selectedTarget && _imgSelectionTargets.TryGetValue(_selectedTarget, out var selectedIcon))
            {
                _imgSelectableTargetPool.Release(selectedIcon, imgSelectableTarget =>
                {
                    var component = imgSelectableTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = null;
                    imgSelectableTarget.SetActive(false);
                });
                _imgSelectionTargets[_selectedTarget] = _imgSelectedTargetPool.Get(imgSelectedTarget =>
                {
                    imgSelectedTarget.SetActive(true);
                    var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                    component.follow = _selectedTarget.Visual.Center;
                });
            }
        }

        private void OnCancelTargetSelection()
        {
            // 回收图标物体
            _imgSelectionTargets.ForEach(pair =>
            {
                if (pair.Key == _selectedTarget)
                {
                    _imgSelectedTargetPool.Release(pair.Value, (imgSelectedTarget) =>
                    {
                        var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectedTarget.SetActive(false);
                    });
                }
                else
                {
                    _imgSelectableTargetPool.Release(pair.Value, (imgSelectableTarget) =>
                    {
                        var component = imgSelectableTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectableTarget.SetActive(false);
                    });
                }
            });
            _imgSelectionTargets.Clear();
            _selectedTarget = null;
        }

        private void OnFinishTargetSelection(CharacterObject target)
        {
            // 回收图标物体
            _imgSelectionTargets.ForEach(pair =>
            {
                if (pair.Key == _selectedTarget)
                {
                    _imgSelectedTargetPool.Release(pair.Value, (imgSelectedTarget) =>
                    {
                        var component = imgSelectedTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectedTarget.SetActive(false);
                    });
                }
                else
                {
                    _imgSelectableTargetPool.Release(pair.Value, (imgSelectableTarget) =>
                    {
                        var component = imgSelectableTarget.GetComponent<FollowAndFacingCameraBehaviour>();
                        component.follow = null;
                        imgSelectableTarget.SetActive(false);
                    });
                }
            });
            _imgSelectionTargets.Clear();
            _selectedTarget = null;
        }

        private void OnCharacterJoinBattle(BattleInfo battleInfo, CharacterObject character)
        {
            // 搜索该场战斗是否为玩家参与的战斗，不是则直接返回
            if (!_gameManager.Player || battleInfo.id != _gameManager.Player.Parameters.battleId)
            {
                return;
            }
        }

        private void OnCharacterExitBattle(BattleInfo battleInfo, CharacterObject character)
        {
            // 搜索该场战斗是否为玩家参与的战斗，不是则直接返回
            if (!_gameManager.Player || battleInfo.id != _gameManager.Player.Parameters.battleId)
            {
                return;
            }
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo releaseInfo)
        {
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo releaseInfo)
        {
        }

        private void RecycleCharacterInfoPanel(CharacterObject character, CharacterInfoPanel panel)
        {
            // 回收旧表盘到对象池中
            switch (panel.Type)
            {
                case CharacterInfoPanelType.Player:
                {
                    _playerInfoPanelPool?.Release(panel.Panel.GetComponent<GamePlayerInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindPlayer();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
                case CharacterInfoPanelType.Ally:
                {
                    _allyInfoPanelPool?.Release(panel.Panel.GetComponent<GameAllyInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindAlly();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
                case CharacterInfoPanelType.Neutral:
                {
                    _allyInfoPanelPool?.Release(panel.Panel.GetComponent<GameAllyInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindAlly();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
                case CharacterInfoPanelType.Monster:
                {
                    _enemyInfoPanelPool?.Release(panel.Panel.GetComponent<GameEnemyInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindEnemy();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
                case CharacterInfoPanelType.Elite:
                {
                    _enemyEliteInfoPanelPool?.Release(panel.Panel.GetComponent<GameEnemyEliteInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindEnemy();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
                case CharacterInfoPanelType.Boss:
                {
                    _enemyBossInfoPanelPool?.Release(panel.Panel.GetComponent<GameEnemyBossInfoPanel>(),
                        panel =>
                        {
                            panel.Hide(false);
                            panel.UnbindBoss();
                            if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                            {
                                component.follow = null;
                            }
                        });
                }
                    break;
            }
        }

        private CharacterInfoPanel CreateCharacterInfoPanel(CharacterObject character, CharacterInfoPanelType type)
        {
            GameObject panel = null;
            switch (type)
            {
                case CharacterInfoPanelType.Player:
                {
                    panel = _playerInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindPlayer(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
                case CharacterInfoPanelType.Ally:
                {
                    panel = _allyInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindAlly(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
                case CharacterInfoPanelType.Neutral:
                {
                    panel = _allyInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindAlly(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
                case CharacterInfoPanelType.Monster:
                {
                    panel = _enemyInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindEnemy(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
                case CharacterInfoPanelType.Elite:
                {
                    panel = _enemyEliteInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindEnemy(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
                case CharacterInfoPanelType.Boss:
                {
                    panel = _enemyBossInfoPanelPool?.Get(panel =>
                    {
                        panel.Show(false);
                        panel.BindBoss(character);
                        if (panel.gameObject.TryGetComponent<FollowAndFacingCameraBehaviour>(out var component))
                        {
                            component.follow = character.Visual.Top;
                        }
                    })?.gameObject;
                }
                    break;
            }

            return panel
                ? new CharacterInfoPanel
                {
                    Type = type,
                    Panel = panel
                }
                : null;
        }
    }
}