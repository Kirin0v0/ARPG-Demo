using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bullet.Data;
using Character;
using Character.Data;
using CollideDetection.Shape;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Bullet
{
    public class BulletObject : MonoBehaviour
    {
        [ReadOnly] public BulletInfo info;
        [ReadOnly] public GameObject prefab;
        [ReadOnly] public Vector3 prefabLocalPosition;
        [ReadOnly] public Quaternion prefabLocalRotation;
        [ReadOnly] public CharacterObject caster;
        [ReadOnly] public CharacterObject target;
        [ReadOnly] public Vector3 firePosition;
        [ReadOnly] public Quaternion fireRotation;
        [ReadOnly] public float speed;
        [ReadOnly] public float duration;
        [ReadOnly] public float timeElapsed;
        [NonSerialized] public BulletTargetFunction TargetFunction;
        [NonSerialized] public BulletLocomotionFunction LocomotionFunction;
        [ReadOnly] public float allowHitDelay;
        [ReadOnly] public float destroyDelay = 2f;
        [NonSerialized] public Dictionary<string, object> RuntimeParams;

        [Inject] private GameManager _gameManager;

        private readonly HashSet<Collider> _collideCollidersTick = new(); // 当前帧碰撞到碰撞器
        private readonly List<BulletHitRecord> _hitRecords = new(); // 子弹命中记录
        [SerializeField, ReadOnly] private int hp = 0; // 子弹生命值，每次命中减一，为0就销毁
        private bool _destroy = false;

        private AudioManager _audioManager; // 音频管理器
        private BaseCollideDetectionShapeObject _collideDetectionShape; // 碰撞检测组件
        private GameObject _prefab;

        private bool Debug
        {
            get
            {
                if (RuntimeParams.TryGetValue(BulletLauncher.Debug, out var debugParameter) &&
                    debugParameter is bool debug)
                {
                    return debug;
                }

                return false;
            }
        }

        public void Init()
        {
            if (_destroy)
            {
                return;
            }

            if (Debug)
            {
                DebugUtil.LogPurple($"子弹({info.id})执行初始化函数");
            }

            _collideCollidersTick.Clear();
            _hitRecords.Clear();
            hp = info.hitTimes;

            // 设置子弹目标
            target = TargetFunction?.Invoke(this, caster);

            // 初始化子弹开火位置和旋转
            transform.position = firePosition;
            transform.rotation = fireRotation;

            // 初始化子弹外观预设体
            if (prefab)
            {
                _prefab = GameObject.Instantiate(prefab, transform);
                _prefab.transform.localPosition = prefabLocalPosition;
                _prefab.transform.localRotation = prefabLocalRotation;
            }

            // 初始化音频管理器
            _audioManager = new GameObject("Game Audio Manager")
            {
                transform =
                {
                    parent = transform,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one,
                }
            }.AddComponent<GameAudioManager>();

            // 初始化碰撞检测
            switch (info.colliderType)
            {
                case BulletColliderType.Box:
                {
                    var boxObject = new GameObject("Collide Detection Box")
                    {
                        transform =
                        {
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    }.AddComponent<CollideDetectionShapeBoxObject>();
                    boxObject.SetParams(
                        transform,
                        info.ColliderTypeParams[0] as Vector3? ?? default,
                        quaternion.Euler(info.ColliderTypeParams[1] as Vector3? ?? Vector3.zero),
                        info.ColliderTypeParams[2] as Vector3? ?? default,
                        false
                    );
                    boxObject.debug = Debug;
                    _collideDetectionShape = boxObject;
                }
                    break;
                case BulletColliderType.Sphere:
                {
                    var sphereObject = new GameObject("Collide Detection Sphere")
                    {
                        transform =
                        {
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    }.AddComponent<CollideDetectionShapeSphereObject>();
                    sphereObject.SetParams(transform, info.ColliderTypeParams[0] as Vector3? ?? default,
                        (float)info.ColliderTypeParams[1], false);
                    sphereObject.debug = Debug;
                    _collideDetectionShape = sphereObject;
                }
                    break;
            }

            Tick(0);
        }

        public void Tick(float deltaTime)
        {
            timeElapsed += deltaTime;

            if (_destroy)
            {
                return;
            }

            if (Debug)
            {
                DebugUtil.LogPurple($"子弹({info.id})执行帧函数，时间间隔为{timeElapsed}");
            }

            if (_prefab && _prefab.TryGetComponent<ParticleSystem>(out var prefabParticleSystem))
            {
                prefabParticleSystem.Simulate(timeElapsed);
                prefabParticleSystem.Pause();
            }

            if (timeElapsed == 0)
            {
                info.OnCreate?.Invoke(this);
            }

            // 处理创建后命中允许时间和命中记录
            allowHitDelay -= deltaTime;
            HandleHitRecords(deltaTime);

            // 执行子弹运动逻辑
            LocomotionFunction?.Invoke(timeElapsed, this, target, deltaTime);

            // 遍历碰撞物体执行碰撞逻辑
            _collideCollidersTick.Clear();
            _collideDetectionShape.Detect(
                HandleCollide,
                GlobalRuleSingletonConfigSO.Instance.bulletObstacleLayer |
                GlobalRuleSingletonConfigSO.Instance.characterHitLayer
            );
            TraverseCollideGameObjects();

            if (timeElapsed >= duration)
            {
                _gameManager.DestroyBullet(this);
            }
        }

        /// <summary>
        /// 逻辑销毁函数
        /// </summary>
        public void DestroyOnLogic()
        {
            if (_destroy)
            {
                return;
            }

            if (Debug)
            {
                DebugUtil.LogPurple($"子弹({info.id})执行逻辑销毁函数");
            }

            _destroy = true;
            info.OnDestroy?.Invoke(this);
            if (Application.isPlaying)
            {
                GameObject.Destroy(_prefab);
                GameObject.Destroy(_collideDetectionShape.gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(_prefab);
                GameObject.DestroyImmediate(_collideDetectionShape.gameObject);
            }

            _prefab = null;
            _collideDetectionShape = null;
        }

        public void SetTimeScale(float timeScale)
        {
            _audioManager.SetSoundsPitch(timeScale);
        }

        public int PlaySound(
            AudioClip audioClip,
            bool loop = false,
            float volume = 1f
        )
        {
            return _audioManager.PlaySound(
                audioClip,
                loop,
                volume,
                spatialBlend: 1f,
                minDistance: GlobalRuleSingletonConfigSO.Instance.bulletSoundMinDistance,
                maxDistance: GlobalRuleSingletonConfigSO.Instance.bulletSoundMaxDistance
            );
        }

        public void StopSound(int id)
        {
            _audioManager.StopSound(id);
        }

        /// <summary>
        /// 实际销毁回调函数
        /// </summary>
        private void OnDestroy()
        {
            if (Debug)
            {
                DebugUtil.LogPurple($"子弹({info.id})执行实际销毁函数");
            }

            // 在实际销毁时才销毁音频管理器，因为在逻辑销毁后可能会播放销毁音效
            _audioManager.ClearSounds();
        }

        private void HandleCollide(Collider collider)
        {
            _collideCollidersTick.Add(collider);
        }

        private void HandleHitRecords(float deltaTime)
        {
            var index = 0;
            while (index < _hitRecords.Count)
            {
                _hitRecords[index].HitColdDown -= deltaTime;
                // 冷却转好了或者命中目标不存在了，都删除记录
                if (_hitRecords[index].HitColdDown <= 0f || !_hitRecords[index].Target)
                {
                    _hitRecords.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void TraverseCollideGameObjects()
        {
            var touchObstacle = false;
            foreach (var collider in _collideCollidersTick)
            {
                // 存在遍历时子弹被销毁，则停止遍历
                if (_destroy)
                {
                    return;
                }

                // 先判断是否在碰撞障碍物时销毁，是则记录障碍物，等待所有碰撞逻辑处理完毕后销毁子弹
                if (info.destroyOnObstacle)
                {
                    if ((info.obstacleLayers & (1 << collider.gameObject.layer)) != 0)
                    {
                        // 记录当前帧碰撞到了障碍物
                        touchObstacle = true;
                    }
                }

                // 如果未获取到受击角色，就直接跳过
                if (!collider.TryGetHitCharacter(out var characterObject, out var damageMultiplier, out var priority))
                {
                    return;
                }

                // 如果允许命中角色，则执行命中逻辑
                if (AllowHit(characterObject))
                {
                    HitCharacter(characterObject);
                }
            }

            // 碰撞障碍物后销毁子弹
            if (touchObstacle)
            {
                _gameManager.DestroyBullet(this);
            }
        }

        private bool AllowHit(CharacterObject character)
        {
            if (_destroy)
            {
                return false;
            }

            if (timeElapsed < allowHitDelay)
            {
                return false;
            }

            if (_hitRecords.Any(record => record.Target == character))
            {
                return false;
            }

            if (character.Parameters.dead || character.Parameters.immune)
            {
                return false;
            }

            return (info.hitEnemy && caster.Parameters.side != character.Parameters.side) ||
                   (info.hitAlly && caster.Parameters.side == character.Parameters.side && caster != character) ||
                   (info.hitSelf && caster == character);
        }

        private void HitCharacter(CharacterObject character)
        {
            if (Debug)
            {
                DebugUtil.LogPurple($"子弹({info.id})命中角色({character.Parameters.DebugName})");
            }

            hp -= 1;
            info.OnHit?.Invoke(this, character);

            // 如果子弹生命值为0，则销毁子弹
            if (hp <= 0)
            {
                _gameManager.DestroyBullet(this);
            }
            else
            {
                _hitRecords.Add(new BulletHitRecord()
                {
                    HitColdDown = info.hitColdDown,
                    Target = character,
                });
            }
        }
    }
}