using System.Collections.Generic;
using Common;
using Features.SceneGoto;
using Framework.Common.Audio;
using Framework.Common.UI.Panel;
using Humanoid.Weapon;
using Inputs;
using Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace Features.Main
{
    public class MainLifetimeScope : LifetimeScope
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
            builder.RegisterComponentInHierarchy<EventSystem>();
            builder.RegisterComponentInHierarchy<PlayerInputManager>();
            builder.RegisterComponentInHierarchy<InputInfoManager>();
            builder.RegisterComponentInHierarchy<AlgorithmManager>();
            builder.RegisterComponentInHierarchy<GameAudioManager>().As<AudioManager>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _panelManager.Destroy();
        }
    }
}