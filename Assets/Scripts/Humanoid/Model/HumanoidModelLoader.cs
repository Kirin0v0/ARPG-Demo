using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Core.Extension;
using Humanoid.Model.Data;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Humanoid.Model
{
    /// <summary>
    /// 模型加载器依赖于模型预设体，加载原理都是通过固定字符串获取游戏物体，因此不可修改预设体内部物体名称
    /// </summary>
    public class HumanoidModelLoader
    {
        private readonly Dictionary<HumanoidModelClassification, List<HumanoidModelGroup>> _modelClassifications =
            new();

        private readonly Dictionary<HumanoidModelIdentity, List<HumanoidModelIdentity>> _modelReplacedRules = new();
        private readonly Dictionary<HumanoidModelRecord, GameObject> _models = new();
        private readonly Dictionary<HumanoidModelRecord, Material> _modelOriginMaterials = new();

        private readonly GameObject _target; // 模型目标物体
        private readonly Material _materialPrefab; // 模型预设材质

        public HumanoidModelLoader(GameObject target, Material materialPrefab = null)
        {
            _target = target;
            _materialPrefab = materialPrefab;

            InitModels();
            InitMaterial(materialPrefab);
            HideAllModels();
        }

        public void ShowModel(
            HumanoidModelType type,
            string part,
            HumanoidModelGenderRestriction genderRestriction,
            string name,
            HumanoidModelColor color
        )
        {
            var record = _models.Keys.ToList()
                .Find(record => record == new HumanoidModelRecord
                {
                    Type = type,
                    Part = part,
                    GenderRestriction = genderRestriction,
                    Name = name,
                });
            if (record == null)
            {
                throw new Exception(
                    $"The model doesn't have the model item({type}, {part}, {name})");
            }

            // 失活被替代类的模型
            if (_modelReplacedRules.TryGetValue(
                    new HumanoidModelIdentity { Type = type, Part = part, GenderRestriction = genderRestriction },
                    out var replacedModelIdentities))
            {
                replacedModelIdentities.ForEach(replacedModelIdentity =>
                {
                    if (!_modelClassifications.TryGetValue(
                            new HumanoidModelClassification
                                { Type = replacedModelIdentity.Type, Part = replacedModelIdentity.Part },
                            out var groups)) return;

                    var modelGroup = groups.Find(group =>
                        group.GenderRestriction == replacedModelIdentity.GenderRestriction);
                    modelGroup?.Models.ForEach(model => { model.SetActive(false); });
                });
            }

            // 失活其他同类模型
            var model = _models[record];
            _modelClassifications[new HumanoidModelClassification { Type = type, Part = part }]
                .ForEach(group => { group.Models.ForEach(model => { model.SetActive(false); }); });

            // 设置模型材质颜色
            var skinnedMeshRenderer = model.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer)
            {
                var material = Application.isEditor ? skinnedMeshRenderer.sharedMaterial : skinnedMeshRenderer.material;
                // 设置肤色
                material.SetColor("_Color_Skin", color.SkinColor);
                // 设置胡子/眉毛/头发的颜色
                material.SetColor("_Color_Hair", color.HairColor);
                // 设置胡茬/发角的颜色
                material.SetColor("_Color_Stubble", color.StubbleColor);
                // 设置眼睛的颜色
                material.SetColor("_Color_Eyes", color.EyesColor);
                // 设置伤疤的颜色
                material.SetColor("_Color_Scar", color.ScarColor);
                // 设置身体彩绘的颜色
                material.SetColor("_Color_BodyArt", color.BodyArtColor);
                material.SetFloat("_BodyArt_Amount", 1f);
                // 设置装备用套装颜色
                material.SetColor("_Color_Primary", color.PrimaryColor);
                material.SetColor("_Color_Secondary", color.SecondaryColor);
                // 设置装备用金属颜色
                material.SetColor("_Color_Metal_Primary", color.MetalPrimaryColor);
                material.SetColor("_Color_Metal_Secondary", color.MetalSecondaryColor);
                material.SetColor("_Color_Metal_Dark", color.MetalDarkColor);
                // 设置装备用皮革颜色
                material.SetColor("_Color_Leather_Primary", color.LeatherPrimaryColor);
                material.SetColor("_Color_Leather_Secondary", color.LeatherSecondaryColor);
            }

            // 最终激活该模型
            model.SetActive(true);
        }

        public void HideAllModels()
        {
            _models.Values.ToList().ForEach(model => { model.SetActive(false); });
        }

        public List<(HumanoidModelRecord modelRecord, HumanoidModelColor modelColor)> GetModels(bool showOnly)
        {
            return _models.Where(pair => !showOnly || pair.Value.activeSelf)
                .Select(pair =>
                {
                    var skinnedMeshRenderer = pair.Value.GetComponent<SkinnedMeshRenderer>();
                    if (!skinnedMeshRenderer)
                    {
                        return (pair.Key, HumanoidModelColor.Empty);
                    }

                    var material = Application.isEditor
                        ? skinnedMeshRenderer.sharedMaterial
                        : skinnedMeshRenderer.material;
                    return (pair.Key, new HumanoidModelColor
                            {
                                SkinColor = material.GetColor("_Color_Skin"),
                                HairColor = material.GetColor("_Color_Hair"),
                                StubbleColor = material.GetColor("_Color_Stubble"),
                                EyesColor = material.GetColor("_Color_Eyes"),
                                ScarColor = material.GetColor("_Color_Scar"),
                                BodyArtColor = material.GetColor("_Color_BodyArt"),
                                PrimaryColor = material.GetColor("_Color_Primary"),
                                SecondaryColor = material.GetColor("_Color_Secondary"),
                                MetalPrimaryColor = material.GetColor("_Color_Metal_Primary"),
                                MetalSecondaryColor = material.GetColor("_Color_Metal_Secondary"),
                                MetalDarkColor = material.GetColor("_Color_Metal_Dark"),
                                LeatherPrimaryColor = material.GetColor("_Color_Leather_Primary"),
                                LeatherSecondaryColor = material.GetColor("_Color_Leather_Secondary")
                            }
                        );
                }).ToList();
        }

        public void ShowRandomBody(HumanoidCharacterRace race, HumanoidModelColor bodyColor)
        {
            HideAllModels();
            // 身体模型需要展示全部身体部位，这里每个部位都随机激活一个模型
            _modelClassifications.ForEach(pair =>
            {
                if (pair.Key.Type == HumanoidModelType.Body)
                {
                    ShowRandomPart(pair.Key.Part);
                }
            });

            // 随机获取部位的某个模型信息，并展示模型
            void ShowRandomPart(string part)
            {
                var modelPool = new List<HumanoidModelRecord>();
                _models.ForEach(pair =>
                {
                    if (pair.Key.Type != HumanoidModelType.Body || pair.Key.Part != part)
                    {
                        return;
                    }

                    switch (race)
                    {
                        case HumanoidCharacterRace.HumanFemale:
                        {
                            if (pair.Key.GenderRestriction == HumanoidModelGenderRestriction.None ||
                                pair.Key.GenderRestriction == HumanoidModelGenderRestriction.FemaleOnly)
                            {
                                modelPool.Add(pair.Key);
                            }
                        }
                            break;
                        case HumanoidCharacterRace.HumanMale:
                        {
                            if (pair.Key.GenderRestriction == HumanoidModelGenderRestriction.None ||
                                pair.Key.GenderRestriction == HumanoidModelGenderRestriction.MaleOnly)
                            {
                                modelPool.Add(pair.Key);
                            }
                        }
                            break;
                    }
                });

                if (modelPool.Count == 0)
                {
                    return;
                }

                var modelRecord = modelPool[Random.Range(0, modelPool.Count)];
                ShowModel(modelRecord.Type, modelRecord.Part, modelRecord.GenderRestriction, modelRecord.Name,
                    bodyColor);
            }
        }

        public void DestroyHiddenModels()
        {
            _models.Values.ForEach(model =>
            {
                if (!model.activeSelf)
                {
                    GameObject.Destroy(model);
                }
            });

            // 重新初始化模型信息
            InitModels();
        }

        public void Destroy()
        {
            // 销毁克隆材质，并恢复原先的模型材质
            if (_materialPrefab)
            {
                _models.ForEach(pair =>
                {
                    if (pair.Value.IsDestroyed())
                    {
                        return;
                    }

                    var skinnedMeshRenderer = pair.Value.GetComponent<SkinnedMeshRenderer>();
                    if (!skinnedMeshRenderer) return;
                    var clonedMaterial = !Application.isPlaying
                        ? skinnedMeshRenderer.sharedMaterial
                        : skinnedMeshRenderer.material;
                    GameObject.Destroy(clonedMaterial);
                    if (!Application.isPlaying)
                    {
                        skinnedMeshRenderer.sharedMaterial = _modelOriginMaterials[pair.Key];
                    }
                    else
                    {
                        skinnedMeshRenderer.material = _modelOriginMaterials[pair.Key];
                    }
                });
            }

            _models.Clear();
            _modelClassifications.Clear();
            _modelReplacedRules.Clear();
            _modelOriginMaterials.Clear();
        }

        private void InitMaterial(Material material)
        {
            if (!material)
            {
                return;
            }

            // 将全部模型的材质换为克隆材质
            _models.Values.ToList().ForEach(model =>
            {
                var skinnedMeshRenderer = model.GetComponent<SkinnedMeshRenderer>();
                if (!skinnedMeshRenderer) return;
                if (!Application.isPlaying)
                {
                    skinnedMeshRenderer.sharedMaterial = GameObject.Instantiate(material);
                }
                else
                {
                    skinnedMeshRenderer.material = GameObject.Instantiate(material);
                }
            });
        }

        private void InitModels()
        {
            _modelClassifications.Clear();
            _modelReplacedRules.Clear();
            _models.Clear();
            _modelOriginMaterials.Clear();

            // 这里是固定按照预设体内部模型依次查询并加载到内存中
            LoadModels(HumanoidModelType.Body, "00_Hair");
            LoadModels(HumanoidModelType.Body, "01_Head");
            LoadModels(HumanoidModelType.Body, "02_Eyebrows");
            LoadModels(HumanoidModelType.Body, "03_FacialHair");
            LoadModels(HumanoidModelType.Body, "04_Torso");
            LoadModels(HumanoidModelType.Body, "05_Arm_Upper_Right");
            LoadModels(HumanoidModelType.Body, "06_Arm_Upper_Left");
            LoadModels(HumanoidModelType.Body, "07_Arm_Lower_Right");
            LoadModels(HumanoidModelType.Body, "08_Arm_Lower_Left");
            LoadModels(HumanoidModelType.Body, "09_Hand_Right");
            LoadModels(HumanoidModelType.Body, "10_Hand_Left");
            LoadModels(HumanoidModelType.Body, "11_Hips");
            LoadModels(HumanoidModelType.Body, "12_Leg_Right");
            LoadModels(HumanoidModelType.Body, "13_Leg_Left");
            LoadModels(HumanoidModelType.Gear, "00_Head");
            LoadModels(HumanoidModelType.Gear, "01_Helmet_Attachment");
            LoadModels(HumanoidModelType.Gear, "02_Torso");
            LoadModels(HumanoidModelType.Gear, "03_Chest_Attachment");
            LoadModels(HumanoidModelType.Gear, "04_Back_Attachment");
            LoadModels(HumanoidModelType.Gear, "05_Arm_Upper_Right");
            LoadModels(HumanoidModelType.Gear, "06_Shoulder_Right_Attachment");
            LoadModels(HumanoidModelType.Gear, "07_Arm_Upper_Left");
            LoadModels(HumanoidModelType.Gear, "08_Shoulder_Left_Attachment");
            LoadModels(HumanoidModelType.Gear, "09_Arm_Lower_Right");
            LoadModels(HumanoidModelType.Gear, "10_Elbow_Right_Attachment");
            LoadModels(HumanoidModelType.Gear, "11_Arm_Lower_Left");
            LoadModels(HumanoidModelType.Gear, "12_Elbow_Left_Attachment");
            LoadModels(HumanoidModelType.Gear, "13_Hand_Right");
            LoadModels(HumanoidModelType.Gear, "14_Hand_Left");
            LoadModels(HumanoidModelType.Gear, "15_Hips");
            LoadModels(HumanoidModelType.Gear, "16_Hips_Attachment");
            LoadModels(HumanoidModelType.Gear, "17_Leg_Right");
            LoadModels(HumanoidModelType.Gear, "18_Knee_Right_Attachment");
            LoadModels(HumanoidModelType.Gear, "19_Leg_Left");
            LoadModels(HumanoidModelType.Gear, "20_Knee_Left_Attachment");

            // 这里写死替换规则，后续可以通过读取配置文件或Excel来动态生成规则
            AddReplacedRules("00_Head",
                new[] { HumanoidModelGenderRestriction.FemaleOnly, HumanoidModelGenderRestriction.MaleOnly },
                "01_Head",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("02_Torso",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "04_Torso",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("05_Arm_Upper_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "05_Arm_Upper_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("07_Arm_Upper_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "06_Arm_Upper_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("09_Arm_Lower_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "07_Arm_Lower_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("11_Arm_Lower_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "08_Arm_Lower_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("13_Hand_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "09_Hand_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("14_Hand_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "10_Hand_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("15_Hips",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "11_Hips",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("17_Leg_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "12_Leg_Right",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );
            AddReplacedRules("19_Leg_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                },
                "13_Leg_Left",
                new[]
                {
                    HumanoidModelGenderRestriction.None, HumanoidModelGenderRestriction.FemaleOnly,
                    HumanoidModelGenderRestriction.MaleOnly
                }
            );

            return;

            void AddReplacedRules(string toReplacePart, HumanoidModelGenderRestriction[] toReplaceGenderRestrictions,
                string beReplacedPart, HumanoidModelGenderRestriction[] beReplacedGenderRestrictions)
            {
                var beReplacedList = beReplacedGenderRestrictions.Select(beReplacedGenderRestriction =>
                    new HumanoidModelIdentity
                    {
                        Type = HumanoidModelType.Body,
                        Part = beReplacedPart,
                        GenderRestriction = beReplacedGenderRestriction,
                    }).ToList();

                toReplaceGenderRestrictions.ForEach(toReplaceGenderRestriction =>
                {
                    _modelReplacedRules.Add(new HumanoidModelIdentity
                    {
                        Type = HumanoidModelType.Gear,
                        Part = toReplacePart,
                        GenderRestriction = toReplaceGenderRestriction,
                    }, beReplacedList);
                });
            }
        }

        private void LoadModels(HumanoidModelType type, string part)
        {
            LoadModels(type, part, HumanoidModelGenderRestriction.None);
            LoadModels(type, part, HumanoidModelGenderRestriction.FemaleOnly);
            LoadModels(type, part, HumanoidModelGenderRestriction.MaleOnly);
        }

        private void LoadModels(
            HumanoidModelType type,
            string part,
            HumanoidModelGenderRestriction genderRestriction
        )
        {
            var searchModels = SearchModels(type, part, genderRestriction);

            var modelClassification = new HumanoidModelClassification
            {
                Type = type,
                Part = part
            };
            if (!_modelClassifications.TryGetValue(modelClassification, out var groups))
            {
                groups = new List<HumanoidModelGroup>();
                _modelClassifications.Add(modelClassification, groups);
            }

            var modelGroup = groups.Find(group => group.GenderRestriction == genderRestriction);
            if (modelGroup == null)
            {
                modelGroup = new HumanoidModelGroup
                {
                    GenderRestriction = genderRestriction,
                    Models = new List<GameObject>(),
                };
                groups.Add(modelGroup);
            }

            modelGroup.Models.AddRange(searchModels);

            searchModels.ForEach(model =>
            {
                _models.Add(new HumanoidModelRecord
                {
                    Type = type,
                    Part = part,
                    GenderRestriction = genderRestriction,
                    Name = model.name
                }, model);
                if (model.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
                {
                    _modelOriginMaterials.Add(new HumanoidModelRecord
                    {
                        Type = type,
                        Part = part,
                        GenderRestriction = genderRestriction,
                        Name = model.name
                    }, !Application.isPlaying ? skinnedMeshRenderer.sharedMaterial : skinnedMeshRenderer.material);
                }
                else
                {
                    _modelOriginMaterials.Add(new HumanoidModelRecord
                    {
                        Type = type,
                        Part = part,
                        GenderRestriction = genderRestriction,
                        Name = model.name
                    }, null);
                }
            });
        }

        private List<GameObject> SearchModels(
            HumanoidModelType type,
            string part,
            HumanoidModelGenderRestriction genderRestriction
        )
        {
            var typeParent = _target.GetComponentsInChildren<Transform>().ToList().Find(transform =>
            {
                switch (type)
                {
                    case HumanoidModelType.Body:
                    {
                        if (transform.gameObject.name == "Body")
                        {
                            return true;
                        }
                    }
                        break;
                    case HumanoidModelType.Gear:
                    {
                        if (transform.gameObject.name == "Gear")
                        {
                            return true;
                        }
                    }
                        break;
                }

                return false;
            });

            if (!typeParent)
            {
                return new List<GameObject>();
            }

            var partParent = typeParent.GetComponentsInChildren<Transform>().ToList()
                .Find(transform => transform.gameObject.name == part);

            if (!partParent)
            {
                return new List<GameObject>();
            }

            var genderRestrictionParent = partParent.GetComponentsInChildren<Transform>().ToList().Find(transform =>
            {
                switch (genderRestriction)
                {
                    case HumanoidModelGenderRestriction.None:
                    {
                        if (transform.gameObject.name == "All")
                        {
                            return true;
                        }
                    }
                        break;
                    case HumanoidModelGenderRestriction.FemaleOnly:
                    {
                        if (transform.gameObject.name == "Female")
                        {
                            return true;
                        }
                    }
                        break;
                    case HumanoidModelGenderRestriction.MaleOnly:
                    {
                        if (transform.gameObject.name == "Male")
                        {
                            return true;
                        }
                    }
                        break;
                }

                return false;
            });

            if (!genderRestrictionParent)
            {
                return new List<GameObject>();
            }

            var models = new List<GameObject>();
            for (var i = 0; i < genderRestrictionParent.childCount; i++)
            {
                models.Add(genderRestrictionParent.GetChild(i).gameObject);
            }

            return models;
        }
    }
}