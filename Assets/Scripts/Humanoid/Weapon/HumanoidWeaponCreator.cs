using System;
using System.Collections.Generic;
using Character.Data.Extension;
using Humanoid.Data;
using Package.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Humanoid.Weapon
{
    public class HumanoidWeaponCreator
    {
        private readonly List<(Type type, string key)> _addressablesLoads = new();
        private readonly Func<HumanoidAppearanceWeaponType, Material> _weaponAppearanceMaterialConverter;

        public HumanoidWeaponCreator(Func<HumanoidAppearanceWeaponType, Material> weaponAppearanceMaterialConverter)
        {
            _weaponAppearanceMaterialConverter = weaponAppearanceMaterialConverter;
        }

        /// <summary>
        /// 通过物品预设数据创建武器模型对象，并不是真正的武器
        /// </summary>
        /// <param name="data">物品预设武器数据</param>
        /// <param name="callback">武器模型对象回调</param>
        /// <returns></returns>
        public void CreateWeaponModelAsync(PackageWeaponData data, UnityAction<GameObject> callback)
        {
            CreateWeaponModelAsync(data.Appearance, callback);
        }

        /// <summary>
        /// 通过武器外观数据创建武器模型对象，并不是真正的武器
        /// </summary>
        /// <param name="data">物品预设武器数据</param>
        /// <param name="callback">武器模型对象回调</param>
        /// <returns></returns>
        public void CreateWeaponModelAsync(HumanoidAppearanceWeaponInfoData data, UnityAction<GameObject> callback)
        {
            CreateWeaponModelAsyncInternal(data.Type.ToWeaponAppearanceType(), data.Model,
                data.GetFantasyAppearanceColor(),
                data.Payload, callback);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 通过基础数据创建武器模型对象，并不是真正的武器
        /// </summary>
        /// <param name="samuraiWeaponTexture"></param>
        /// <param name="weaponAppearanceType"></param>
        /// <param name="weaponPrefab"></param>
        /// <param name="fantasyWeaponColor"></param>
        /// <param name="callback">武器模型对象回调</param>
        /// <returns></returns>
        public void CreateWeaponModelAsync(
            HumanoidAppearanceWeaponType weaponAppearanceType,
            string weaponPrefab,
            HumanoidAppearanceColor fantasyWeaponColor,
            string samuraiWeaponTexture,
            UnityAction<GameObject> callback
        )
        {
            CreateWeaponModelAsyncInternal(weaponAppearanceType, weaponPrefab, fantasyWeaponColor, samuraiWeaponTexture,
                callback);
        }
#endif

        /// <summary>
        /// 通过武器数据创建武器对象并绑定武器组件
        /// </summary>
        /// <param name="weaponData">武器数据</param>
        /// <param name="unequippedHolderTransform"></param>
        /// <param name="equippedHolderTransform"></param>
        /// <param name="callback">武器对象回调</param>
        /// <returns></returns>
        public void CreateWeaponObjectAsync(
            PackageWeaponData weaponData,
            Transform unequippedHolderTransform,
            Transform equippedHolderTransform,
            UnityAction<HumanoidWeaponObject> callback
        )
        {
            // 异步创建武器模型
            CreateWeaponModelAsyncInternal(
                weaponData.AppearanceType,
                weaponData.Appearance.Model,
                weaponData.Appearance.GetFantasyAppearanceColor(),
                weaponData.Appearance.Payload,
                (instance =>
                {
                    // 附加WeaponObject组件
                    var weaponObject = instance.GetComponent<HumanoidWeaponObject>();
                    if (!weaponObject)
                    {
                        weaponObject = instance.AddComponent<HumanoidWeaponObject>();
                    }

                    // 初始化组件
                    weaponObject.Init(weaponData, unequippedHolderTransform, equippedHolderTransform);

                    // 执行异步结果回调
                    callback?.Invoke(weaponObject);
                })
            );
        }

        /// <summary>
        /// 通过武器外观预设体创建幻想武器模型对象，并不是真正的武器
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <param name="fantasyColor"></param>
        /// <returns></returns>
        private GameObject CreateFantasyWeaponModel(
            GameObject weaponPrefab,
            HumanoidAppearanceColor fantasyColor
        )
        {
            var instance = GameObject.Instantiate(weaponPrefab);
            // 生成克隆材质
            var material =
                GameObject.Instantiate(_weaponAppearanceMaterialConverter(HumanoidAppearanceWeaponType.Fantasy));
            if (!material)
            {
                // 如果没有设置材质则直接获取部件的材质
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    // 这里判断处于编辑状态还是其他状态
                    material = Application.isEditor ? meshRenderer.sharedMaterial : meshRenderer.material;
                }
            }
            else
            {
                // 如果有材质则直接同步部件的材质
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    // 这里判断处于编辑状态还是其他状态
                    if (Application.isEditor)
                    {
                        meshRenderer.sharedMaterial = material;
                    }
                    else
                    {
                        meshRenderer.material = material;
                    }
                }
            }

            if (!material)
            {
                return instance;
            }

            // 设置肤色
            material.SetColor("_Color_Skin", fantasyColor.SkinColor);
            // 设置胡子/眉毛/头发的颜色
            material.SetColor("_Color_Hair", fantasyColor.HairColor);
            // 设置胡茬/发角的颜色
            material.SetColor("_Color_Stubble", fantasyColor.StubbleColor);
            // 设置眼睛的颜色
            material.SetColor("_Color_Eyes", fantasyColor.EyesColor);
            // 设置伤疤的颜色
            material.SetColor("_Color_Scar", fantasyColor.ScarColor);
            // 设置身体彩绘的颜色
            material.SetColor("_Color_BodyArt", fantasyColor.BodyArtColor);
            material.SetFloat("_BodyArt_Amount", 1f);
            // 设置装备用套装颜色
            material.SetColor("_Color_Primary", fantasyColor.PrimaryColor);
            material.SetColor("_Color_Secondary", fantasyColor.SecondaryColor);
            // 设置装备用金属颜色
            material.SetColor("_Color_Metal_Primary", fantasyColor.MetalPrimaryColor);
            material.SetColor("_Color_Metal_Secondary", fantasyColor.MetalSecondaryColor);
            material.SetColor("_Color_Metal_Dark", fantasyColor.MetalDarkColor);
            // 设置装备用皮革颜色
            material.SetColor("_Color_Leather_Primary", fantasyColor.LeatherPrimaryColor);
            material.SetColor("_Color_Leather_Secondary", fantasyColor.LeatherSecondaryColor);

            return instance;
        }

        /// <summary>
        /// 通过武器外观预设体创建武士武器模型对象，并不是真正的武器
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private GameObject CreateSamuraiWeaponModel(
            GameObject weaponPrefab,
            Texture texture = null
        )
        {
            var instance = GameObject.Instantiate(weaponPrefab);
            // 生成克隆材质
            var material =
                GameObject.Instantiate(_weaponAppearanceMaterialConverter(HumanoidAppearanceWeaponType.Samurai));
            if (!material)
            {
                // 如果没有设置材质则直接获取部件的材质
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    // 这里判断处于编辑状态还是其他状态
                    material = Application.isEditor ? meshRenderer.sharedMaterial : meshRenderer.material;
                }
            }
            else
            {
                // 如果有材质则直接同步部件的材质
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    // 这里判断处于编辑状态还是其他状态
                    if (Application.isEditor)
                    {
                        meshRenderer.sharedMaterial = material;
                    }
                    else
                    {
                        meshRenderer.material = material;
                    }
                }
            }

            if (!material || !texture)
            {
                return instance;
            }

            // 设置纹理
            material.SetTexture("_BaseMap", texture);
            return instance;
        }

        public void Destroy()
        {
            foreach (var valueTuple in _addressablesLoads)
            {
                GameApplication.Instance?.AddressablesManager.ReleaseAsset(valueTuple.key, valueTuple.type);
            }
        }

        private void CreateWeaponModelAsyncInternal(
            HumanoidAppearanceWeaponType weaponAppearanceType,
            string weaponPrefab,
            HumanoidAppearanceColor fantasyWeaponColor,
            string samuraiWeaponTexture,
            UnityAction<GameObject> callback
        )
        {
            // 记录加载
            _addressablesLoads.Add(new() { type = typeof(GameObject), key = weaponPrefab });
            GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>(weaponPrefab,
                (weaponModel =>
                {
                    switch (weaponAppearanceType)
                    {
                        case HumanoidAppearanceWeaponType.Fantasy:
                        {
                            var instance = CreateFantasyWeaponModel(
                                weaponModel,
                                fantasyWeaponColor
                            );
                            // 执行异步结果回调
                            callback?.Invoke(instance);
                        }
                            break;
                        case HumanoidAppearanceWeaponType.Samurai:
                        {
                            if (string.IsNullOrEmpty(samuraiWeaponTexture))
                            {
                                var instance = CreateSamuraiWeaponModel(weaponModel);
                                // 执行异步结果回调
                                callback?.Invoke(instance);
                            }
                            else
                            {
                                // 记录加载
                                _addressablesLoads.Add(new() { type = typeof(Texture), key = samuraiWeaponTexture });
                                // 加载纹理资源
                                GameApplication.Instance.AddressablesManager.LoadAssetAsync<Texture>(
                                    samuraiWeaponTexture,
                                    (texture =>
                                    {
                                        var instance = CreateSamuraiWeaponModel(weaponModel, texture);
                                        // 执行异步结果回调
                                        callback?.Invoke(instance);
                                    }));
                            }
                        }
                            break;
                    }
                }));
        }
    }
}