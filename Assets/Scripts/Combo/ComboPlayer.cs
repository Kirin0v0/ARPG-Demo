using System;
using System.Collections.Generic;
using System.Linq;
using Action;
using Camera.Data;
using Character;
using Common;
using Damage;
using Damage.Data;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Humanoid;
using Humanoid.Weapon.Data;
using Package.Data;
using UnityEngine;

namespace Combo
{
    [Flags]
    public enum ComboStage
    {
        Idle = 1 << 0,
        Start = 1 << 1,
        Anticipation = 1 << 2,
        Judgment = 1 << 3,
        Recovery = 1 << 4,
        End = 1 << 5,
        Stop = 1 << 6,
    }

    public class ComboPlayer : IComboPlay
    {
        private ComboStage _stage = ComboStage.Idle;

        public ComboStage Stage
        {
            private set
            {
                if (value <= Stage)
                {
                    return;
                }

                _stage = value;

                // 如果使用流程时间点配置霸体状态，每次阶段改变时检查是否开始/结束霸体
                if (_endureExitsInCombo && _comboConfig.endureType == ComboStateConfigurationType.Process)
                {
                    switch (_stage)
                    {
                        case ComboStage.Start:
                        {
                            if (_startEndureTimePoint == ComboStateProcessTimePoint.Start)
                            {
                                _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endEndureTimePoint == ComboStateProcessTimePoint.Start)
                            {
                                _character.StateAbility?.StopEndure(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Anticipation:
                        {
                            if (_startEndureTimePoint == ComboStateProcessTimePoint.Anticipation)
                            {
                                _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endEndureTimePoint ==
                                ComboStateProcessTimePoint.Anticipation)
                            {
                                _character.StateAbility?.StopEndure(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Judgment:
                        {
                            if (_startEndureTimePoint == ComboStateProcessTimePoint.Judgment)
                            {
                                _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endEndureTimePoint == ComboStateProcessTimePoint.Judgment)
                            {
                                _character.StateAbility?.StopEndure(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Recovery:
                        {
                            if (_startEndureTimePoint == ComboStateProcessTimePoint.Recovery)
                            {
                                _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endEndureTimePoint == ComboStateProcessTimePoint.Recovery)
                            {
                                _character.StateAbility?.StopEndure(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.End:
                        case ComboStage.Stop:
                        {
                            if (_startEndureTimePoint == ComboStateProcessTimePoint.End)
                            {
                                _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endEndureTimePoint == ComboStateProcessTimePoint.End)
                            {
                                _character.StateAbility?.StopEndure(_comboConfig.Name);
                            }
                        }
                            break;
                    }
                }

                // 如果使用流程时间点配置不可破防状态，每次阶段改变时检查是否开始/结束不可破防
                if (_unbreakableExitsInCombo && _comboConfig.unbreakableType == ComboStateConfigurationType.Process)
                {
                    switch (_stage)
                    {
                        case ComboStage.Start:
                        {
                            if (_startUnbreakableTimePoint == ComboStateProcessTimePoint.Start)
                            {
                                _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endUnbreakableTimePoint == ComboStateProcessTimePoint.Start)
                            {
                                _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Anticipation:
                        {
                            if (_startUnbreakableTimePoint == ComboStateProcessTimePoint.Anticipation)
                            {
                                _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endUnbreakableTimePoint == ComboStateProcessTimePoint.Anticipation)
                            {
                                _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Judgment:
                        {
                            if (_startUnbreakableTimePoint == ComboStateProcessTimePoint.Judgment)
                            {
                                _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endUnbreakableTimePoint == ComboStateProcessTimePoint.Judgment)
                            {
                                _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.Recovery:
                        {
                            if (_startUnbreakableTimePoint == ComboStateProcessTimePoint.Recovery)
                            {
                                _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endUnbreakableTimePoint == ComboStateProcessTimePoint.Recovery)
                            {
                                _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                            }
                        }
                            break;
                        case ComboStage.End:
                        case ComboStage.Stop:
                        {
                            if (_startUnbreakableTimePoint == ComboStateProcessTimePoint.End)
                            {
                                _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                            }

                            if (_endUnbreakableTimePoint == ComboStateProcessTimePoint.End)
                            {
                                _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                            }
                        }
                            break;
                    }
                }

                if (_stage is ComboStage.End or ComboStage.Stop)
                {
                    // 这里无论是否配置霸体和不可破防，或是配置为流程还是事件，都会在连招结束时尝试停止霸体和不可破防
                    _character.StateAbility?.StopEndure(_comboConfig.Name);
                    _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                }

                OnStageChanged?.Invoke(value);
            }
            get => _stage;
        }

        public event Action<ComboStage> OnStageChanged;

        private readonly ComboConfig _comboConfig;
        private readonly CharacterObject _character;
        private readonly ActionClipPlayer _actionClipPlayer;
        private readonly GameManager _gameManager;
        private readonly DamageManager _damageManager;
        private readonly AlgorithmManager _algorithmManager;

        // 全局共享碰撞检测委托
        private readonly Func<string, Collider, bool> _globalSharedColliderDetectionDelegate;

        // 非全局的碰撞组检测记录，以碰撞组id为键
        private readonly Dictionary<string, List<(GameObject gameObject, float countdown)>> _detectedObjectRecords =
            new();

        // 连招中是否存在霸体状态
        private readonly bool _endureExitsInCombo;

        // 触发霸体的流程时间点
        private readonly ComboStateProcessTimePoint _startEndureTimePoint;
        private readonly ComboStateProcessTimePoint _endEndureTimePoint;

        // 触发霸体和不可破防的事件名称
        private readonly string _startEndureEventName;
        private readonly string _endEndureEventName;

        // 连招中是否存在不可破防状态
        private readonly bool _unbreakableExitsInCombo;

        // 触发霸体和不可破防的流程时间点
        private readonly ComboStateProcessTimePoint _startUnbreakableTimePoint;
        private readonly ComboStateProcessTimePoint _endUnbreakableTimePoint;

        // 触发不可破防的事件名称
        private readonly string _startUnbreakableEventName;
        private readonly string _endUnbreakableEventName;

        // 连招帧间隔
        private float _deltaTime;

        public ComboPlayer(
            ComboConfig comboConfig,
            CharacterObject character,
            GameManager gameManager,
            DamageManager damageManager,
            AlgorithmManager algorithmManager,
            Func<string, Collider, bool> globalSharedColliderDetectionDelegate = null
        )
        {
            _comboConfig = comboConfig;
            _character = character;
            _actionClipPlayer = new ActionClipPlayer(
                comboConfig.actionClip,
                character,
                GlobalRuleSingletonConfigSO.Instance.characterHitLayer,
                HandleCollide,
                comboConfig.loop
            );
            _gameManager = gameManager;
            _damageManager = damageManager;
            _algorithmManager = algorithmManager;
            _globalSharedColliderDetectionDelegate = globalSharedColliderDetectionDelegate;

            // 监听阶段变化
            _actionClipPlayer.OnStartStage += () => { Stage = ComboStage.Start; };
            _actionClipPlayer.OnAnticipationStage += () => { Stage = ComboStage.Anticipation; };
            _actionClipPlayer.OnJudgmentStage += () => { Stage = ComboStage.Judgment; };
            _actionClipPlayer.OnRecoveryStage += () => { Stage = ComboStage.Recovery; };
            _actionClipPlayer.OnEndStage += () =>
            {
                ClearData();
                Stage = ComboStage.End;
            };
            _actionClipPlayer.OnStopStage += () =>
            {
                ClearData();
                Stage = ComboStage.Stop;
            };

            #region 检查霸体配置是否合理，防止配置错误导致角色一直处于霸体状态

            switch (comboConfig.endureType)
            {
                case ComboStateConfigurationType.Process:
                {
                    _startEndureTimePoint = comboConfig.startEndureTimePoint;
                    _endUnbreakableTimePoint = comboConfig.endEndureTimePoint < _startEndureTimePoint
                        ? _startEndureTimePoint
                        : comboConfig.endEndureTimePoint;
                    _endureExitsInCombo = true;
                }
                    break;
                case ComboStateConfigurationType.Event:
                {
                    var startEndureEvent = comboConfig.actionClip.events.eventClips.Find(eventClip =>
                        eventClip.name == comboConfig.startEndureEventName);
                    var endEndureEvent = comboConfig.actionClip.events.eventClips.Find(eventClip =>
                        eventClip.name == comboConfig.endEndureEventName);
                    if (startEndureEvent != null && endEndureEvent != null)
                    {
                        _startEndureEventName = comboConfig.startEndureEventName;
                        _endEndureEventName = startEndureEvent.time > endEndureEvent.time
                            ? comboConfig.startEndureEventName
                            : comboConfig.endEndureEventName;
                        _endureExitsInCombo = true;
                    }
                }
                    break;
            }

            #endregion

            #region 检查不可破防配置是否合理，防止配置错误导致角色一直处于不可破防状态

            switch (comboConfig.unbreakableType)
            {
                case ComboStateConfigurationType.Process:
                {
                    _startUnbreakableTimePoint = comboConfig.startUnbreakableTimePoint;
                    _endUnbreakableTimePoint = comboConfig.endUnbreakableTimePoint < _startUnbreakableTimePoint
                        ? _startUnbreakableTimePoint
                        : comboConfig.endUnbreakableTimePoint;
                    _unbreakableExitsInCombo = true;
                }
                    break;
                case ComboStateConfigurationType.Event:
                {
                    var startUnbreakableEvent = comboConfig.actionClip.events.eventClips.Find(eventClip =>
                        eventClip.name == comboConfig.startUnbreakableEventName);
                    var endUnbreakableEvent = comboConfig.actionClip.events.eventClips.Find(eventClip =>
                        eventClip.name == comboConfig.endUnbreakableEventName);
                    if (startUnbreakableEvent != null && endUnbreakableEvent != null)
                    {
                        _startUnbreakableEventName = comboConfig.startUnbreakableEventName;
                        _endUnbreakableEventName = startUnbreakableEvent.time > endUnbreakableEvent.time
                            ? comboConfig.startUnbreakableEventName
                            : comboConfig.endUnbreakableEventName;
                        _unbreakableExitsInCombo = true;
                    }
                }
                    break;
            }

            #endregion

            // 如果使用事件配置霸体状态，则注册动作播放器事件监听器
            if (_endureExitsInCombo && comboConfig.endureType == ComboStateConfigurationType.Event)
            {
                _actionClipPlayer.RegisterEventListener((name, type, payload) =>
                {
                    if (_startEndureEventName == name)
                    {
                        _character.StateAbility?.StartEndure(_comboConfig.Name, float.MaxValue);
                    }

                    if (_endEndureEventName == name)
                    {
                        _character.StateAbility?.StopEndure(_comboConfig.Name);
                    }
                });
            }

            // 如果使用事件配置不可破防状态，则注册动作播放器事件监听器
            if (_unbreakableExitsInCombo && comboConfig.endureType == ComboStateConfigurationType.Event)
            {
                _actionClipPlayer.RegisterEventListener((name, type, payload) =>
                {
                    if (_startUnbreakableEventName == name)
                    {
                        _character.StateAbility?.StartUnbreakable(_comboConfig.Name, float.MaxValue);
                    }

                    if (_endUnbreakableEventName == name)
                    {
                        _character.StateAbility?.StopUnbreakable(_comboConfig.Name);
                    }
                });
            }
        }

        public void Start()
        {
            _actionClipPlayer.Start();
        }

        public void Tick(float deltaTime)
        {
            _deltaTime = deltaTime;
            CalculateCollideInterval();
            _actionClipPlayer.Tick(deltaTime);
        }

        public void Stop()
        {
            _actionClipPlayer.Stop();
        }

        private void HandleCollide(string groupId, Collider collider)
        {
            // 只有与角色碰撞才会执行后续逻辑
            if (!collider.TryGetHitCharacter(out var targetCharacter, out var damageMultiplier, out var priority))
            {
                return;
            }

            // 屏蔽自身及友方碰撞
            if (targetCharacter == _character || targetCharacter.Parameters.side == _character.Parameters.side)
            {
                return;
            }

            // 判断当前碰撞组是否是全局共享的碰撞组
            if (_comboConfig.globalSharedColliderGroups.Contains(groupId))
            {
                // 是则由全局共享委托处理，如果处理失败就直接返回
                if (_globalSharedColliderDetectionDelegate == null ||
                    !_globalSharedColliderDetectionDelegate.Invoke(groupId, collider))
                {
                    return;
                }
            }
            else
            {
                // 否则由内部处理
                // 获取碰撞组的数据
                var colliderDetectionInterval = _comboConfig.colliderDetectionInterval;
                var colliderDetectionMaximum = _comboConfig.colliderDetectionMaximum;
                var index = _comboConfig.colliderGroupSettings.FindIndex(setting => setting.groupId == groupId);
                if (index != -1)
                {
                    colliderDetectionInterval = _comboConfig.colliderGroupSettings[index].detectionInterval;
                    colliderDetectionMaximum = _comboConfig.colliderGroupSettings[index].detectionMaximum;
                }

                // 判断当前碰撞组是否存在碰撞记录
                if (_detectedObjectRecords.TryGetValue(groupId, out var detectedRecords))
                {
                    // 判断已记录的碰撞体数量是否达到最大值，是则不响应碰撞
                    if (detectedRecords.Count >= colliderDetectionMaximum)
                    {
                        return;
                    }

                    // 判断该碰撞体是否已经被记录
                    if (detectedRecords.Any((tuple => tuple.gameObject == collider.gameObject)))
                    {
                        return;
                    }

                    detectedRecords.Add((collider.gameObject, colliderDetectionInterval));
                }
                else
                {
                    _detectedObjectRecords.Add(groupId, new List<(GameObject gameObject, float countdown)>
                    {
                        new ValueTuple<GameObject, float>(collider.gameObject, colliderDetectionInterval)
                    });
                }
            }

            HitTargetCharacter();
            HitFreeze();
            ShakeCameraIfPlayerAttackOrHit();
            AttackTargetCharacter();

            return;

            void HitTargetCharacter()
            {
                _comboConfig.hitAudioSettings.ForEach(hitAudioSetting =>
                {
                    _character.AudioAbility?.PlaySound(hitAudioSetting.AudioClip, false, hitAudioSetting.volume);
                });
            }

            void HitFreeze()
            {
                if (_comboConfig.openHitFreeze)
                {
                    _gameManager.AddTimeScaleComboCommand(
                        $"{_character.Parameters.name}{_character.Parameters.id}PlayCombo{_comboConfig.Name}",
                        _comboConfig.hitFreezeDuration,
                        _comboConfig.hitFreezeTimeScale
                    );
                }
            }

            void ShakeCameraIfPlayerAttackOrHit()
            {
                if (_character == _gameManager.Player || targetCharacter == _gameManager.Player)
                {
                    if (_comboConfig.hitShake is CameraShakeUniformData { useDamageDirectionAsVelocity: true } uniformData)
                    {
                        // 玩家作为攻击方，震动速度采用伤害朝向
                        if (_character == _gameManager.Player)
                        {
                            uniformData.SetVelocity(Vector3.ProjectOnPlane(
                                collider.ClosestPoint(_character.Visual.Center.position) -
                                _character.Visual.Center.position,
                                Vector3.up).normalized);
                        }

                        // 玩家作为受击方，震动速度采用被伤害朝向
                        if (targetCharacter == _gameManager.Player)
                        {
                            uniformData.SetVelocity(-Vector3.ProjectOnPlane(
                                collider.ClosestPoint(_character.Visual.Center.position) -
                                _character.Visual.Center.position,
                                Vector3.up).normalized);
                        }
                    }

                    _comboConfig.hitShake?.GenerateShake(_character.Parameters.position);
                }
            }

            void AttackTargetCharacter()
            {
                DebugUtil.LogOrange($"角色({_character.Parameters.DebugName})的连招({_comboConfig.Name})与角色({targetCharacter.Parameters.DebugName})发生碰撞");

                // 获取连招方式
                var damageMethod = new DamageComboMethod
                {
                    Name = _comboConfig.Name,
                    WeaponType = DamageComboWeaponType.Self,
                    CollideDetectionChannelId = groupId
                };
                if (_character is HumanoidCharacterObject humanoidCharacterObject &&
                    humanoidCharacterObject.WeaponAbility &&
                    humanoidCharacterObject.WeaponAbility.AggressiveWeaponSlot != null)
                {
                    damageMethod = new DamageComboMethod
                    {
                        Name = _comboConfig.Name,
                        WeaponType = humanoidCharacterObject.WeaponAbility.AggressiveWeaponSlot.Data.Type switch
                        {
                            HumanoidWeaponType.Sword => DamageComboWeaponType.Sword,
                            HumanoidWeaponType.Shield => DamageComboWeaponType.Shield,
                            HumanoidWeaponType.Katana => DamageComboWeaponType.Katana,
                            _ => DamageComboWeaponType.Self,
                        },
                        CollideDetectionChannelId = groupId
                    };
                }

                // 我们规定连招伤害全是物理伤害
                var comboDamage = _algorithmManager.DamageAttackConvertSO.ConvertToDamageValue(
                    originFixedDamage: new DamageValue
                    {
                        physics = _comboConfig.fixedPhysicsDamage,
                    },
                    originDamageTimes: new DamageValueMultiplier
                    {
                        physics = _comboConfig.physicsDamageMultiplier,
                    },
                    damageType: DamageType.DirectDamage,
                    attacker: _character.Parameters
                );
                // 计算暴击率
                var criticalRate = _algorithmManager.DamageCriticalRateCalculateSO.CalculateCriticalRate(
                    DamageType.DirectDamage,
                    _character.Parameters.property
                );
                // 计算伤害方向
                var damageDirection =
                    Vector3.ProjectOnPlane(
                        collider.ClosestPoint(_character.Visual.Center.position) - _character.Visual.Center.position,
                        Vector3.up);
                // 最终添加伤害
                _damageManager.AddDamage(
                    source: _character,
                    target: targetCharacter,
                    method: damageMethod,
                    type: DamageType.DirectDamage,
                    value: comboDamage,
                    resourceMultiplier: _comboConfig.resourceMultiplier,
                    criticalRate: criticalRate,
                    direction: damageDirection
                );
            }
        }

        private void CalculateCollideInterval()
        {
            for (int i = 0; i < _detectedObjectRecords.Keys.Count; i++)
            {
                var key = _detectedObjectRecords.Keys.ElementAt(i);
                var detectedRecords = _detectedObjectRecords[key];
                detectedRecords.RemoveAll(tuple =>
                {
                    tuple.countdown -= _deltaTime;
                    return tuple.countdown <= 0f;
                });
            }
        }

        private void ClearData()
        {
            // 清除碰撞检测数据
            _detectedObjectRecords.Clear();
        }
    }
}