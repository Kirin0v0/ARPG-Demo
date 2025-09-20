using System;
using System.Collections.Generic;
using System.Linq;
using Buff.Config;
using Buff.Data;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Buff
{
    [CreateAssetMenu(fileName = "Buff Pool", menuName = "Buff/Buff Pool")]
    public class BuffPool : ScriptableObject
    {
        [ReadOnly, SerializeField] private List<BuffConfig> buffConfigs = new();
        public BuffConfig[] BuffConfigs => buffConfigs.ToArray();

        [HideInInspector, SerializeField] private int idGeneratorSeed = 0;
        
        /// <summary>
        /// 克隆函数，用于运行时根据预设克隆实例
        /// </summary>
        /// <returns></returns>
        public BuffPool Clone()
        {
            var clonedBuffPool = ScriptableObject.Instantiate(this);
            clonedBuffPool.buffConfigs = this.buffConfigs.Select(ScriptableObject.Instantiate).ToList();
            return clonedBuffPool;
        }

        public void Clear()
        {
            buffConfigs.ForEach(GameObject.Destroy);
            buffConfigs.Clear();
        }

#if UNITY_EDITOR
        public BuffConfig AddBuff()
        {
            Undo.RecordObject(this, "Buff Pool(Add Buff)");
            var scriptableObject = ScriptableObject.CreateInstance(typeof(BuffConfig));
            var buffId = GetBuffId();
            var buffName = $"Buff {buffId}";
            scriptableObject.name = buffName;
            var buffConfig = scriptableObject as BuffConfig;
            buffConfig.id = buffId;
            buffConfig.name = buffName;
            buffConfigs.Add(buffConfig);
            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(scriptableObject, this);
            }

            Undo.RegisterCreatedObjectUndo(buffConfig, "Buff Pool(Add Buff)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return buffConfig;
        }

        public BuffConfig CopyBuff(BuffConfig originBuffConfig)
        {
            Undo.RecordObject(this, "Buff Pool(Copy Buff)");
            var scriptableObject = ScriptableObject.Instantiate(originBuffConfig);
            var buffId = GetBuffId();
            var buffName = $"Buff {buffId}";
            scriptableObject.name = buffName;
            var buffConfig = scriptableObject as BuffConfig;
            buffConfig.id = buffId;
            buffConfig.name = buffName;
            buffConfigs.Add(buffConfig);
            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(scriptableObject, this);
            }

            Undo.RegisterCreatedObjectUndo(buffConfig, "Buff Pool(Add Buff)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return buffConfig;
        }

        public void DeleteBuff(BuffConfig buffConfig)
        {
            Undo.RecordObject(this, "Buff Pool(Delete Buff)");
            buffConfigs.Remove(buffConfig);
            // AssetDatabase.RemoveObjectFromAsset(node);
            Undo.DestroyObjectImmediate(buffConfig);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif

        public bool TryGetBuffInfo(string buffId, out BuffInfo buffInfo)
        {
            buffInfo = default;
            var buffConfig = buffConfigs.Find(buffConfig => buffConfig.id == buffId);
            if (buffConfig == null)
            {
                DebugUtil.LogWarning($"The BuffConfig(id={buffId}) is not found in the pool");
                return false;
            }

            buffInfo = buffConfig.ToBuffInfo();
            return true;
        }

        private string GetBuffId()
        {
            return (++idGeneratorSeed).ToString();
        }
    }
}