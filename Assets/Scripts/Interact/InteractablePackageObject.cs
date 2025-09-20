using System;
using System.Collections.Generic;
using Character;
using Character.Data.Extension;
using Common;
using Features.Game;
using Features.Game.UI;
using Framework.Common.Audio;
using Humanoid.Data;
using Humanoid.Model;
using Humanoid.Model.Data;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using Map;
using Package;
using Package.Data;
using Package.Data.Extension;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Interact
{
    [RequireComponent(typeof(GUIContentShowBehaviour))]
    public class InteractablePackageObject : InteractableObject
    {
        [Title("标签配置")] [SerializeField] private GUIStyle labelStyle = new();

        [Title("物品位置")] [SerializeField] private Vector3 weaponOffset = Vector3.zero;
        [SerializeField] private Vector3 gearOffset = Vector3.zero;
        [SerializeField] private Vector3 itemOffset = Vector3.up;
        [SerializeField] private Vector3 materialOffset = Vector3.up;

        [FormerlySerializedAs("gearPrefab")] [Title("外观配置")] [SerializeField]
        private GameObject gearModelPrefab;

        [SerializeField] private Material gearMaterial;

        [FormerlySerializedAs("itemPrefab")] [SerializeField]
        private GameObject itemDefaultPrefab;

        [FormerlySerializedAs("materialPrefab")] [SerializeField]
        private GameObject materialDefaultPrefab;

        [Title("音效配置")] [SerializeField] private AudioClip weaponGetAudioClip;
        [SerializeField] private AudioClip gearGetAudioClip;
        [SerializeField] private AudioClip itemGetAudioClip;
        [SerializeField] private AudioClip materialGetAudioClip;
        [SerializeField] [Range(0f, 1f)] private float audioVolume = 1f;

        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;
        [Inject] private HumanoidWeaponManager _weaponManager;
        [Inject] private PackageManager _packageManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private GameScene _gameScene;

        private PackageInfoData _packageInfoData;
        private int _id = -1; // 地图物品id
        private int _packageId = -1; // 物品id
        private int _number = 0; // 数量

        [Title("运行时数据")]
        [ShowInInspector]
        public PackageType PackageType => _packageInfoData?.GetPackageType() ?? PackageType.Material;

        [ShowInInspector] public int Id => _id;
        [ShowInInspector] public int PackageId => _packageId;
        [ShowInInspector] public int Number => _number;

        private GameObject _model;

        private HumanoidWeaponCreator _weaponCreator;
        private HumanoidModelLoader _gearLoader;

        private GUIContentShowBehaviour _contentShowBehaviour;

        private void Awake()
        {
            // 获取GUI显示组件
            _contentShowBehaviour = GetComponent<GUIContentShowBehaviour>();
            _contentShowBehaviour.AllowGUIShow =
                _gameUIModel.AllowGUIShowing().HasValue() && _gameUIModel.AllowGUIShowing().Value;
        }

        private void Update()
        {
            _contentShowBehaviour.enabled = visible;
        }

        private void OnDestroy()
        {
            // 销毁武器创建类
            _weaponCreator?.Destroy();
            // 销毁装备模型加载器
            _gearLoader?.Destroy();
        }

        public void Init(int id, int packageId, int number = 1)
        {
            if (_model)
            {
                GameObject.Destroy(_model);
            }

            var packageInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
            if (!packageInfoContainer.Data.TryGetValue(packageId, out _packageInfoData))
            {
                throw new Exception(
                    $"The PackageInfoData whose id is {packageId} is not existed in PackageInfoContainer");
            }

            _id = id;
            _packageId = packageId;
            _number = number;

            switch (_packageInfoData.GetPackageType())
            {
                case PackageType.Weapon:
                {
                    var weaponInfoContainer =
                        GameApplication.Instance.ExcelBinaryManager
                            .GetContainer<HumanoidAppearanceWeaponInfoContainer>();
                    // 初始化武器创建类
                    _weaponCreator =
                        new HumanoidWeaponCreator(HumanoidWeaponSingletonConfigSO.Instance.GetWeaponAppearanceMaterial);
                    // 创建武器物体
                    _weaponCreator.CreateWeaponModelAsync(weaponInfoContainer.Data[_packageInfoData.WeaponAppearanceId],
                        weapon =>
                        {
                            _model = weapon;
                            _model.transform.parent = transform;
                            _model.transform.localPosition = weaponOffset;
                        });
                }
                    break;
                case PackageType.Gear:
                {
                    var gearInfoContainer =
                        GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();
                    // 创建装备物体
                    _model = GameObject.Instantiate(gearModelPrefab);
                    _model.transform.parent = transform;
                    _model.transform.localPosition = gearOffset;
                    _gearLoader = new HumanoidModelLoader(_model, gearMaterial);
                    var modelInfoContainer =
                        GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidModelInfoContainer>();
                    var gearAppearance = gearInfoContainer.Data[_packageInfoData.GearAppearanceId]
                        .ToGearAppearance(modelInfoContainer);
                    var appearanceData = new HumanoidAppearanceData
                    {
                        Body = new HumanoidBodyAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultBodyColor,
                        },
                        HeadGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                        TorsoGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                        LeftArmGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                        RightArmGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                        LeftLegGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                        RightLegGear = new HumanoidGearAppearance
                        {
                            Models = new List<HumanoidAppearanceModel>(),
                            Color = HumanoidAppearanceColor.DefaultGearColor,
                        },
                    };
                    switch (_packageInfoData.GetPackageGearPart())
                    {
                        case HumanoidAppearanceGearPart.Head:
                        {
                            appearanceData.HeadGear = gearAppearance;
                        }
                            break;
                        case HumanoidAppearanceGearPart.Torso:
                        {
                            appearanceData.TorsoGear = gearAppearance;
                        }
                            break;
                        case HumanoidAppearanceGearPart.LeftArm:
                        {
                            appearanceData.LeftArmGear = gearAppearance;
                        }
                            break;
                        case HumanoidAppearanceGearPart.RightArm:
                        {
                            appearanceData.RightArmGear = gearAppearance;
                        }
                            break;
                        case HumanoidAppearanceGearPart.LeftLeg:
                        {
                            appearanceData.LeftArmGear = gearAppearance;
                        }
                            break;
                        case HumanoidAppearanceGearPart.RightLeg:
                        {
                            appearanceData.RightLegGear = gearAppearance;
                        }
                            break;
                    }

                    appearanceData.SynchronizeAppearanceModel(_gearLoader);
                }
                    break;
                case PackageType.Item:
                {
                    // 如果道具不存在外观预设体，则使用默认预设体，否则通过Addressables加载外观预设体
                    if (string.IsNullOrEmpty(_packageInfoData.ItemAppearancePrefab))
                    {
                        // 创建道具默认预设体物体
                        _model = GameObject.Instantiate(itemDefaultPrefab);
                        _model.transform.parent = transform;
                        _model.transform.localPosition = itemOffset;
                    }
                    else
                    {
                        // 创建道具外观预设体物体
                        _gameScene.LoadAssetAsyncTemporary<GameObject>(
                            _packageInfoData.ItemAppearancePrefab,
                            prefab =>
                            {
                                prefab ??= itemDefaultPrefab;
                                _model = GameObject.Instantiate(prefab);
                                _model!.transform.parent = transform;
                                _model.transform.localPosition = itemOffset;
                            });
                    }
                }
                    break;
                case PackageType.Material:
                {
                    // 如果材料不存在外观预设体，则使用默认预设体，否则通过Addressables加载外观预设体
                    if (string.IsNullOrEmpty(_packageInfoData.MaterialAppearancePrefab))
                    {
                        // 创建材料默认预设体物体
                        _model = GameObject.Instantiate(materialDefaultPrefab);
                        _model.transform.parent = transform;
                        _model.transform.localPosition = materialOffset;
                    }
                    else
                    {
                        // 创建材料外观预设体物体
                        _gameScene.LoadAssetAsyncTemporary<GameObject>(
                            _packageInfoData.MaterialAppearancePrefab,
                            prefab =>
                            {
                                prefab ??= materialDefaultPrefab;
                                _model = GameObject.Instantiate(prefab);
                                _model!.transform.parent = transform;
                                _model.transform.localPosition = materialOffset;
                            });
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 设置GUI显示内容
            _contentShowBehaviour.SetContent(new GUIShowContent
            {
                contentType = GUIShowContentType.Label,
                width = 0,
                height = 0,
                labelContent = _packageInfoData.Name,
                labelStyle = labelStyle,
            });
        }

        public override bool AllowInteract(GameObject target)
        {
            if (!gameObject || _packageId == -1)
            {
                return false;
            }

            var character = target.GetComponent<CharacterObject>();
            if (character != _gameManager.Player || character.Parameters.dead ||
                character.Parameters.battleState == CharacterBattleState.Battle)
            {
                return false;
            }

            return true;
        }

        public override void Interact(GameObject target)
        {
            // 如果未添加到物品管理器，就不视为成功交互
            if (!_packageManager.AddPackage(_packageId, _number, true))
            {
                return;
            }

            // 销毁物品对象并记录已获得
            _gameManager.DestroyPackage(this);
            _mapManager.RecordPackageInteracted(_id);

            // 获取对应类型的获得音效
            var audioClip = _packageInfoData.GetPackageType() switch
            {
                PackageType.Weapon => weaponGetAudioClip,
                PackageType.Gear => gearGetAudioClip,
                PackageType.Item => itemGetAudioClip,
                PackageType.Material => materialGetAudioClip,
                _ => materialGetAudioClip
            };
            var targetCharacter = target.GetComponent<CharacterObject>();
            targetCharacter.AudioAbility?.PlaySound(audioClip, false, audioVolume);
        }

        public override string Tip(GameObject target)
        {
            var packageInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
            if (!packageInfoContainer.Data.TryGetValue(_packageId, out var packageInfoData))
            {
                throw new Exception(
                    $"The PackageInfoData whose id is {_packageId} is not existed in PackageInfoContainer");
            }

            return packageInfoData.GetPackageType() switch
            {
                PackageType.Weapon => "拾取武器",
                PackageType.Gear => "拾取装备",
                PackageType.Item => "拾取道具",
                PackageType.Material => "拾取材料",
                _ => ""
            };
        }
    }
}