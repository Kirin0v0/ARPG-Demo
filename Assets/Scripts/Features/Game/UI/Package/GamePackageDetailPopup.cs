using System;
using System.Text;
using Buff;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Package;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Sirenix.OdinInspector;
using Skill;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Features.Game.UI.Package
{
    public class GamePackageDetailPopup : BasePopup<PackageGroup>
    {
        [Title("UI关联")] [SerializeField] private TextMeshProUGUI textDetailName;
        [SerializeField] private Image imgDetailThumbnail;
        [SerializeField] private TextMeshProUGUI textDetailIntroduction;
        [SerializeField] private TextMeshProUGUI textDetailType;
        [SerializeField] private TextMeshProUGUI textDetailNumber;
        [SerializeField] private RectTransform detailWeaponBar;
        [SerializeField] private TextMeshProUGUI textDetailWeaponType;
        [SerializeField] private TextMeshProUGUI textDetailWeaponDefenceDamageMultiplier;
        [SerializeField] private RectTransform detailGearBar;
        [SerializeField] private TextMeshProUGUI textDetailGearPart;
        [SerializeField] private TextMeshProUGUI textDetailGearRaces;
        [SerializeField] private TextMeshProUGUI textDetailSkills;
        [SerializeField] private TextMeshProUGUI textDetailBuff;
        [SerializeField] private TextMeshProUGUI textDetailMaxHp;
        [SerializeField] private TextMeshProUGUI textDetailMaxMp;
        [SerializeField] private TextMeshProUGUI textDetailStamina;
        [SerializeField] private TextMeshProUGUI textDetailStrength;
        [SerializeField] private TextMeshProUGUI textDetailMagic;
        [SerializeField] private TextMeshProUGUI textDetailReaction;
        [SerializeField] private TextMeshProUGUI textDetailLuck;
        [SerializeField] private TextMeshProUGUI textDetailHp;
        [SerializeField] private TextMeshProUGUI textDetailMp;

        [Title("展示信息")] [SerializeField] private bool showPackageNumber = false;
        [SerializeField] private bool showEquipStateByPackage = false;

        private SkillManager _skillManager;
        private BuffManager _buffManager;
        private PackageManager _packageManager;
        private GameScene _gameScene;

        public void Init(SkillManager skillManager, BuffManager buffManager, PackageManager packageManager, GameScene gameScene)
        {
            _skillManager = skillManager;
            _buffManager = buffManager;
            _packageManager = packageManager;
            _gameScene = gameScene;
        }

        protected override void UpdateContent()
        {
            // 重置所有UI
            textDetailName.gameObject.SetActive(false);
            imgDetailThumbnail.gameObject.SetActive(false);
            textDetailIntroduction.gameObject.SetActive(false);
            textDetailType.gameObject.SetActive(false);
            textDetailNumber.gameObject.SetActive(false);
            detailWeaponBar.gameObject.SetActive(false);
            textDetailWeaponType.gameObject.SetActive(false);
            textDetailWeaponDefenceDamageMultiplier.gameObject.SetActive(false);
            detailGearBar.gameObject.SetActive(false);
            textDetailGearPart.gameObject.SetActive(false);
            textDetailGearRaces.gameObject.SetActive(false);
            textDetailSkills.gameObject.SetActive(false);
            textDetailBuff.gameObject.SetActive(false);
            textDetailMaxHp.gameObject.SetActive(false);
            textDetailMaxMp.gameObject.SetActive(false);
            textDetailStamina.gameObject.SetActive(false);
            textDetailStrength.gameObject.SetActive(false);
            textDetailMagic.gameObject.SetActive(false);
            textDetailReaction.gameObject.SetActive(false);
            textDetailLuck.gameObject.SetActive(false);
            textDetailHp.gameObject.SetActive(false);
            textDetailMp.gameObject.SetActive(false);

            if (Data == null)
            {
                return;
            }

            var packageData = Data.Data;

            #region 共通UI

            textDetailName.gameObject.SetActive(true);
            textDetailName.text = packageData.Name;
            imgDetailThumbnail.gameObject.SetActive(true);
            _gameScene.LoadAssetAsyncTemporary<SpriteAtlas>(packageData.ThumbnailAtlas,
                handle => { imgDetailThumbnail.sprite = handle.GetSprite(packageData.ThumbnailName); }
            );
            textDetailIntroduction.gameObject.SetActive(true);
            textDetailIntroduction.text = packageData.Introduction;
            textDetailType.gameObject.SetActive(true);
            var equipped = showEquipStateByPackage
                ? _packageManager.IsPackageEquipped(packageData.Id)
                : _packageManager.IsGroupEquipped(Data.GroupId);
            textDetailType.text = packageData.GetPackageType() switch
            {
                PackageType.Weapon => "类型：武器\n" + (equipped ? "（装备中）" : "（未装备）"),
                PackageType.Gear => "类型：装备\n" + (equipped ? "（装备中）" : "（未装备）"),
                PackageType.Item => "类型：道具",
                PackageType.Material => "类型：材料",
                _ => "未知类型"
            };
            if (showPackageNumber)
            {
                textDetailNumber.gameObject.SetActive(true);
                textDetailNumber.text = $"数量：{Data.Number}/{packageData.GroupMaximum}";
            }

            #endregion

            switch (packageData)
            {
                case PackageWeaponData weaponData:
                {
                    #region 武器UI

                    detailWeaponBar.gameObject.SetActive(true);

                    var typeTextBuilder = new StringBuilder();
                    switch (weaponData.Type)
                    {
                        case HumanoidWeaponType.Sword:
                        {
                            typeTextBuilder.Append("剑类");
                        }
                            break;
                        case HumanoidWeaponType.Shield:
                        {
                            typeTextBuilder.Append("盾类");
                        }
                            break;
                        case HumanoidWeaponType.Katana:
                        {
                            typeTextBuilder.Append("刀类");
                        }
                            break;
                    }

                    switch (weaponData.Equipment.tag)
                    {
                        case HumanoidWeaponTag.LeftHandWeapon:
                        {
                            typeTextBuilder.Append("左手");
                        }
                            break;
                        case HumanoidWeaponTag.RightHandWeapon:
                        {
                            typeTextBuilder.Append("右手");
                        }
                            break;
                        case HumanoidWeaponTag.TwoHandsWeapon:
                        {
                            typeTextBuilder.Append("双手");
                        }
                            break;
                    }

                    typeTextBuilder.Append("武器");
                    textDetailWeaponType.gameObject.SetActive(true);
                    textDetailWeaponType.text = typeTextBuilder.ToString();

                    if (weaponData.Defence.supportDefend)
                    {
                        if (weaponData.DefenceDamageMultiplier < 1f)
                        {
                            var defenceDamageMultiplierPercentage =
                                Mathf.RoundToInt(weaponData.DefenceDamageMultiplier * 100f);
                            textDetailWeaponDefenceDamageMultiplier.gameObject.SetActive(true);
                            textDetailWeaponDefenceDamageMultiplier.text =
                                $"防御减伤至{defenceDamageMultiplierPercentage.ToString("D2")}%";
                        }
                    }

                    if (weaponData.Skills.Count != 0)
                    {
                        textDetailSkills.gameObject.SetActive(true);
                        var skillStringBuilder = new StringBuilder();
                        weaponData.Skills.ForEach(skill =>
                        {
                            if (_skillManager.TryGetSkillPrototype(skill, out var skillPrototype))
                            {
                                if (skillStringBuilder.Length == 0)
                                {
                                    skillStringBuilder.AppendLine("配置能力");
                                }

                                skillStringBuilder.AppendLine($"【{skillPrototype.Name}】");
                            }
                        });
                        textDetailSkills.text = skillStringBuilder.ToString();
                    }

                    if (weaponData.MaxHp != default)
                    {
                        textDetailMaxHp.gameObject.SetActive(true);
                        textDetailMaxHp.text = "最大生命值 " + (weaponData.MaxHp > 0 ? "+" : "") + weaponData.MaxHp;
                    }

                    if (weaponData.MaxMp != default)
                    {
                        textDetailMaxMp.gameObject.SetActive(true);
                        textDetailMaxMp.text = "最大法力值 " + (weaponData.MaxMp > 0 ? "+" : "") + weaponData.MaxMp;
                    }

                    if (weaponData.Stamina != default)
                    {
                        textDetailStamina.gameObject.SetActive(true);
                        textDetailStamina.text = "耐力 " + (weaponData.Stamina > 0 ? "+" : "") + weaponData.Stamina;
                    }

                    if (weaponData.Strength != default)
                    {
                        textDetailStrength.gameObject.SetActive(true);
                        textDetailStrength.text = "力量 " + (weaponData.Strength > 0 ? "+" : "") + weaponData.Strength;
                    }

                    if (weaponData.Magic != default)
                    {
                        textDetailMagic.gameObject.SetActive(true);
                        textDetailMagic.text = "魔力 " + (weaponData.Magic > 0 ? "+" : "") + weaponData.Magic;
                    }

                    if (weaponData.Reaction != default)
                    {
                        textDetailReaction.gameObject.SetActive(true);
                        textDetailReaction.text = "反应 " + (weaponData.Reaction > 0 ? "+" : "") + weaponData.Reaction;
                    }

                    if (weaponData.Luck != default)
                    {
                        textDetailLuck.gameObject.SetActive(true);
                        textDetailLuck.text = "幸运 " + (weaponData.Luck > 0 ? "+" : "") + weaponData.Luck;
                    }

                    #endregion
                }
                    break;
                case PackageGearData gearData:
                {
                    #region 装备UI

                    detailGearBar.gameObject.SetActive(true);
                    textDetailGearPart.gameObject.SetActive(true);
                    textDetailGearPart.text = gearData.Part switch
                    {
                        HumanoidAppearanceGearPart.Head => "头部装备",
                        HumanoidAppearanceGearPart.Torso => "身体装备",
                        HumanoidAppearanceGearPart.LeftArm => "左臂装备",
                        HumanoidAppearanceGearPart.RightArm => "右臂装备",
                        HumanoidAppearanceGearPart.LeftLeg => "左腿装备",
                        HumanoidAppearanceGearPart.RightLeg => "右腿装备",
                        _ => ""
                    };

                    textDetailGearRaces.gameObject.SetActive(true);
                    var raceStringBuilder = new StringBuilder();
                    if (gearData.Races == HumanoidAppearanceRace.None)
                    {
                        raceStringBuilder.Append("无");
                    }
                    else
                    {
                        if ((gearData.Races & HumanoidAppearanceRace.HumanMale) != 0)
                        {
                            if (raceStringBuilder.Length != 0)
                            {
                                raceStringBuilder.Append("、");
                            }

                            raceStringBuilder.Append("人类男");
                        }

                        if ((gearData.Races & HumanoidAppearanceRace.HumanFemale) != 0)
                        {
                            if (raceStringBuilder.Length != 0)
                            {
                                raceStringBuilder.Append("、");
                            }

                            raceStringBuilder.Append("人类女");
                        }
                    }

                    textDetailGearRaces.text = "适用种族：" + raceStringBuilder.ToString();

                    if (gearData.MaxHp != default)
                    {
                        textDetailMaxHp.gameObject.SetActive(true);
                        textDetailMaxHp.text = "最大生命值 " + (gearData.MaxHp > 0 ? "+" : "") + gearData.MaxHp;
                    }

                    if (gearData.MaxMp != default)
                    {
                        textDetailMaxMp.gameObject.SetActive(true);
                        textDetailMaxMp.text = "最大法力值 " + (gearData.MaxMp > 0 ? "+" : "") + gearData.MaxMp;
                    }

                    if (gearData.Stamina != default)
                    {
                        textDetailStamina.gameObject.SetActive(true);
                        textDetailStamina.text = "耐力 " + (gearData.Stamina > 0 ? "+" : "") + gearData.Stamina;
                    }

                    if (gearData.Strength != default)
                    {
                        textDetailStrength.gameObject.SetActive(true);
                        textDetailStrength.text = "力量 " + (gearData.Strength > 0 ? "+" : "") + gearData.Strength;
                    }

                    if (gearData.Magic != default)
                    {
                        textDetailMagic.gameObject.SetActive(true);
                        textDetailMagic.text = "魔力 " + (gearData.Magic > 0 ? "+" : "") + gearData.Magic;
                    }

                    if (gearData.Reaction != default)
                    {
                        textDetailReaction.gameObject.SetActive(true);
                        textDetailReaction.text = "反应 " + (gearData.Reaction > 0 ? "+" : "") + gearData.Reaction;
                    }

                    if (gearData.Luck != default)
                    {
                        textDetailLuck.gameObject.SetActive(true);
                        textDetailLuck.text = "幸运 " + (gearData.Luck > 0 ? "+" : "") + gearData.Luck;
                    }

                    #endregion
                }
                    break;
                case PackageItemData itemData:
                {
                    #region 道具UI

                    if (itemData.Hp != default)
                    {
                        textDetailHp.gameObject.SetActive(true);
                        textDetailHp.text = "生命值 " + (itemData.Hp > 0 ? "+" : "") + itemData.Hp;
                    }

                    if (itemData.Mp != default)
                    {
                        textDetailMp.gameObject.SetActive(true);
                        textDetailMp.text = "法力值 " + (itemData.Mp > 0 ? "+" : "") + itemData.Mp;
                    }

                    if (!string.IsNullOrEmpty(itemData.BuffId) &&
                        _buffManager.TryGetBuffInfo(itemData.BuffId, out var buffInfo))
                    {
                        textDetailBuff.gameObject.SetActive(true);
                        textDetailBuff.text = $"赋予Buff\n【{buffInfo.name}】";
                    }

                    if (itemData.Skills.Count != 0)
                    {
                        textDetailSkills.gameObject.SetActive(true);
                        var skillStringBuilder = new StringBuilder();
                        itemData.Skills.ForEach(skill =>
                        {
                            if (_skillManager.TryGetSkillPrototype(skill, out var skillPrototype))
                            {
                                if (skillStringBuilder.Length == 0)
                                {
                                    skillStringBuilder.AppendLine("习得能力");
                                }

                                skillStringBuilder.AppendLine($"【{skillPrototype.Name}】");
                            }
                        });
                        textDetailSkills.text = skillStringBuilder.ToString();
                    }

                    #endregion
                }
                    break;
                case PackageMaterialData materialData:
                {
                }
                    break;
            }
        }
    }
}