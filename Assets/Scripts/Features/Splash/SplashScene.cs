using System;
using Features.SceneGoto;
using Framework.Common.UI.Panel;
using UnityEngine;
using VContainer;

namespace Features.Splash
{
    public class SplashScene : BaseScene
    {
        [SerializeField] private RectTransform placeholder;
        [SerializeField] private float splashShowTime = 1f;
        [SerializeField] private BaseSceneGotoSO sceneGotoSO;

        [Inject] private UGUIPanelManager _panelManager;

        protected override void OnAwake()
        {
            base.OnAwake();
            Time.timeScale = 1f;
        }

        private void Start()
        {
            // 回收资源并执行垃圾收集
            Resources.UnloadUnusedAssets();
            GC.Collect();
            // 延迟跳转到主场景
            Invoke(nameof(GotoScene), splashShowTime);
        }

        private void GotoScene()
        {
            sceneGotoSO.Goto(null);
        }
    }
}