using System.Collections.Generic;
using Buff;
using Camera;
using CollideDetection;
using Common;
using Damage;
using Dialogue;
using Features.Game.UI;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Timeline;
using Framework.Common.UI.Panel;
using Framework.Common.UI.PopupText;
using Humanoid.Weapon;
using Inputs;
using Map;
using Package;
using Player;
using Quest;
using Skill;
using Trade;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace Features.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private Canvas panelCanvas;

        private UGUIPanelManager _panelManager;

        protected override void Awake()
        {
            _panelManager = new UGUIPanelManager(
                uiCanvas: panelCanvas,
                loadPanelAsset: (panelName, _, _, callback) =>
                {
                    var path = panelName;
                    GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>(path, callback);
                },
                unloadPanelAsset: (panelName, _, callback) =>
                {
                    var path = panelName;
                    GameApplication.Instance?.AddressablesManager.ReleaseAsset<GameObject>(path, callback);
                });
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterComponent(_panelManager);

            builder.RegisterEntryPoint<GameUIModel>(Lifetime.Singleton).As<IGameUIModel>().AsSelf();
            builder.RegisterEntryPoint<CameraModel>(Lifetime.Singleton).As<ICameraModel>().AsSelf();
            builder.RegisterEntryPoint<GameModel>(Lifetime.Singleton).As<IGameModel>().AsSelf();

            builder.RegisterComponentInHierarchy<EventSystem>();
            builder.RegisterComponentInHierarchy<CameraManager>();
            builder.RegisterComponentInHierarchy<DialogueManager>();
            builder.RegisterComponentInHierarchy<PlayerInputManager>();
            builder.RegisterComponentInHierarchy<PopupTextManager>();
            builder.RegisterComponentInHierarchy<DamageManager>();
            builder.RegisterComponentInHierarchy<InputInfoManager>();
            builder.RegisterComponentInHierarchy<GameAudioManager>().As<AudioManager>();
            builder.RegisterComponentInHierarchy<TimelineManager>();
            builder.RegisterComponentInHierarchy<AlgorithmManager>();
            builder.RegisterComponentInHierarchy<GameManager>();
            builder.RegisterComponentInHierarchy<BuffManager>();
            builder.RegisterComponentInHierarchy<AtbManager>();
            builder.RegisterComponentInHierarchy<PackageManager>();
            builder.RegisterComponentInHierarchy<PlayerDataManager>();
            builder.RegisterComponentInHierarchy<BattleManager>();
            builder.RegisterComponentInHierarchy<QuestManager>();
            builder.RegisterComponentInHierarchy<TradeManager>();
            builder.RegisterComponentInHierarchy<MapManager>();
            builder.RegisterComponentInHierarchy<CollideDetectionManager>();
            builder.RegisterComponentInHierarchy<SkillManager>();
            builder.RegisterComponentInHierarchy<HumanoidWeaponManager>();

            builder.RegisterComponentInHierarchy<MainCameraController>();
            builder.RegisterComponentInHierarchy<UICameraController>();
            builder.RegisterComponentInHierarchy<MiniMapCameraController>();

            builder.RegisterComponentInHierarchy<GameSystemUIController>();
            builder.RegisterComponentInHierarchy<GamePlayerUIController>();
            builder.RegisterComponentInHierarchy<GameBattleUIController>();

            builder.RegisterComponentInHierarchy<GameScene>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _panelManager.Destroy();
        }
    }
}