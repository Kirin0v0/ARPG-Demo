using System;
using System.IO;
using System.Threading.Tasks;
using Character.Data.Extension;
using Features.Appearance.Data;
using Features.Appearance.Model;
using Features.Appearance.UI;
using Features.Splash.UI;
using Framework.Common.Audio;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Framework.Core.Lifecycle;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model;
using Humanoid.Model.Data;
using Map.Data;
using Rendering;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace Features.Appearance
{
    public class AppearanceScene : BaseScene
    {
        [Title("二进制文件路径")] [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string modelInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string defaultModelInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapInfoBinaryPath = "";

        [Title("模型配置")] [SerializeField] private GameObject model;
        [SerializeField] private Material material;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private RuntimeAnimatorController maleIdleAnimatorController;
        [SerializeField] private RuntimeAnimatorController femaleIdleAnimatorController;

        [Title("背景音乐")] [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

        [Inject] private AudioManager _audioManager;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private AppearanceController _appearanceController;
        [Inject] private EventSystem _eventSystem;
        [Inject] private IAppearanceModel _appearanceModel;
        [Inject] private IObjectResolver _objectResolver;

        private HumanoidModelLoader _modelLoader;

        protected override void OnAwake()
        {
            base.OnAwake();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            // 监听种族和外观变化
            _appearanceModel.GetSelectedRace().Observe(gameObject.GetMonoLifecycle(), HandleSelectedRace);
            _appearanceModel.GetAppearance().Observe(gameObject.GetMonoLifecycle(), HandleAppearanceChanged);
            // 监听场景加载
            SceneManager.Instance.OnSceneStartLoad += HandleSceneStartLoad;
            SceneManager.Instance.OnSceneCompleteLoad += HandleSceneCompleteLoad;
            // 初始化模型加载器
            _modelLoader = new HumanoidModelLoader(model, material);
            // // 添加模型描边
            // RenderingUtil.AddRenderingLayerMask(
            //     model,
            //     GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
            //     GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask
            // );
        }

        private void Start()
        {
            // 初始化场景
            InitScene();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _audioManager.RemoveBackgroundMusic(backgroundMusic);
            _modelLoader.Destroy();
            if (SceneManager.Instance)
            {
                SceneManager.Instance.OnSceneStartLoad -= HandleSceneStartLoad;
                SceneManager.Instance.OnSceneCompleteLoad -= HandleSceneCompleteLoad;
            }
        }

        private void HandleSelectedRace(HumanoidCharacterRace race)
        {
            switch (race)
            {
                case HumanoidCharacterRace.HumanMale:
                    modelAnimator.runtimeAnimatorController = maleIdleAnimatorController;
                    break;
                case HumanoidCharacterRace.HumanFemale:
                    modelAnimator.runtimeAnimatorController = femaleIdleAnimatorController;
                    break;
            }
        }

        private void HandleAppearanceChanged(HumanoidAppearanceData data)
        {
            data.SynchronizeAppearanceModel(_modelLoader);
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

        private async void InitScene()
        {
            // 播放背景音乐
            _audioManager.AddBackgroundMusic(backgroundMusic, 0, musicVolume);
            
            await Task.Run(() =>
            {
                // 提前加载二进制数据
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidModelInfoContainer, HumanoidModelInfoData>(
                        Path.Combine(Application.streamingAssetsPath, modelInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<AppearanceDefaultModelInfoContainer, AppearanceDefaultModelInfoData>(
                        Path.Combine(Application.streamingAssetsPath, defaultModelInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager.LoadContainer<MapInfoContainer, MapInfoData>(
                    Path.Combine(Application.streamingAssetsPath, mapInfoBinaryPath));
            });

            // 设置模型外观
            _appearanceModel.SelectRace(HumanoidCharacterRace.HumanMale);
            _appearanceModel.SetConfigurationColor(HumanoidAppearanceColor.DefaultBodyColor);

            // 显示编辑面板
            _panelManager.Show<AppearanceEditPanel>(
                beforeShowCallback: panel => { _objectResolver.Inject(panel); }
            );
        }
    }
}