using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Character.Data.Extension;
using Features.Splash.UI;
using Framework.Common.Audio;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model.Data;
using Humanoid.Weapon.Data;
using Humanoid.Weapon.SO;
using Map.Data;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Features.Main
{
    public class MainScene : BaseScene
    {
        [Title("二进制文件路径")] [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string modelInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string appearanceWeaponInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string appearanceGearInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string packageInfoBinaryPath = "";

        [SerializeField, FilePath(ParentFolder = "Assets/StreamingAssets")]
        private string mapInfoBinaryPath = "";

        [Title("模型配置")] [SerializeField] private HumanoidCharacterObject modelCharacter;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private RuntimeAnimatorController maleIdleAnimatorController;
        [SerializeField] private RuntimeAnimatorController femaleIdleAnimatorController;

        [Title("背景音乐")] [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

        [Inject] private AudioManager _audioManager;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private IObjectResolver _objectResolver;

        protected override void OnAwake()
        {
            base.OnAwake();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
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
            _audioManager?.RemoveBackgroundMusic(backgroundMusic);
            if (SceneManager.Instance)
            {
                SceneManager.Instance.OnSceneStartLoad -= HandleSceneStartLoad;
                SceneManager.Instance.OnSceneCompleteLoad -= HandleSceneCompleteLoad;
            }
        }

        private async void InitScene()
        {
            _audioManager.AddBackgroundMusic(backgroundMusic, 0, musicVolume);
            
            await Task.Run(() =>
            {
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidModelInfoContainer, HumanoidModelInfoData>(
                        Path.Combine(Application.streamingAssetsPath, modelInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidAppearanceWeaponInfoContainer, HumanoidAppearanceWeaponInfoData>(
                        Path.Combine(Application.streamingAssetsPath, appearanceWeaponInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<HumanoidAppearanceGearInfoContainer, HumanoidAppearanceGearInfoData>(
                        Path.Combine(Application.streamingAssetsPath, appearanceGearInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<PackageInfoContainer, PackageInfoData>(
                        Path.Combine(Application.streamingAssetsPath, packageInfoBinaryPath));
                GameApplication.Instance.ExcelBinaryManager
                    .LoadContainer<MapInfoContainer, MapInfoData>(
                        Path.Combine(Application.streamingAssetsPath, mapInfoBinaryPath));
            });

            var newestArchiveData = GameApplication.Instance.ArchiveManager.GetNewestArchive();
            if (newestArchiveData != null)
            {
                // 获取物品组列表，并从中取出武器和装备列表
                var packageGroups = newestArchiveData.package.packages.Select(packageData =>
                    packageData.ToPackageGroup(
                        GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>(),
                        HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                        _ => new HumanoidWeaponAttackConfigData
                        {
                            supportAttack = false,
                            attackAbility = new()
                        },
                        _ => new HumanoidWeaponDefenceConfigData
                        {
                            supportDefend = false,
                            defenceAbility = new()
                        },
                        GameApplication.Instance.ExcelBinaryManager
                            .GetContainer<HumanoidAppearanceWeaponInfoContainer>(),
                        GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>())
                ).ToList();
                var weapons = new HashSet<PackageGroup>();
                var gears = new HashSet<PackageGroup>();
                packageGroups.ForEach(packageGroup =>
                {
                    switch (packageGroup.Data)
                    {
                        case PackageWeaponData weaponData:
                        {
                            if (packageGroup.GroupId == newestArchiveData.package.leftHandWeaponGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.rightHandWeaponGroupId)
                            {
                                weapons.Add(packageGroup);
                            }

                            break;
                        }
                        case PackageGearData gearData:
                        {
                            if (packageGroup.GroupId == newestArchiveData.package.headGearGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.torsoGearGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.leftArmGearGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.rightArmGearGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.leftLegGearGroupId ||
                                packageGroup.GroupId == newestArchiveData.package.rightLegGearGroupId)
                            {
                                gears.Add(packageGroup);
                            }

                            break;
                        }
                    }
                });

                modelCharacter.gameObject.SetActive(true);
                modelCharacter.Init();
                modelCharacter.SetHumanoidCharacterParameters(
                    newestArchiveData.player.race,
                    weapons.ToList(),
                    gears.ToList()
                );
                modelCharacter.AppearanceAbility?.SetAppearance(newestArchiveData.player.appearance.ToAppearanceData(
                    GameApplication.Instance.ExcelBinaryManager
                        .GetContainer<HumanoidModelInfoContainer>()));
                modelAnimator.runtimeAnimatorController =
                    newestArchiveData.player.race == HumanoidCharacterRace.HumanFemale
                        ? femaleIdleAnimatorController
                        : maleIdleAnimatorController;
            }
            else
            {
                modelCharacter.gameObject.SetActive(false);
            }

            _panelManager.Show<MainHomepagePanel>(
                UGUIPanelLayer.Middle,
                panel => { _objectResolver.Inject(panel); },
                null
            );
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
    }
}