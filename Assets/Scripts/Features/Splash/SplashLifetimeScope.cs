using System.Collections.Generic;
using Features.SceneGoto;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Splash
{
    public class SplashLifetimeScope : LifetimeScope
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
                    GameApplication.Instance.AddressablesManager.ReleaseAsset<GameObject>(path, callback);
                });
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(_panelManager);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _panelManager.Destroy();
        }
    }
}