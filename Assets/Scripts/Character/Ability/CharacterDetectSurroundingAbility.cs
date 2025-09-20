using System;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Ability
{
    public enum CharacterSurroundingDetectionStrategy
    {
        Default,
        DistanceFirst,
        AngleFirst,
    }

    public class CharacterDetectSurroundingAbility : BaseCharacterOptionalAbility
    {
        [Title("检测设置")] [SerializeField] private float detectionRadius = 2f;
        [SerializeField] private LayerMask detectionLayers;
        [SerializeField] private CharacterSurroundingDetectionStrategy detectionStrategy;

        public GameObject FindSurroundingObject()
        {
            UnityEngine.Collider[] colliders =
                Physics.OverlapSphere(Owner.transform.position, detectionRadius, detectionLayers,
                    QueryTriggerInteraction.Collide);

            if (colliders.Length == 0)
            {
                return null;
            }

            switch (detectionStrategy)
            {
                case CharacterSurroundingDetectionStrategy.DistanceFirst:
                    GameObject minDistanceObject = null;
                    // 遍历获取距离最近的物体
                    foreach (var surroundingCollider in colliders)
                    {
                        if (minDistanceObject == null)
                        {
                            minDistanceObject = surroundingCollider.gameObject;
                        }
                        else
                        {
                            if ((minDistanceObject.transform.position - Owner.transform.position).sqrMagnitude >
                                (surroundingCollider.transform.position - Owner.transform.position).sqrMagnitude)
                            {
                                minDistanceObject = surroundingCollider.gameObject;
                            }
                        }
                    }

                    return minDistanceObject;
                case CharacterSurroundingDetectionStrategy.AngleFirst:
                    GameObject minAngleObject = null;
                    // 遍历获取与角色正朝向相比角度最小的物体
                    foreach (var surroundingCollider in colliders)
                    {
                        if (minAngleObject == null)
                        {
                            minAngleObject = surroundingCollider.gameObject;
                        }
                        else
                        {
                            if (Vector3.Angle(Owner.transform.forward,
                                    minAngleObject.transform.position - Owner.transform.position) >
                                Vector3.Angle(Owner.transform.forward,
                                    surroundingCollider.transform.position - Owner.transform.position))
                            {
                                minAngleObject = surroundingCollider.gameObject;
                            }
                        }
                    }

                    return minAngleObject;
                default:
                    return colliders[0].gameObject;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Owner)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Owner.transform.position, detectionRadius);
            }
        }
    }
}