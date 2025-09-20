using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combo.Blackboard
{
    public class ComboBlackboard : Framework.Common.Blackboard.Blackboard
    {
        [FormerlySerializedAs("inputTips")] [SerializeField]
        private List<ComboBlackboardTipVariable> tips = new();

        public ComboBlackboardTipVariable[] Tips => tips.ToArray();

        [SerializeField] private List<ComboBlackboardColliderGroupSetting> globalSharedColliderGroupSettings;

        // 全局的碰撞组检测记录，以碰撞组id为键
        private readonly Dictionary<string, List<(GameObject gameObject, float countdown)>> _detectedObjectRecords = new();

        public void CalculateGlobalSharedCollideInterval(float deltaTime)
        {
            for (int i = 0; i < _detectedObjectRecords.Keys.Count; i++)
            {
                var key = _detectedObjectRecords.Keys.ElementAt(i);
                var detectedRecords = _detectedObjectRecords[key];
                detectedRecords.RemoveAll(tuple =>
                {
                    tuple.countdown -= deltaTime;
                    return tuple.countdown <= 0f;
                });
            }
        }

        public bool AllowCollide(string groupId, Collider collider)
        {
            // 判断碰撞组是否被共享
            var index = globalSharedColliderGroupSettings.FindIndex(setting => setting.groupId == groupId);
            if (index == -1)
            {
                DebugUtil.LogWarning($"The collider group({groupId}) is not shared in ComboBlackboard");
                return false;
            }

            var sharedColliderGroupSetting = globalSharedColliderGroupSettings[index];
            var colliderDetectionInterval = sharedColliderGroupSetting.detectionInterval;
            var colliderDetectionMaximum = sharedColliderGroupSetting.detectionMaximum;

            // 判断当前碰撞组是否存在碰撞记录
            if (_detectedObjectRecords.TryGetValue(groupId, out var detectedRecords))
            {
                // 判断已记录的碰撞体数量是否达到最大值，是则不响应碰撞
                if (detectedRecords.Count >= colliderDetectionMaximum)
                {
                    return false;
                }

                // 判断该碰撞体是否已经被记录
                if (detectedRecords.Any((tuple => tuple.gameObject == collider.gameObject)))
                {
                    return false;
                }

                detectedRecords.Add((collider.gameObject, colliderDetectionInterval));
                return true;
            }

            _detectedObjectRecords.Add(groupId, new List<(GameObject gameObject, float countdown)>
            {
                new ValueTuple<GameObject, float>(collider.gameObject, colliderDetectionInterval)
            });
            return true;
        }
    }
}