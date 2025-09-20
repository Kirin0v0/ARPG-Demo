using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Common;
using Framework.Common.Debug;
using Framework.Common.Util;
using Framework.Core.Extension;
using Map;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace CollideDetection
{
    /// <summary>
    /// 碰撞检测管理器，用于管理CharacterController的碰撞检测，由于CharacterController本身无法检测碰撞方，因此需要额外检测逻辑
    /// </summary>
    public class CollideDetectionManager : MonoBehaviour
    {
        [SerializeField] private bool debug = false;

        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;

        private Vector2 _mapCenter = Vector2.zero;
        private Vector2 _mapSize = Vector2.zero;

        private readonly Dictionary<CharacterObject, HashSet<CharacterObject>> _movementCollideRecords = new();
        private readonly Dictionary<CharacterObject, HashSet<CharacterObject>> _airborneCollideRecords = new();

        private void Awake()
        {
            _mapManager.AfterMapLoad += HandleMapChanged;
        }

        private void LateUpdate()
        {
            _movementCollideRecords.Clear();
            _airborneCollideRecords.Clear();
            _gameManager.Characters.ForEach(character =>
            {
                CheckMovementCollision(character);
                CheckAirborneCollision(character);
            });

            if (debug)
            {
                _movementCollideRecords.ForEach(pair =>
                {
                    var character = pair.Key;
                    pair.Value.ForEach(target =>
                    {
                        DebugUtil.LogCyan($"移动碰撞: {character.gameObject.name} ==> {target.gameObject.name}");
                    });
                });
            }

            return;

            void CheckMovementCollision(CharacterObject character)
            {
                var characterController = character.CharacterController;
                if (!characterController)
                {
                    return;
                }

                // 胶囊投射检测，从角色控制器胶囊的底部半圆往上至角色控制器胶囊的顶部半圆
                var center = characterController.transform.position + characterController.center;
                var origin = center - Vector3.up * (characterController.height / 2 +
                    -characterController.radius - GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius);
                var destination = center + Vector3.up * (characterController.height / 2 +
                    -characterController.radius - GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius);
                var maxDistance = Mathf.Max(destination.y - origin.y, 0);
                var hits = Physics.SphereCastAll(
                    origin,
                    characterController.radius + GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius +
                    characterController.skinWidth,
                    Vector3.up,
                    maxDistance,
                    GlobalRuleSingletonConfigSO.Instance.characterPhysicsLayer
                );
                var colliders = new HashSet<CharacterObject>();
                hits.ForEach(raycastHit =>
                {
                    var target = raycastHit.collider.gameObject.GetComponent<CharacterObject>();
                    if (!target || target == character)
                    {
                        return;
                    }

                    colliders.Add(target);
                });

                if (colliders.Count != 0)
                {
                    _movementCollideRecords.Add(character, colliders);
                }
            }

            void CheckAirborneCollision(CharacterObject character)
            {
                if (!character.Parameters.Airborne)
                {
                    return;
                }

                if (!character.CharacterController)
                {
                    return;
                }

                var selfCenter = character.CharacterController.transform.position +
                                 character.CharacterController.center;
                var selfRadius = character.CharacterController.radius;
                var collisions = new HashSet<CharacterObject>();
                // 遍历角色检测
                _gameManager.Characters.ForEach(target =>
                {
                    if (target == character || !target.CharacterController)
                    {
                        return;
                    }

                    var targetCenter = target.Parameters.position + target.CharacterController.center;
                    var targetTop = target.Parameters.position +
                                    (target.CharacterController?.height ?? 0f) * Vector3.up;
                    var targetRadius = target.CharacterController?.radius ?? 0f;

                    // 判断XZ轴距离是否小于双方碰撞器半径+全局碰撞器半径，是则继续检测
                    if (MathUtil.IsLessThanDistance(selfCenter, targetCenter,
                            selfRadius + targetRadius +
                            GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
                            MathUtil.TwoDimensionAxisType.XZ))
                    {
                        // 如果当前角色脚底处于特定高度，则认为是发生碰撞
                        if (character.Parameters.position.y > target.Parameters.position.y &&
                            character.Parameters.position.y <= targetTop.y + 0.1f)
                        {
                            collisions.Add(target);
                        }
                    }
                });
                
                if (collisions.Count != 0)
                {
                    _airborneCollideRecords.Add(character, collisions);
                }
            }
        }

        private void OnDestroy()
        {
            _mapManager.AfterMapLoad -= HandleMapChanged;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debug)
            {
                return;
            }

            _gameManager.Characters.ForEach(character =>
            {
                var characterController = character.CharacterController;
                if (!characterController)
                {
                    return;
                }

                var center = characterController.transform.position + characterController.center;
                var bottom = center - Vector3.up * (characterController.height / 2 + characterController.skinWidth);
                var top = center + Vector3.up * (characterController.height / 2 + characterController.skinWidth);
                var horizontalSize =
                    2 * (characterController.radius + GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius +
                         characterController.skinWidth);
                var verticalSize = characterController.height + 2 * characterController.skinWidth;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube((bottom + top) / 2, new Vector3(horizontalSize, verticalSize, horizontalSize));
            });
        }

        public void LimitCharacterMovement(CharacterObject self, ref Vector3 proactiveMovement,
            ref Vector3 reactiveMovement)
        {
            // 先检测被动移动，再检测主动移动
            var center = self.transform.position + (self.CharacterController?.center ?? Vector3.zero);
            reactiveMovement = CheckCharacterMovementInternal(center, reactiveMovement);
            proactiveMovement = CheckCharacterMovementInternal(center + reactiveMovement, proactiveMovement);

            return;

            Vector3 CheckCharacterMovementInternal(Vector3 selfCenter, Vector3 movement)
            {
                // // 如果角色移动后超出地图边界的X轴，就清除移动向量的X轴
                // if (selfCenter.x + movement.x > _mapCenter.x + _mapSize.x ||
                //     selfCenter.x + movement.x < _mapCenter.x - _mapSize.x)
                // {
                //     movement = new Vector3(0f, movement.y, movement.z);
                // }
                //
                // // 如果角色移动后超出地图边界的Z轴，就清除移动向量的Z轴
                // if (selfCenter.z + movement.z > _mapCenter.y + _mapSize.y ||
                //     selfCenter.z + movement.z < _mapCenter.y - _mapSize.y)
                // {
                //     movement = new Vector3(movement.x, movement.y, 0f);
                // }

                // 如果角色存在与其他角色的碰撞，就检查移动向量
                if (_movementCollideRecords.TryGetValue(self, out var collides) && collides != null &&
                    collides.Count != 0)
                {
                    // 检测移动后距离自身碰撞的角色的距离是否更近了，是则清除移动向量
                    collides.ForEach(collidedCharacter =>
                    {
                        if (collidedCharacter == self || collidedCharacter.IsGameObjectDestroyed())
                        {
                            return;
                        }

                        var targetCenter = collidedCharacter.Parameters.position +
                                           (collidedCharacter.CharacterController?.center ?? Vector3.zero);
                        var originHorizontalDistance =
                            Vector2.Distance(
                                new Vector2(selfCenter.x, selfCenter.z),
                                new Vector2(targetCenter.x, targetCenter.z)
                            );
                        var movementHorizontalDistance =
                            Vector2.Distance(
                                new Vector2(selfCenter.x + movement.x, selfCenter.z + movement.z),
                                new Vector2(targetCenter.x, targetCenter.z)
                            );
                        if (movementHorizontalDistance < originHorizontalDistance)
                        {
                            movement = new Vector3(0, movement.y, 0);
                        }
                    });
                }

                return movement;
            }
        }

        public bool HasCollidedCharacterInAirborne(CharacterObject self, out CharacterObject character)
        {
            character = null;
            if (_airborneCollideRecords.TryGetValue(self, out var collides)  && collides != null &&
                collides.Count != 0)
            {
                character = collides.First();
                return true;
            }

            return false;
        }

        private void HandleMapChanged()
        {
            if (_mapManager.Map)
            {
                var mapSnapshot = _mapManager.Map.Snapshot;
                _mapCenter = mapSnapshot.Center2D;
                _mapSize = mapSnapshot.Size;
            }
            else
            {
                _mapCenter = Vector2.zero;
                _mapSize = Vector2.zero;
            }
        }
    }
}