using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Archive;
using Archive.Data;
using Camera;
using Character.Data;
using Common;
using Damage;
using Damage.Data;
using Events;
using Events.Data;
using Features.Game.Data;
using Features.Game.UI;
using Features.Game.UI.Archive;
using Features.Game.UI.BattleCommand;
using Features.Game.UI.CharacterInfo;
using Features.Game.UI.Dialogue;
using Features.Game.UI.Map;
using Features.Game.UI.Notification;
using Features.Game.UI.Package;
using Features.Game.UI.Quest;
using Features.Game.UI.Trade;
using Features.Splash.UI;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.Timeline;
using Framework.Common.UI.Panel;
using Framework.Core.Extension;
using Framework.Core.Lifecycle;
using Humanoid.Data;
using Humanoid.Model.Data;
using Humanoid.Weapon;
using Inputs;
using Map;
using Map.Data;
using Package;
using Package.Data;
using Player;
using Quest;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Runtime;
using Trade;
using UnityEngine;
using VContainer;

namespace Features.Game
{
    public class GameScene : BaseScene, IArchivable
    {
        private delegate void LoadStepChanged(string name, int currentStepIndex, int totalSteps);

        [Title("二进制文件路径")] [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string modelInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string appearanceBodyInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string appearanceWeaponInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string appearanceGearInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string packageInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string characterInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapCharacterInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapPackageInfoBinaryPath = "";

        [SerializeField, Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapRestSpotInfoBinaryPath = "";

        [Title("默认占位UI")] [SerializeField] private RectTransform defaultPlaceholderUI;

        [Title("魔女时间配置")] [SerializeField] private string witchTimeThumbnailAtlas;
        [SerializeField] private string witchTimeThumbnailName;

        [Inject] private GameModel _gameModel;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private InputInfoManager _inputInfoManager;
        [Inject] private DamageManager _damageManager;
        [Inject] private TimelineManager _timelineManager;
        [Inject] private MapManager _mapManager;
        [Inject] private GameManager _gameManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private QuestManager _questManager;
        [Inject] private PackageManager _packageManager;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private AudioManager _audioManager;
        [Inject] private TradeManager _tradeManager;
        [Inject] private CameraManager _cameraManager;
        [Inject] private HumanoidWeaponManager _weaponManager;
        [Inject] private IObjectResolver _objectResolver;

        protected override void OnAwake()
        {
            base.OnAwake();
            // 监听游戏时间数据，用于控制游戏流速
            _gameModel.GetGameTime().Observe(gameObject.GetMonoLifecycle(), gameTimeData =>
            {
                switch (gameTimeData.Mode)
                {
                    case GameTimeMode.Pause:
                    {
                        DebugUtil.LogOrange("游戏时间暂停");
                        Time.timeScale = 0f;
                    }
                        break;
                    case GameTimeMode.Default:
                    {
                        DebugUtil.LogOrange("游戏时间恢复");
                        Time.timeScale = 1f;
                    }
                        break;
                }
            });

            // 监听战斗命令列表展开和关闭导致的时间场景切换
            _gameUIModel.IsBattleCommandExpanding().Observe(gameObject.GetMonoLifecycle(), expanding =>
            {
                if (expanding)
                {
                    _gameManager.AddTimeScaleGlobalCommand("BattleCommandExpanding", 0.05f);
                }
                else
                {
                    _gameManager.RemoveTimeScaleCommand("BattleCommandExpanding");
                }
            });

            // 控制玩家输入图切换
            _gameModel.AllowPlayerInput().Observe(gameObject.GetMonoLifecycle(),
                allow => { _playerInputManager.SwitchCurrentActionMap(allow ? "Player" : "UI"); });

            // 控制游标显示度
            _gameModel.ShowCursor().Observe(gameObject.GetMonoLifecycle(),
                visible =>
                {
                    if (visible)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.Confined;
                    }
                    else
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                });

            // 监听玩家魔女时间的激活和失活
            _gameModel.IsPlayerWitchTimeActive().Observe(gameObject.GetMonoLifecycle(), active =>
            {
                if (active)
                {
                    // 添加时间缩放命令
                    _gameManager.AddTimeScaleWitchCommand("WitchTime", float.MaxValue, 0.1f, 1.5f, _gameManager.Player);
                    // 发送通知事件
                    GameApplication.Instance.EventCenter.TriggerEvent<NotificationEventParameter>(
                        GameEvents.Notification, new NotificationEventParameter
                        {
                            ThumbnailAtlas = witchTimeThumbnailAtlas,
                            ThumbnailName = witchTimeThumbnailName,
                            Title = "进入魔女时间",
                        });
                }
                else
                {
                    // 删除时间缩放命令
                    _gameManager.RemoveTimeScaleCommand("WitchTime");
                    // 发送通知事件
                    GameApplication.Instance.EventCenter.TriggerEvent<NotificationEventParameter>(
                        GameEvents.Notification, new NotificationEventParameter
                        {
                            ThumbnailAtlas = witchTimeThumbnailAtlas,
                            ThumbnailName = witchTimeThumbnailName,
                            Title = "退出魔女时间",
                        });
                }
            });

            // 监听是否隐藏占位UI
            _gameUIModel.HidePlaceholderUI().Observe(gameObject.GetMonoLifecycle(),
                (toHide) => { defaultPlaceholderUI.gameObject.SetActive(!toHide); });

            // 注册玩家创建和销毁监听
            _gameManager.OnPlayerCreated += HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed += HandlePlayerDestroyed;

            // 注册伤害处理监听，用于设置魔女时间
            _damageManager.AfterDamageHandled += HandleDamage;

            // 注册存档监听
            GameApplication.Instance.ArchiveManager.Register(this);

            // 监听触发魔女时间事件
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.TriggerWitchTime, HandleWitchTimeTrigger);

            // 注册传送事件监听
            GameApplication.Instance.EventCenter.AddEventListener<TeleportEventParameter>(GameEvents.Teleport,
                HandlePlayerTeleport);

            // 注册刷新地图事件监听
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.RefreshMap, HandleMapRefresh);

            // 注册场景切换监听
            SceneManager.Instance.OnSceneStartLoad += HandleSceneStartLoad;
            SceneManager.Instance.OnSceneCompleteLoad += HandleSceneCompleteLoad;
        }

        private void Start()
        {
            InitScene();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 解除注册玩家创建和销毁监听
            _gameManager.OnPlayerCreated -= HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed -= HandlePlayerDestroyed;

            if (_gameManager.Player)
            {
                HandlePlayerDestroyed(_gameManager.Player);
            }

            // 解除注册伤害处理监听，用于设置魔女时间
            _damageManager.AfterDamageHandled -= HandleDamage;

            // 解除注册存档监听
            GameApplication.Instance?.ArchiveManager.Unregister(this);

            // 解除监听触发魔女时间事件
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.TriggerWitchTime,
                HandleWitchTimeTrigger);

            // 解除注册传送事件监听
            GameApplication.Instance?.EventCenter.RemoveEventListener<TeleportEventParameter>(GameEvents.Teleport,
                HandlePlayerTeleport);

            // 解除注册刷新地图事件监听
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.RefreshMap, HandleMapRefresh);

            // 解除注册场景切换监听
            if (SceneManager.Instance)
            {
                SceneManager.Instance.OnSceneStartLoad -= HandleSceneStartLoad;
                SceneManager.Instance.OnSceneCompleteLoad -= HandleSceneCompleteLoad;
            }
        }

        public void Save(ArchiveData archiveData)
        {
        }

        public async void Load(ArchiveData archiveData)
        {
            _gameUIModel.LoadingUI.SetValue(_gameUIModel.LoadingUI.Value.Open());
            // 等待其他模块加载完成后再加载该模块
            await Task.Yield();
            DebugUtil.LogCyan("开始加载场景");
            await LoadInternal(archiveData);
            await Task.Delay(500);
            _gameUIModel.LoadingUI.SetValue(_gameUIModel.LoadingUI.Value.Close());
        }

        protected override void OnGotoSceneBefore(GotoSceneEventParameter parameter)
        {
            base.OnGotoSceneBefore(parameter);
            // 在前往场景前无论如何都要执行以下逻辑
            ClearScene();
        }

        private async void InitScene()
        {
            await Task.Run(() =>
            {
                // 在加载存档前先加载二进制数据
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidModelInfoContainer, HumanoidModelInfoData>(
                        Path.Combine(Application.streamingAssetsPath, modelInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidAppearanceBodyInfoContainer, HumanoidAppearanceBodyInfoData>(
                        Path.Combine(Application.streamingAssetsPath, appearanceBodyInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidAppearanceWeaponInfoContainer, HumanoidAppearanceWeaponInfoData>(
                        Path.Combine(Application.streamingAssetsPath, appearanceWeaponInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidAppearanceGearInfoContainer, HumanoidAppearanceGearInfoData>(
                        Path.Combine(Application.streamingAssetsPath, appearanceGearInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager.LoadContainer<PackageInfoContainer, PackageInfoData>(
                    Path.Combine(Application.streamingAssetsPath, packageInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<CharacterInfoContainer, CharacterInfoData>(
                        Path.Combine(Application.streamingAssetsPath, characterInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<MapInfoContainer, MapInfoData>(
                        Path.Combine(Application.streamingAssetsPath, mapInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<MapCharacterInfoContainer, MapCharacterInfoData>(
                        Path.Combine(Application.streamingAssetsPath, mapCharacterInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<MapPackageInfoContainer, MapPackageInfoData>(
                        Path.Combine(Application.streamingAssetsPath, mapPackageInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<MapRestSpotInfoContainer, MapRestSpotInfoData>(
                        Path.Combine(Application.streamingAssetsPath, mapRestSpotInfoBinaryPath));
            });

            // 加载二进制数据完毕后才开始通知读取存档
            GameApplication.Instance.ArchiveManager.NotifyLoad(GameApplication.Instance.CurrentArchiveId);
        }

        private void HandleWitchTimeTrigger()
        {
            _gameModel.ActivePlayerWitchTime(3f);
        }

        private async void HandlePlayerTeleport(TeleportEventParameter parameter)
        {
            _gameUIModel.LoadingUI.SetValue(_gameUIModel.LoadingUI.Value.Open());
            await TeleportInternal(parameter.MapId, parameter.Position, parameter.ForwardAngle);
            await Task.Delay(500);
            _gameUIModel.LoadingUI.SetValue(_gameUIModel.LoadingUI.Value.Close());
        }

        private void HandleMapRefresh()
        {
            RefreshMapInternal(_mapManager.MapId);
        }

        private void HandleSceneStartLoad(SceneEventParameter parameter)
        {
            _panelManager.Show<SplashPanel>(
                UGUIPanelLayer.System,
                panel => { _objectResolver.Inject(panel); },
                null
            );
        }

        private void HandleSceneCompleteLoad(SceneEventParameter parameter)
        {
            _panelManager.Hide<SplashPanel>();
        }

        private void HandlePlayerCreated(PlayerCharacterObject player)
        {
            // 注册玩家技能相关监听
            if (player.SkillAbility)
            {
                player.SkillAbility.OnSkillReleased += HandlePlayerSkillReleased;
                player.SkillAbility.OnSkillStopped += HandlePlayerSkillStoppedOrCompleted;
                player.SkillAbility.OnSkillCompleted += HandlePlayerSkillStoppedOrCompleted;
            }

            // 清空玩家魔女时间
            _gameModel.ClearPlayerWitchTime();
        }

        private void HandlePlayerDestroyed(PlayerCharacterObject player)
        {
            // 解除注册玩家技能相关监听
            if (player.SkillAbility)
            {
                player.SkillAbility.OnSkillReleased -= HandlePlayerSkillReleased;
                player.SkillAbility.OnSkillStopped -= HandlePlayerSkillStoppedOrCompleted;
                player.SkillAbility.OnSkillCompleted -= HandlePlayerSkillStoppedOrCompleted;
            }

            // 清空玩家魔女时间
            _gameModel.ClearPlayerWitchTime();
        }

        private void HandleDamage(DamageInfo damageInfo)
        {
            // 如果伤害的承受方是玩家才执行后续逻辑
            if (damageInfo.Target != _gameManager.Player) return;

            // // 判断伤害触发标识符，仅在触发完美防御或完美闪避时激活魔女时间
            // if ((damageInfo.TriggerFlags & DamageInfo.PerfectDefenceFlag) != 0 ||
            //     (damageInfo.TriggerFlags & DamageInfo.PerfectEvadeFlag) != 0)
            // {
            //     _gameModel.ActivePlayerWitchTime(3f);
            // }
        }

        private void HandlePlayerSkillReleased(SkillReleaseInfo skillReleaseInfo)
        {
            // 发送释放技能事件
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.ReleasePlayerSkill, skillReleaseInfo);
            // 添加时间释放技能命令
            _gameManager.AddTimeScaleSkillCommand("ReleasePlayerSkill", 1f, 0.05f, skillReleaseInfo.Caster);
        }

        private void HandlePlayerSkillStoppedOrCompleted(SkillReleaseInfo skillReleaseInfo)
        {
            // 发送完成技能事件
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.CompletePlayerSkill, skillReleaseInfo);
            // 删除时间释放技能命令
            _gameManager.RemoveTimeScaleCommand("ReleasePlayerSkill");
        }

        private async Task TeleportInternal(int mapId, Vector3 position, float forwardAngle)
        {
            _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
            {
                Name = "",
                Progress = 0f,
            });
            // 先判断是否是同地图上的传送，是则仅改变角色位置，否则在切换地图的基础上清空所有数据并重新加载
            if (_mapManager.MapId == mapId)
            {
                if (_gameManager.Player)
                {
                    _gameManager.Player.transform.position = position;
                    _gameManager.Player.transform.rotation = Quaternion.Euler(0, forwardAngle, 0);
                }

                _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
                {
                    Name = "完成加载",
                    Progress = 1f,
                });
            }
            else
            {
                // 清空游戏场景
                ClearScene();
                
                // 切换地图
                var mapSwitchProgress = 0.8f;
                await SwitchMapInternal(mapId, (name, index, steps) =>
                {
                    _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
                    {
                        Name = name,
                        Progress = mapSwitchProgress * index / steps,
                    });
                });

                // 最后加载玩家角色
                DebugUtil.LogCyan($"开始加载玩家: {_playerDataManager.Name}");
                _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
                {
                    Name = "加载玩家数据",
                    Progress = mapSwitchProgress,
                });
                await _gameManager.CreatePlayerCharacter(
                    prefabPath: "Player",
                    position: position,
                    forwardAngle: forwardAngle,
                    name: _playerDataManager.Name,
                    level: _playerDataManager.Level,
                    hp: _playerDataManager.Hp,
                    mp: _playerDataManager.Mp,
                    race: _playerDataManager.Race,
                    appearance: _playerDataManager.Appearance,
                    weapons: _packageManager.Weapons,
                    gears: _packageManager.Gears,
                    tags: new[] { "player" },
                    abilitySkills: _playerDataManager.Skills,
                    callback: null
                );

                _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
                {
                    Name = "完成加载",
                    Progress = 1f,
                });
            }
        }

        /// <summary>
        /// 场景加载存档的实际方法，将各个模块串联起来
        /// </summary>
        /// <param name="archiveData"></param>
        private async Task LoadInternal(ArchiveData archiveData)
        {
            _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
            {
                Name = "",
                Progress = 0f,
            });
            
            // 清空游戏场景
            ClearScene();

            var uiLoadCompleteProgress = 0.2f;
            var mapLoadCompleteProgress = 0.8f;

            // 预加载UI资源
            DebugUtil.LogCyan($"开始加载UI资源");
            _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
            {
                Name = "加载UI资源",
                Progress = 0f,
            });
            await PreloadUIPanel();

            // 切换地图
            await SwitchMapInternal(archiveData.player.map.id, (name, index, steps) =>
            {
                _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
                {
                    Name = name,
                    Progress = uiLoadCompleteProgress +
                               (mapLoadCompleteProgress - uiLoadCompleteProgress) * index / steps,
                });
            });

            // 最后加载玩家角色
            DebugUtil.LogCyan($"开始加载玩家: {_playerDataManager.Name}");
            _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
            {
                Name = "加载玩家数据",
                Progress = mapLoadCompleteProgress,
            });
            await _gameManager.CreatePlayerCharacter(
                prefabPath: "Player",
                position: _playerDataManager.Position,
                forwardAngle: Quaternion.Angle(Quaternion.AngleAxis(0, Vector3.up), _playerDataManager.Rotation),
                name: _playerDataManager.Name,
                level: _playerDataManager.Level,
                hp: _playerDataManager.Hp,
                mp: _playerDataManager.Mp,
                race: _playerDataManager.Race,
                appearance: _playerDataManager.Appearance,
                weapons: _packageManager.Weapons,
                gears: _packageManager.Gears,
                tags: new[] { "player" },
                abilitySkills: _playerDataManager.Skills,
                callback: null
            );

            _gameUIModel.LoadingData.SetValue(new GameLoadingUIData
            {
                Name = "完成加载",
                Progress = 1f,
            });
        }

        /// <summary>
        /// 预加载UI资源
        /// </summary>
        private async Task PreloadUIPanel()
        {
            // 批量UI加载任务
            var tasks = new List<Task>
            {
                LoadAlonePanelTask<GameAllyInfoPanel>(),
                LoadAlonePanelTask<GameEnemyBossInfoPanel>(),
                LoadAlonePanelTask<GameEnemyEliteInfoPanel>(),
                LoadAlonePanelTask<GameEnemyInfoPanel>(),
                LoadAlonePanelTask<GamePlayerInfoPanel>(),
                LoadShowingPanelTask<GameArchivePanel>(),
                LoadShowingPanelTask<GameDialoguePanel>(),
                LoadShowingPanelTask<GameMapPanel>(),
                LoadShowingPanelTask<GameMiniMapPanel>(),
                LoadShowingPanelTask<GameNotificationPanel>(),
                LoadShowingPanelTask<GamePackagePanel>(),
                LoadShowingPanelTask<GameQuestPanel>(),
                LoadShowingPanelTask<GameTradePanel>(),
                LoadShowingPanelTask<GameSystemCommandPanel>(),
                LoadShowingPanelTask<GameBattleCommandPanel>(),
                LoadShowingPanelTask<GameComboCommandPanel>(),
                LoadShowingPanelTask<GameCommonCommandPanel>(),
                LoadShowingPanelTask<GameSkillCommandPanel>(),
                LoadShowingPanelTask<GameCutscenePanel>(),
                LoadShowingPanelTask<GameDeathPanel>(),
                LoadShowingPanelTask<GameFullScreenEffectPanel>(),
                LoadShowingPanelTask<GameMenuPanel>(),
                LoadShowingPanelTask<GamePlayerResourcePanel>(),
                LoadShowingPanelTask<GameTipPanel>(),
            };
            await Task.WhenAll(tasks);

            Task LoadShowingPanelTask<T>() where T : BaseUGUIPanel
            {
                var source = new TaskCompletionSource<bool>();
                _panelManager.ToLoad<T>(() => source.SetResult(true), true);
                return source.Task;
            }

            Task LoadAlonePanelTask<T>() where T : BaseUGUIPanel
            {
                var source = new TaskCompletionSource<bool>();
                _panelManager.LoadAlone<T>((_) => source.SetResult(true), true);
                return source.Task;
            }
        }

        /// <summary>
        /// 加载地图以及其地图对象的实际方法
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="loadStepChanged"></param>
        private async Task SwitchMapInternal(int mapId, LoadStepChanged loadStepChanged)
        {
            DebugUtil.LogCyan("开始加载地图");
            loadStepChanged?.Invoke("加载地图", 0, 3);
            // 加载地图
            var mapInfoData = GameApplication.Instance.ExcelBinaryManager.GetContainer<MapInfoContainer>()
                .Data[mapId];
            await _mapManager.SwitchMap(mapInfoData.Id, mapInfoData.LoadPath);

            DebugUtil.LogCyan("开始加载角色");
            loadStepChanged?.Invoke("加载角色", 1, 3);
            // 根据地图角色表加载角色
            var characterLoadTasks = new List<Task>();
            var characterInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<CharacterInfoContainer>();
            var mapCharacterInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapCharacterInfoContainer>();
            mapCharacterInfoContainer.Data.ForEach(pair =>
            {
                // 过滤非当前地图角色和不可用角色
                if (pair.Value.MapId != mapId || !pair.Value.Enable)
                {
                    return;
                }

                // 过滤不可重加载且已被记录击杀的角色
                if (!pair.Value.Reload &&
                    _playerDataManager.TryGetKillMapCharacterRecord(pair.Value.MapId, pair.Value.Id, out _))
                {
                    return;
                }

                characterLoadTasks.Add(_gameManager.CreateCharacterByConfigurations(
                    characterInfoContainer.Data[pair.Value.PrototypeId],
                    pair.Value,
                    GameApplication.Instance.ExcelBinaryManager
                        .GetContainer<HumanoidAppearanceWeaponInfoContainer>(),
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>(),
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>(),
                    callback: character => { }
                ));
            });
            await Task.WhenAll(characterLoadTasks);

            DebugUtil.LogCyan("开始加载地图交互");
            loadStepChanged?.Invoke("加载地图交互", 2, 3);
            // 根据地图物品表加载物品
            var packageLoadTasks = new List<Task>();
            var mapPackageInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapPackageInfoContainer>();
            mapPackageInfoContainer.Data.ForEach(pair =>
            {
                // 过滤非当前地图物品、不可用物品和已交互物品
                if (pair.Value.MapId != mapId ||
                    !pair.Value.Enable ||
                    _mapManager.IsPackageInteracted(mapId, pair.Value.Id))
                {
                    return;
                }

                packageLoadTasks.Add(_gameManager.CreatePackage(
                    "Package",
                    new Vector3(pair.Value.PositionX, pair.Value.PositionY, pair.Value.PositionZ),
                    Quaternion.AngleAxis(pair.Value.ForwardAngle, Vector3.up),
                    pair.Value.Id,
                    pair.Value.PrototypeId,
                    pair.Value.Number
                ));
            });
            await Task.WhenAll(packageLoadTasks);
            // 根据地图休息点表加载休息点
            var restSpotLoadTasks = new List<Task>();
            var mapRestSpotInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapRestSpotInfoContainer>();
            mapRestSpotInfoContainer.Data.ForEach(pair =>
            {
                // 过滤非当前地图休息点和不可用休息点
                if (pair.Value.MapId != mapId || !pair.Value.Enable)
                {
                    return;
                }

                restSpotLoadTasks.Add(_gameManager.CreateRestSpot(
                    "Campfire",
                    new Vector3(pair.Value.PositionX, pair.Value.PositionY, pair.Value.PositionZ),
                    Quaternion.AngleAxis(pair.Value.ForwardAngle, Vector3.up),
                    pair.Value.Id
                ));
            });
            await Task.WhenAll(restSpotLoadTasks);
        }

        /// <summary>
        /// 刷新地图上的可重加载的死亡角色和消失物品
        /// </summary>
        /// <param name="mapId"></param>
        private void RefreshMapInternal(int mapId)
        {
            DebugUtil.LogCyan("开始刷新地图");
            // 根据地图角色表加载角色
            var characterInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<CharacterInfoContainer>();
            var mapCharacterInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapCharacterInfoContainer>();
            mapCharacterInfoContainer.Data.ForEach(pair =>
            {
                // 过滤非当前地图角色、不可用和不可重加载角色
                if (pair.Value.MapId != mapId || !pair.Value.Enable || !pair.Value.Reload)
                {
                    return;
                }

                // 过滤仍在地图上存在的角色
                if (!_gameManager.IsCharacterDeadOrNonexistent(pair.Value.Id))
                {
                    return;
                }

                // 异步加载角色
                _gameManager.CreateCharacterByConfigurations(
                    characterInfoContainer.Data[pair.Value.PrototypeId],
                    pair.Value,
                    GameApplication.Instance.ExcelBinaryManager
                        .GetContainer<HumanoidAppearanceWeaponInfoContainer>(),
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>(),
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>(),
                    callback: character => { }
                );
            });

            // 根据地图物品表加载物品
            var mapPackageInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapPackageInfoContainer>();
            mapPackageInfoContainer.Data.ForEach(pair =>
            {
                // 过滤非当前地图物品、不可用物品
                if (pair.Value.MapId != mapId ||
                    !pair.Value.Enable)
                {
                    return;
                }

                // 过滤仍在地图上存在的物品
                if (!_gameManager.IsPackageNonexistent(pair.Value.Id))
                {
                    return;
                }

                // 异步加载物品
                _gameManager.CreatePackage(
                    "Package",
                    new Vector3(pair.Value.PositionX, pair.Value.PositionY, pair.Value.PositionZ),
                    Quaternion.AngleAxis(pair.Value.ForwardAngle, Vector3.up),
                    pair.Value.Id,
                    pair.Value.PrototypeId,
                    pair.Value.Number
                );
            });
        }

        private void ClearScene()
        {
            DebugUtil.LogGrey("清除游戏场景");
            _timelineManager.StopAllTimelines();
            _tradeManager.ClearAllTrades();
            _battleManager.ClearAllBattles();
            _gameManager.ClearScene();
            _cameraManager.ClearAllCameras();
            _mapManager.DestroyCurrentMap();
            _audioManager.ClearAllBackgroundMusic();
            _audioManager.ClearSounds();
        }
    }
}