using System;
using System.Collections.Generic;
using Framework.Core.Singleton;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Framework.Common.Resource
{
    /// <summary>
    /// Editor文件夹资源加载管理器，加载Editor文件夹内的资源
    /// </summary>
    public class EditorResourceManager
    {
        // 用于放置需要打包进AB包中的资源路径 
        private const string RootPath = "Assets/Editor/";

        public T Load<T>(string path) where T : Object
        {
            return (T)LoadAsset(path, typeof(T));
        }

        public void LoadAsync<T>(string path, UnityAction<T> callback) where T : Object
        {
            var asset = (T)LoadAsset(path, typeof(T));
            callback?.Invoke(asset);
        }

        public void UnloadAsset<T>(string path, UnityAction<T> callback) where T : Object
        {
        }

        public Object LoadAsset(string path, Type type)
        {
#if UNITY_EDITOR
            string suffixName = "";
            // 写入预设体、纹理（图片）、材质球、音效等等后缀名
            if (type == typeof(GameObject))
                suffixName = ".prefab";
            else if (type == typeof(Material))
                suffixName = ".mat";
            else if (type == typeof(Texture))
                suffixName = ".png";
            else if (type == typeof(AudioClip))
                suffixName = ".mp3";
            return AssetDatabase.LoadAssetAtPath(RootPath + path + suffixName, type);
#else
        return null;
#endif
        }

        public Sprite LoadSprite(string path, string spriteName)
        {
#if UNITY_EDITOR
            Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(RootPath + path);
            foreach (var item in sprites)
            {
                if (spriteName == item.name)
                    return item as Sprite;
            }

            return null;
#else
        return null;
#endif
        }

        public Dictionary<string, Sprite> LoadSprites(string path)
        {
#if UNITY_EDITOR
            Dictionary<string, Sprite> spriteDic = new Dictionary<string, Sprite>();
            Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(RootPath + path);
            foreach (var item in sprites)
            {
                spriteDic.Add(item.name, item as Sprite);
            }

            return spriteDic;
#else
        return null;
#endif
        }
    }
}