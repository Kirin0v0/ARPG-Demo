using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Quest.Config;
using Quest.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Quest
{
    [CreateAssetMenu(fileName = "Quest Pool", menuName = "Quest/Quest Pool")]
    public class QuestPool: ScriptableObject
    {
        [SerializeField, ReadOnly] private List<QuestConfig> questConfigs = new();
        public QuestConfig[] QuestConfigs => questConfigs.ToArray();

        [HideInInspector, SerializeField] private int idGeneratorSeed = 0;
        
        /// <summary>
        /// 克隆函数，用于运行时根据预设克隆实例
        /// </summary>
        /// <returns></returns>
        public QuestPool Clone()
        {
            var clonedQuestPool = ScriptableObject.Instantiate(this);
            clonedQuestPool.questConfigs = this.questConfigs.Select(ScriptableObject.Instantiate).ToList();
            return clonedQuestPool;
        }

        public void Clear()
        {
            questConfigs.ForEach(GameObject.Destroy);
            questConfigs.Clear();
        }

#if UNITY_EDITOR
        public QuestConfig AddQuest()
        {
            Undo.RecordObject(this, "Quest Pool(Add Quest)");
            var scriptableObject = ScriptableObject.CreateInstance(typeof(QuestConfig));
            var questId = GetQuestId();
            var questName = $"Quest {questId}";
            scriptableObject.name = questName;
            var questConfig = scriptableObject as QuestConfig;
            questConfig.id = questId;
            questConfig.name = questName;
            questConfigs.Add(questConfig);
            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(scriptableObject, this);
            }

            Undo.RegisterCreatedObjectUndo(questConfig, "Quest Pool(Add Quest)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return questConfig;
        }

        public void DeleteQuest(QuestConfig questConfig)
        {
            Undo.RecordObject(this, "Quest Pool(Delete Quest)");
            questConfigs.Remove(questConfig);
            // AssetDatabase.RemoveObjectFromAsset(node);
            Undo.DestroyObjectImmediate(questConfig);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
        
        public bool TryGetQuestConfig(string questId, out QuestConfig questConfig)
        {
            questConfig = questConfigs.Find(questConfig => questConfig.id == questId);
            if (questConfig == null)
            {
                DebugUtil.LogWarning($"The QuestConfig(id={questId}) is not found in the pool");
                return false;
            }

            return true;
        }
        
        public bool TryGetQuestInfo(string questId, out QuestInfo questInfo)
        {
            questInfo = questConfigs.Find(questConfig => questConfig.id == questId)?.ToQuestInfo();
            if (questInfo == null)
            {
                DebugUtil.LogWarning($"The QuestConfig(id={questId}) is not found in the pool");
                return false;
            }

            return true;
        }

        private string GetQuestId()
        {
            return (++idGeneratorSeed).ToString();
        }
    }
}