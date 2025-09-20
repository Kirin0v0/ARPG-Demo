using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AoE.Data;
using Character;
using Character.Data;
using CollideDetection.Shape;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace AoE
{
    public class AoEObject : MonoBehaviour
    {
        [ReadOnly] public AoEInfo info;
        [ReadOnly] public GameObject prefab;
        [ReadOnly] public float prefabSimulationSpeed;
        [ReadOnly] public Vector3 prefabLocalPosition;
        [ReadOnly] public Quaternion prefabLocalRotation;
        [ReadOnly] public CharacterObject caster;
        [ReadOnly] public bool fixedPositionAndRotation;
        [ReadOnly] public float duration;
        [ReadOnly] public float timeElapsed;
        [ReadOnly] public float destroyDelay;
        [NonSerialized] public Dictionary<string, object> RuntimeParams;
        [ReadOnly] public List<CharacterObject> charactersInAoE = new();

        [Inject] private GameManager _gameManager;

        private int _tick;
        private int _endTick;
        private bool _destroy = false;

        private AudioManager _audioManager; // 音频管理器
        private BaseCollideDetectionShapeObject _collideDetectionShape; // 碰撞检测组件
        private GameObject _prefab;

        private bool Debug
        {
            get
            {
                if (RuntimeParams.TryGetValue(AoELauncher.Debug, out var debugParameter) &&
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
                DebugUtil.LogPurple($"AoE({info.id})执行初始化函数");
            }

            timeElapsed = 0;
            _tick = -1;
            _endTick = (int)(duration / info.tickTime);

            // 初始化AOE特效预设体
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
                case AoEColliderType.Box:
                {
                    var instance = new GameObject("Collide Detection Box")
                    {
                        transform =
                        {
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var boxObject = instance.AddComponent<CollideDetectionShapeBoxObject>();
                    boxObject.SetParams(transform, info.ColliderTypeParams[0] as Vector3? ?? default,
                        Quaternion.Euler(info.ColliderTypeParams[1] as Vector3? ?? Vector3.zero),
                        info.ColliderTypeParams[2] as Vector3? ?? default,
                        fixedPositionAndRotation);
                    boxObject.debug = Debug;
                    _collideDetectionShape = boxObject;
                }
                    break;
                case AoEColliderType.Sphere:
                {
                    var instance = new GameObject("Collide Detection Sphere")
                    {
                        transform =
                        {
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var sphereObject = instance.AddComponent<CollideDetectionShapeSphereObject>();
                    sphereObject.SetParams(transform, info.ColliderTypeParams[0] as Vector3? ?? default,
                        (float)info.ColliderTypeParams[1], fixedPositionAndRotation);
                    sphereObject.debug = Debug;
                    _collideDetectionShape = sphereObject;
                }
                    break;
                case AoEColliderType.Sector:
                {
                    var instance = new GameObject("Collide Detection Sector")
                    {
                        transform =
                        {
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        }
                    };
                    var sectorObject = instance.AddComponent<CollideDetectionShapeSectorObject>();
                    sectorObject.SetParams(
                        transform,
                        info.ColliderTypeParams[0] as Vector3? ?? default,
                        Quaternion.Euler(info.ColliderTypeParams[1] as Vector3? ?? Vector3.zero),
                        (float)info.ColliderTypeParams[2],
                        (float)info.ColliderTypeParams[3],
                        (float)info.ColliderTypeParams[4],
                        (float)info.ColliderTypeParams[5],
                        (float)info.ColliderTypeParams[6],
                        fixedPositionAndRotation
                    );
                    sectorObject.debug = Debug;
                    _collideDetectionShape = sectorObject;
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
                DebugUtil.LogPurple($"AoE({info.id})执行帧函数，时间间隔为{timeElapsed}");
            }

            if (_prefab && _prefab.TryGetComponent<ParticleSystem>(out var prefabParticleSystem))
            {
                prefabParticleSystem.Simulate(timeElapsed * prefabSimulationSpeed);
                prefabParticleSystem.Pause();
            }

            #region 检查AOE区域内的进入角色、退出角色以及保留角色

            // 丢弃旧列表并重置新列表
            var recentCharactersInAoE = charactersInAoE;
            charactersInAoE = new List<CharacterObject>();
            // 使用碰撞对象检测碰撞
            _collideDetectionShape.Detect(
                HandleCollide,
                GlobalRuleSingletonConfigSO.Instance.characterHitLayer
            );
            // 对比新旧数据获取不同角色列表
            var enterCharacters =
                charactersInAoE.Where(character => !recentCharactersInAoE.Contains(character)).ToList();
            var stayCharacters = recentCharactersInAoE.Where(character => charactersInAoE.Contains(character)).ToList();
            var leaveCharacters =
                recentCharactersInAoE.Where(character => !charactersInAoE.Contains(character)).ToList();

            #endregion

            if (timeElapsed == 0)
            {
                info.OnCreate?.Invoke(this);
            }

            if (enterCharacters.Count > 0)
            {
                if (Debug)
                {
                    enterCharacters.ForEach(character =>
                    {
                        DebugUtil.LogPurple($"角色({character.Parameters.DebugName})进入AoE({info.id})范围内");
                    });
                }

                info.OnCharactersEnter?.Invoke(this, enterCharacters);
            }

            if (stayCharacters.Count > 0)
            {
                if (Debug)
                {
                    stayCharacters.ForEach(character =>
                    {
                        DebugUtil.LogPurple($"角色({character.Parameters.DebugName})停留在AoE({info.id})范围内");
                    });
                }

                info.OnCharactersStay?.Invoke(this, stayCharacters);
            }

            if (leaveCharacters.Count > 0)
            {
                if (Debug)
                {
                    leaveCharacters.ForEach(character =>
                    {
                        DebugUtil.LogPurple($"角色({character.Parameters.DebugName})退出AoE({info.id})范围内");
                    });
                }

                info.OnCharactersLeave?.Invoke(this, leaveCharacters);
            }

            // 当帧间隔不为0才会执行OnTick
            if (info.tickTime > 0)
            {
                var newTick = (int)(timeElapsed / info.tickTime);
                // 追赶帧并执行帧函数，追赶至最后一帧，起始执行一帧，后续每隔一段时间执行一帧
                while (_tick < newTick && _tick <= _endTick)
                {
                    info.OnTick?.Invoke(this);
                    _tick++;
                }
            }

            if (timeElapsed >= duration)
            {
                _gameManager.DestroyAoE(this);
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
                DebugUtil.LogPurple($"AoE({info.id})执行逻辑销毁函数");
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
                minDistance: GlobalRuleSingletonConfigSO.Instance.aoeSoundMinDistance,
                maxDistance: GlobalRuleSingletonConfigSO.Instance.aoeSoundMaxDistance
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
                DebugUtil.LogPurple($"AoE({info.id})执行实际销毁函数");
            }

            // 在实际销毁时才销毁音频管理器，因为在逻辑销毁后可能会播放销毁音效
            _audioManager.ClearSounds();
        }

        private void HandleCollide(Collider collider)
        {
            if (!collider.TryGetHitCharacter(out var characterObject, out var damageMultiplier, out var priority))
            {
                return;
            }

            if (AllowHit(characterObject))
            {
                charactersInAoE.Add(characterObject);
            }
        }

        private bool AllowHit(CharacterObject character)
        {
            if (_destroy)
            {
                return false;
            }

            if (charactersInAoE.Contains(character))
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
    }
}