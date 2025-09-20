using System.Collections;
using System.Collections.Generic;
using Common;
using Damage;
using Features.Appearance;
using Features.Appearance.Model;
using Features.SceneGoto;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Resource;
using Framework.Common.UI;
using Framework.Common.UI.Panel;
using Framework.Common.UI.PopupText;
using Humanoid;
using Inputs;
using Player;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace Features.Appearance
{
    public class AppearanceLifetimeScope : LifetimeScope
    {
        [SerializeField] private Canvas uiCanvas;
        
        private UGUIPanelManager _panelManager;

        protected override void Awake()
        {
            _panelManager = new UGUIPanelManager(
                uiCanvas: uiCanvas,
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

            builder.RegisterEntryPoint<AppearanceModel>().As<IAppearanceModel>().AsSelf();

            builder.RegisterComponentInHierarchy<AppearanceController>();
            builder.RegisterComponentInHierarchy<PlayerInputManager>();
            builder.RegisterComponentInHierarchy<EventSystem>();
            builder.RegisterComponentInHierarchy<GameAudioManager>().As<AudioManager>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _panelManager.Destroy();
        }
    }
}