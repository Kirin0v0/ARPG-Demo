using System;
using System.Collections.Generic;
using Common;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skill
{
    [CreateAssetMenu(fileName = "Skill Pool", menuName = "Skill/Skill Pool")]
    public class SkillPool : SerializedScriptableObject
    {
        [SerializeField] private Dictionary<string, SkillFlow> skillFlowPool = new();

        public bool TryGetSkillPrototype(string skillId, out SkillFlow skillPrototype)
        {
            if (!skillFlowPool.TryGetValue(skillId, out skillPrototype))
            {
                DebugUtil.LogWarning($"The SkillFlow(id={skillId}) is not found in the pool");
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        [Button]
        private void AddAllSkillFlowsInResourceToPool()
        {
            var skillFlows = Resources.FindObjectsOfTypeAll<SkillFlow>();
            skillFlowPool.Clear();
            skillFlows.ForEach(skillFlow =>
            {
                if (skillFlowPool.ContainsKey(skillFlow.Id))
                {
                    throw new Exception($"The Skill(id={skillFlow.Id}) is already existed in the pool");
                }

                skillFlowPool.Add(skillFlow.Id, skillFlow);
            });
        }
#endif
    }
}