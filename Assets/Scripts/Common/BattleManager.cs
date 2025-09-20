using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Character;
using Character.Data;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Util;
using Framework.Core.Extension;
using JetBrains.Annotations;
using Map;
using ParadoxNotion;
using Player;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Common
{
    [Serializable]
    public class BattleMusic
    {
        public string ownPrototype = "";
        public AudioClip music;
        public int priority = 0;
        [Range(0, 1f)] public float volume = 1f;
    }

    public enum BattleTriggerType
    {
        Standoff, // 对峙触发
        SneakAttack, // 偷袭触发
    }

    public enum BattleLevel
    {
        Easy, // 整场战斗仅存在普通敌人
        Normal, // 整场战斗存在精英敌人
        Hard // 整场战斗存在首领敌人
    }

    [Serializable]
    public class BattleInfo
    {
        public string id;
        public float startTime;
        public float endTime;
        public BattleTriggerType triggerType;
        public CharacterSide triggerSide;
        public CharacterObject triggerCharacter;
        public Vector2 fieldCenter;
        public float fieldRadius;
        public GameObject filedEffect;
        public List<CharacterObject> totalCharacters;
        public List<CharacterObject> activeCharacters;
        public BattleLevel level;
        public float[,] damageRecords; // 战斗伤害记录，记录每个角色对其他角色的hp数值（伤害为负数，治疗为正数），索引为全部角色列表的索引

        public bool IsCharacterBeActiveInBattle(PlayerCharacterObject player)
        {
            foreach (var activeCharacter in activeCharacters)
            {
                if (activeCharacter == player)
                {
                    return true;
                }
            }

            return false;
        }

        public bool InBattleField(Vector3 position)
        {
            return Vector2.Distance(fieldCenter, new Vector2(position.x, position.z)) <= fieldRadius;
        }

        public Dictionary<CharacterObject, float> GetTargetToOthersDamageRecords(CharacterObject target)
        {
            var index = totalCharacters.FindIndex(character => character == target);
            // 记录中不存在目标角色则直接返回空字典
            if (index == -1)
            {
                return new();
            }

            var records = new Dictionary<CharacterObject, float>();
            for (var j = 0; j < damageRecords.GetLength(1); j++)
            {
                var other = totalCharacters[j];
                records.Add(other, damageRecords[index, j]);
            }

            return records;
        }

        public Dictionary<CharacterObject, float> GetOthersToTargetDamageRecords(CharacterObject target)
        {
            var index = totalCharacters.FindIndex(character => character == target);
            // 记录中不存在目标角色则直接返回空字典
            if (index == -1)
            {
                return new();
            }

            var records = new Dictionary<CharacterObject, float>();
            for (var j = 0; j < damageRecords.GetLength(0); j++)
            {
                var other = totalCharacters[j];
                records.Add(other, damageRecords[j, index]);
            }

            return records;
        }

        public (List<CharacterObject> enemies, List<CharacterObject> allies) GetSideCharacters(
            CharacterObject pivotCharacter)
        {
            var enemies = new List<CharacterObject>();
            var allies = new List<CharacterObject>();
            activeCharacters.ForEach(battleCharacter =>
            {
                if (battleCharacter == pivotCharacter)
                {
                    return;
                }

                if (battleCharacter.Parameters.side == pivotCharacter.Parameters.side)
                {
                    allies.Add(battleCharacter);
                }
                else
                {
                    enemies.Add(battleCharacter);
                }
            });
            return (enemies, allies);
        }
    }

    public class BattleManager : SerializedMonoBehaviour, IGameCharacterLifecycleOptimization
    {
        private class BattlePlayingMusic
        {
            public int OwnId;
            public int MusicId;
        }

        /// <summary>
        /// 战斗开始/结束事件
        /// </summary>
        public event Action<BattleInfo> OnBattleStarted;

        public event Action<BattleInfo> OnBattleFinished;

        /// <summary>
        /// 战斗等级升级事件
        /// </summary>
        public event Action<BattleInfo> OnBattleLevelUpgraded;

        /// <summary>
        /// 角色进入战斗/退出战斗（包含逃离、死亡以及结束战斗）
        /// </summary>
        public event Action<BattleInfo, CharacterObject> OnCharacterJoinBattle;

        public event Action<BattleInfo, CharacterObject> OnCharacterExitBattle;

        /// <summary>
        /// 玩家敌人死亡函数，这里我们认为在战斗中敌人死亡都算作玩家杀死对方
        /// </summary>
        public event Action<CharacterObject> OnPlayerKillEnemyInBattle;

        [Title("场地配置")] [SerializeField] [InfoBox("战斗场地最小半径")]
        private float minFieldRadius = 10f;

        [SerializeField] private GameObject fieldWarningEffect;

        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;
        [Inject] private AudioManager _audioManager;

        private int _battleIdSeed = 0;

        private readonly Dictionary<string, BattleInfo> _activeBattleInfos = new();
        private readonly Dictionary<string, BattleInfo> _completedBattleInfos = new();

        [Title("战斗音乐")] [SerializeField] private AudioClip universalBattleMusic;
        [SerializeField] private int universalBattleMusicPriority = 10;
        [SerializeField, Range(0, 1f)] private float universalBattleMusicVolume = 1f;
        [SerializeField] private Dictionary<string, BattleMusic> specifiedBattleMusic = new(); // 配置角色原型对应的战斗音乐
        private readonly Dictionary<int, BattlePlayingMusic> _playingBattleMusic = new(); // 记录正在播放的战斗音乐，键为角色id，值为音乐数据

        [Title("实时战斗数据")]
        [ShowInInspector]
        public List<BattleInfo> ActiveBattleInfos => _activeBattleInfos.Values.ToList();

        private void Awake()
        {
            _mapManager.BeforeMapLoad += ClearAllBattles;
            _gameManager.AddCharacterLifecycleOptimization(this);
        }

        private void FixedUpdate()
        {
            // 先对已激活战斗信息进行更新
            _activeBattleInfos.Values.ToArray().ForEach(UpdateBattle);

            #region 对未加入战斗的角色进行战斗触发检测

            // 记录未战斗角色之间的侦察结果
            var idleCharacters = _gameManager.Characters
                .Where(character =>
                    character.Parameters.battleState != CharacterBattleState.Battle && character.BattleAbility)
                .ToArray();
            var detectedResult = new bool[idleCharacters.Length, idleCharacters.Length];
            for (var i = 0; i < idleCharacters.Length; i++)
            {
                var character = idleCharacters[i];
                for (var j = 0; j < idleCharacters.Length; j++)
                {
                    var targetCharacter = idleCharacters[j];
                    // 如果该角色侦察到目标，就记录起来
                    if (character.BattleAbility.DetectedEnemies.ToList().Find(target => target == targetCharacter) !=
                        null)
                    {
                        detectedResult[i, j] = true;
                    }
                }
            }

            // 遍历侦察矩阵，如果两个角色之间互相侦察到对方，就认为他们会触发战斗，这里的战斗类型是对峙战斗
            for (var i = 0; i < idleCharacters.Length; i++)
            {
                for (var j = i + 1; j < idleCharacters.Length; j++)
                {
                    if (detectedResult[i, j] && detectedResult[j, i])
                    {
                        TriggerBattle(idleCharacters[i], idleCharacters[j], BattleTriggerType.Standoff);
                    }
                }
            }

            #endregion
        }

        private void OnDestroy()
        {
            _mapManager.BeforeMapLoad -= ClearAllBattles;
            _gameManager.RemoveCharacterLifecycleOptimization(this);
            ClearAllBattles();
        }

        /// <summary>
        /// 记录战斗，内部先判断是否触发战斗，再记录战斗伤害
        /// </summary>
        public void RecordBattle(CharacterObject proactiveCharacter, CharacterObject reactiveCharacter,
            CharacterResource resource)
        {
            // 如果两方是同一阵营，不认为是战斗
            if (proactiveCharacter.Parameters.side == reactiveCharacter.Parameters.side)
            {
                return;
            }

            // 如果被动方不在战斗中，认为是偷袭触发战斗
            var isSneakBattle = reactiveCharacter.Parameters.battleState != CharacterBattleState.Battle;

            // 如果受到伤害的角色是中立阵营，则改变阵营到攻击者的敌对阵营
            if (reactiveCharacter.Parameters.side == CharacterSide.Neutral)
            {
                reactiveCharacter.Parameters.side = proactiveCharacter.Parameters.side == CharacterSide.Player
                    ? CharacterSide.Enemy
                    : CharacterSide.Player;
            }

            // 造成伤害的角色直接调用偷袭函数，而不是进入战斗时触发
            if (isSneakBattle)
            {
                proactiveCharacter.BattleAbility?.SneakAttack();
            }

            // 受到伤害的角色直接调用被偷袭函数，而不是进入战斗时触发
            if (isSneakBattle)
            {
                reactiveCharacter.BattleAbility?.BeSneakAttacked();
            }

            // 触发战斗
            var battleInfo = TriggerBattle(proactiveCharacter, reactiveCharacter,
                isSneakBattle ? BattleTriggerType.SneakAttack : BattleTriggerType.Standoff);

            // 记录战斗伤害
            if (battleInfo != null)
            {
                var proactiveIndex = battleInfo.totalCharacters.FindIndex(character => character == proactiveCharacter);
                var reactiveIndex = battleInfo.totalCharacters.FindIndex(character => character == reactiveCharacter);
                battleInfo.damageRecords[proactiveIndex, reactiveIndex] += resource.hp;
            }
        }

        /// <summary>
        /// 清空所有战斗
        /// </summary>
        public void ClearAllBattles()
        {
            // 停止所有战斗
            _activeBattleInfos.Keys.ToArray().ForEach(FinishBattle);
            // 销毁所有战斗场特效
            _completedBattleInfos.ForEach(pair => { GameObject.Destroy(pair.Value.filedEffect); });
            _completedBattleInfos.Clear();
            // 清空所有战斗音乐
            _audioManager.RemoveBackgroundMusic(universalBattleMusic);
            _playingBattleMusic.ForEach(pair => { _audioManager.RemoveBackgroundMusic(pair.Value.MusicId); });
            _playingBattleMusic.Clear();
        }

        /// <summary>
        /// 获取对应战斗信息
        /// </summary>
        public bool TryGetBattleInfo(string battleId, out BattleInfo battleInfo)
        {
            return _activeBattleInfos.TryGetValue(battleId, out battleInfo);
        }

        public bool IsPlayerActiveBattle(string battleId)
        {
            if (TryGetBattleInfo(battleId, out var battleInfo))
            {
                return _gameManager.Player && battleInfo.IsCharacterBeActiveInBattle(_gameManager.Player);
            }

            return false;
        }

        public bool AllowCharacterExecuteLifecycle(CharacterObject character)
        {
            // 如果角色处于玩家正在活跃的战斗中，则允许角色执行生命周期
            if (character.Parameters.battleState == CharacterBattleState.Battle &&
                IsPlayerActiveBattle(character.Parameters.battleId))
            {
                return true;
            }

            return false;
        }

        private void UpdateBattle(BattleInfo battleInfo)
        {
            // 检测战斗的活跃角色
            battleInfo.activeCharacters.ToArray().ForEach(character =>
            {
                // 如果角色离开战斗场地，认为其逃离战斗
                if (character.Parameters.battleState != CharacterBattleState.Battle ||
                    !battleInfo.InBattleField(character.Parameters.position))
                {
                    if (EscapeBattle(battleInfo.id, character))
                    {
                        return;
                    }
                }

                // 如果角色死亡，执行战斗死亡函数
                if (character.Parameters.dead)
                {
                    DeadInBattle(battleInfo.id, character);
                    return;
                }

                // 默认执行维持战斗函数
                character.BattleAbility?.StayBattle(battleInfo);
            });

            // 对当前场景的全部角色进行轮询
            _gameManager.Characters.ForEach(character =>
            {
                // 如果角色处于战斗场地内并且不处于该战斗，认为其加入战斗
                if (!battleInfo.activeCharacters.Contains(character) &&
                    character.Parameters.battleState != CharacterBattleState.Battle &&
                    battleInfo.InBattleField(character.Parameters.position)
                   )
                {
                    JoinBattle(battleInfo.id, character);
                }
            });

            // 如果正在战斗的角色全是同一阵营，则结束战斗
            var playerSideCharacterSize = battleInfo.activeCharacters
                .Count(character => character.Parameters.side == CharacterSide.Player);
            var enemySideCharacterSize = battleInfo.activeCharacters
                .Count(character => character.Parameters.side == CharacterSide.Enemy);
            if (playerSideCharacterSize == 0 || enemySideCharacterSize == 0)
            {
                FinishBattle(battleInfo.id);
            }
        }

        private BattleInfo TriggerBattle(
            CharacterObject proactiveCharacter,
            CharacterObject reactiveCharacter,
            BattleTriggerType triggerType
        )
        {
            // 任何一方没有战斗能力就不认为能够触发战斗
            if (proactiveCharacter.BattleAbility == null || reactiveCharacter.BattleAbility == null)
            {
                return null;
            }

            // 若两方都已加入战斗，则不会触发新战斗
            if (proactiveCharacter.Parameters.battleState == CharacterBattleState.Battle &&
                reactiveCharacter.Parameters.battleState == CharacterBattleState.Battle)
            {
                return null;
            }

            // 若两方都没加入战斗，则触发战斗
            if (proactiveCharacter.Parameters.battleState != CharacterBattleState.Battle &&
                reactiveCharacter.Parameters.battleState != CharacterBattleState.Battle)
            {
                // 获取战斗场地参数
                var visualCenter =
                    (proactiveCharacter.Visual.Center.position + reactiveCharacter.Visual.Center.position) / 2f;
                Quaternion visualRotation;
                if (Physics.Raycast(visualCenter, Vector3.down, out var hitInfo, 100f,
                        GlobalRuleSingletonConfigSO.Instance.groundLayer))
                {
                    var referenceVector = Mathf.Abs(hitInfo.normal.y) > 0.9f ? Vector3.forward : Vector3.up;
                    var tangent = Vector3.Cross(referenceVector, hitInfo.normal).normalized;
                    visualRotation = Quaternion.LookRotation(hitInfo.normal, tangent);
                }
                else
                {
                    visualRotation = Quaternion.LookRotation(Vector3.up);
                }

                var position =
                    new Vector2(
                        (proactiveCharacter.Parameters.position.x + reactiveCharacter.Parameters.position.x) / 2f,
                        (proactiveCharacter.Parameters.position.z + reactiveCharacter.Parameters.position.z) / 2f);
                var radius = Mathf.Max(minFieldRadius,
                    MathUtil.GetDistance(proactiveCharacter.Parameters.position,
                        reactiveCharacter.Parameters.position, MathUtil.TwoDimensionAxisType.XZ) / 2 + 3);

                // 创建新战斗
                var newBattleInfo = CreateBattle(
                    triggerType,
                    proactiveCharacter.Parameters.side,
                    proactiveCharacter,
                    visualCenter,
                    visualRotation,
                    radius * Vector3.one,
                    position,
                    radius
                );

                // 将两个角色拉入新战斗
                JoinBattle(newBattleInfo.id, proactiveCharacter, false);
                JoinBattle(newBattleInfo.id, reactiveCharacter, false);

                return newBattleInfo;
            }

            // 到这里则说明存在一方加入战斗，则获取加入的战斗Id
            var proactiveCharacterBattleId = proactiveCharacter.Parameters.battleId;
            var reactiveCharacterBattleId = reactiveCharacter.Parameters.battleId;
            var existentBattleId = string.IsNullOrEmpty(proactiveCharacterBattleId)
                ? reactiveCharacterBattleId
                : proactiveCharacterBattleId;
            // 将未加入战斗的角色拉入已存在的战斗中
            if (!string.IsNullOrEmpty(existentBattleId))
            {
                JoinBattle(existentBattleId, proactiveCharacter, false);
                JoinBattle(existentBattleId, reactiveCharacter, false);
                return _activeBattleInfos[existentBattleId];
            }

            return null;
        }

        private BattleInfo CreateBattle(
            BattleTriggerType triggerType,
            CharacterSide triggerSide,
            CharacterObject triggerCharacter,
            Vector3 battleFieldCenter,
            Quaternion battleFieldRotation,
            Vector3 battleFieldScale,
            Vector2 battleFieldPosition,
            float battleFieldRadius
        )
        {
            // 先创建场地特效和战斗信息
            var effect = GameObject.Instantiate(fieldWarningEffect, transform, true);
            effect.transform.position = battleFieldCenter;
            effect.transform.rotation = battleFieldRotation;
            effect.transform.localScale = battleFieldScale;
            var battleInfo = new BattleInfo
            {
                id = GenerateUniqueId(),
                startTime = Time.time,
                endTime = 0f,
                triggerType = triggerType,
                triggerSide = triggerSide,
                triggerCharacter = triggerCharacter,
                fieldCenter = battleFieldPosition,
                fieldRadius = battleFieldRadius,
                filedEffect = effect,
                totalCharacters = new List<CharacterObject>(),
                activeCharacters = new List<CharacterObject>(),
                level = BattleLevel.Easy,
                damageRecords = new float[0, 0]
            };
            OnBattleStarted?.Invoke(battleInfo);
            _activeBattleInfos.Add(battleInfo.id, battleInfo);
            DebugUtil.LogCyan($"创建战斗({battleInfo.id})，位置处于{battleInfo.fieldCenter}");

            // 对当前场景的全部角色进行轮询
            _gameManager.Characters.ForEach(character =>
            {
                // 如果角色处于战斗场地内并且不处于该战斗，认为其加入战斗
                if (!battleInfo.activeCharacters.Contains(character) &&
                    character.Parameters.battleState != CharacterBattleState.Battle &&
                    battleInfo.InBattleField(character.Parameters.position)
                   )
                {
                    JoinBattle(battleInfo.id, character);
                }
            });

            return battleInfo;
        }

        private void JoinBattle(string battleId, CharacterObject character, bool checkSneakAttack = true)
        {
            if (!_activeBattleInfos.TryGetValue(battleId, out var battleInfo))
            {
                DebugUtil.LogError($"角色({character.Parameters.DebugName})加入无法加入不活跃的战斗({battleId})");
                return;
            }

            // 如果该角色正在该战斗中，则直接返回
            if (battleInfo.activeCharacters.Contains(character))
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})正在该场战斗({battleId})中");
                return;
            }

            // 如果该角色战斗能力不允许加入该战斗，则直接返回
            if (character.BattleAbility?.AllowJoinBattle(battleInfo) != true)
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})不允许加入该战斗({battleId})");
                return;
            }

            // 检查该角色是否已经加入过该战斗，如果没有就在战斗信息中添加角色
            if (!battleInfo.totalCharacters.Contains(character))
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})初次加入该战斗({battleId})");
                battleInfo.totalCharacters.Add(character);
                // 新建伤害记录数组
                var damageRecords =
                    new float[battleInfo.totalCharacters.Count, battleInfo.totalCharacters.Count];
                var originI = battleInfo.damageRecords.GetLength(0) - 1;
                var originJ = battleInfo.damageRecords.GetLength(1) - 1;
                for (var i = 0; i < damageRecords.GetLength(0); i++)
                {
                    for (var j = 0; j < damageRecords.GetLength(1); j++)
                    {
                        if (i <= originI && j <= originJ)
                        {
                            damageRecords[i, j] = battleInfo.damageRecords[i, j];
                        }
                        else
                        {
                            damageRecords[i, j] = 0f;
                        }
                    }
                }

                battleInfo.damageRecords = damageRecords;

                // 这里设计为偷袭触发的战斗中偷袭方和被偷袭方角色加入战斗时自动触发对应函数
                if (checkSneakAttack && battleInfo.triggerType == BattleTriggerType.SneakAttack)
                {
                    if (battleInfo.triggerSide == character.Parameters.side)
                    {
                        character.BattleAbility?.SneakAttack();
                    }
                    else
                    {
                        character.BattleAbility?.BeSneakAttacked();
                    }
                }

                // 如果角色是敌方阵营则更新战斗等级，注意战斗等级只提升不回退
                if (character.Parameters.side == CharacterSide.Enemy)
                {
                    if (battleInfo.level < BattleLevel.Hard && character.HasTag("Boss"))
                    {
                        battleInfo.level = BattleLevel.Hard;
                        OnBattleLevelUpgraded?.Invoke(battleInfo);
                    }

                    if (battleInfo.level < BattleLevel.Normal && character.HasTag("Elite"))
                    {
                        battleInfo.level = BattleLevel.Normal;
                        OnBattleLevelUpgraded?.Invoke(battleInfo);
                    }
                }
            }
            else
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})重新加入该战斗({battleId})");
            }

            // 检查战斗是否是玩家活跃的战斗，是则继续检查该角色是否存在指定的战斗音乐，是则添加战斗音乐
            if (battleInfo.IsCharacterBeActiveInBattle(_gameManager.Player))
            {
                if (specifiedBattleMusic.TryGetValue(character.Parameters.prototype, out var battleMusic))
                {
                    var musicId =
                        _audioManager.AddBackgroundMusic(battleMusic.music, battleMusic.priority, battleMusic.volume);
                    _playingBattleMusic.Add(character.Parameters.id,
                        new BattlePlayingMusic { OwnId = character.Parameters.id, MusicId = musicId });
                }
            }

            // 检查是否为玩家加入战斗，是则添加通用战斗音乐以及当前战斗活跃角色的战斗音乐
            if (character == _gameManager.Player)
            {
                _audioManager.AddBackgroundMusic(universalBattleMusic, universalBattleMusicPriority,
                    universalBattleMusicVolume);
                battleInfo.activeCharacters.ForEach(activeCharacter =>
                {
                    if (specifiedBattleMusic.TryGetValue(activeCharacter.Parameters.prototype, out var battleMusic))
                    {
                        var musicId = _audioManager.AddBackgroundMusic(battleMusic.music, battleMusic.priority,
                            battleMusic.volume);
                        _playingBattleMusic.Add(activeCharacter.Parameters.id,
                            new BattlePlayingMusic { OwnId = activeCharacter.Parameters.id, MusicId = musicId });
                    }
                });
            }

            // 如果该角色是中立阵营，则默认改变为玩家阵营
            if (character.Parameters.side == CharacterSide.Neutral)
            {
                character.Parameters.side = CharacterSide.Player;
            }

            // 将角色设置为活跃角色
            battleInfo.activeCharacters.Add(character);
            // 最终调用角色函数
            character.BattleAbility?.JoinBattle(battleInfo);
            // 调用角色加入事件
            OnCharacterJoinBattle?.Invoke(battleInfo, character);
        }

        private bool EscapeBattle(string battleId, CharacterObject character)
        {
            if (!_activeBattleInfos.TryGetValue(battleId, out var battleInfo))
            {
                DebugUtil.LogError($"角色({character.Parameters.DebugName})无法逃离不活跃的战斗({battleId})");
                return false;
            }

            // 如果该角色不在该战斗中，或者是该角色战斗能力不允许逃离该战斗，则直接返回
            if (!battleInfo.activeCharacters.Contains(character))
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})不处于该场战斗({battleId})中");
                return false;
            }

            // 如果该角色不在该战斗中，或者是该角色战斗能力不允许逃离该战斗，则直接返回
            if (character.BattleAbility?.AllowEscapeBattle(battleInfo) != true)
            {
                DebugUtil.LogCyan($"角色({character.Parameters.DebugName})不允许逃离战斗({battleId})");
                return false;
            }

            DebugUtil.LogCyan($"角色({character.Parameters.DebugName})逃离战斗({battleId})");

            // 检查战斗是否是玩家活跃的战斗，是则继续检查是否在播放该角色对应的战斗音乐，是则删除战斗音乐
            if (battleInfo.IsCharacterBeActiveInBattle(_gameManager.Player))
            {
                if (_playingBattleMusic.TryGetValue(character.Parameters.id, out var battleMusic))
                {
                    _audioManager.RemoveBackgroundMusic(battleMusic.MusicId);
                    _playingBattleMusic.Remove(character.Parameters.id);
                }
            }

            // 检查是否为玩家逃离战斗，是则删除通用战斗音乐以及当前战斗活跃角色的战斗音乐
            if (character == _gameManager.Player)
            {
                _audioManager.RemoveBackgroundMusic(universalBattleMusic);
                battleInfo.activeCharacters.ForEach(activeCharacter =>
                {
                    if (_playingBattleMusic.TryGetValue(activeCharacter.Parameters.id, out var battleMusic))
                    {
                        _audioManager.RemoveBackgroundMusic(battleMusic.MusicId);
                        _playingBattleMusic.Remove(activeCharacter.Parameters.id);
                    }
                });
                // 这里兜底删除播放的战斗音乐
                _playingBattleMusic.ForEach(pair => { _audioManager.RemoveBackgroundMusic(pair.Value.MusicId); });
                _playingBattleMusic.Clear();
            }

            // 将角色设置为非活跃角色
            battleInfo.activeCharacters.Remove(character);
            // 最终调用角色函数
            character.BattleAbility?.EscapeBattle(battleInfo);
            // 调用角色退出事件
            OnCharacterExitBattle?.Invoke(battleInfo, character);

            return true;
        }

        private bool DeadInBattle(string battleId, CharacterObject character)
        {
            if (!_activeBattleInfos.TryGetValue(battleId, out var battleInfo))
            {
                DebugUtil.LogError($"角色({character.Parameters.DebugName})无法在不活跃的战斗({battleId})中死亡");
                return false;
            }

            DebugUtil.LogCyan($"角色({character.Parameters.DebugName})在战斗({battleId})中死亡");

            // 检查战斗是否是玩家活跃的战斗，是则继续检查是否在播放该角色对应的战斗音乐，是则删除战斗音乐
            if (battleInfo.IsCharacterBeActiveInBattle(_gameManager.Player))
            {
                if (_playingBattleMusic.TryGetValue(character.Parameters.id, out var battleMusic))
                {
                    _audioManager.RemoveBackgroundMusic(battleMusic.MusicId);
                    _playingBattleMusic.Remove(character.Parameters.id);
                }
            }

            // 检查是否为玩家在战斗中死亡，是则删除该场战斗的音乐
            if (character == _gameManager.Player)
            {
                ClearBattleMusic(battleInfo);
            }

            // 将角色设置为非活跃角色
            battleInfo.activeCharacters.Remove(character);
            // 调用角色自身函数
            character.BattleAbility?.DeadInBattle(battleInfo);
            // 调用角色退出事件
            OnCharacterExitBattle?.Invoke(battleInfo, character);
            // 判断对应战斗中该角色是否是玩家的敌人，是则调用敌人死亡事件
            if (battleInfo.activeCharacters.Find(x =>
                    x.Parameters.side != character.Parameters.side && x == _gameManager.Player) != null)
            {
                OnPlayerKillEnemyInBattle?.Invoke(character);
            }

            return true;
        }

        private void FinishBattle(string battleId)
        {
            if (!_activeBattleInfos.ContainsKey(battleId) || _completedBattleInfos.ContainsKey(battleId))
            {
                DebugUtil.LogError($"无法结束不活跃的战斗({battleId})");
                return;
            }

            DebugUtil.LogCyan($"结束战斗({battleId})");
            var battleInfo = _activeBattleInfos[battleId];
            battleInfo.endTime = Time.time;
            // 销毁场地特效
            if (battleInfo.filedEffect)
            {
                GameObject.Destroy(battleInfo.filedEffect);
            }

            // 检查是否为玩家活跃的战斗，是则删除该场战斗的音乐
            if (battleInfo.IsCharacterBeActiveInBattle(_gameManager.Player))
            {
                ClearBattleMusic(battleInfo);
            }

            // 剩余角色完成战斗
            battleInfo.activeCharacters.ToArray().ForEach(character =>
            {
                // 调用角色函数
                character.BattleAbility?.FinishBattle(battleInfo);
                // 调用角色退出事件
                OnCharacterExitBattle?.Invoke(battleInfo, character);
            });
            // 战斗结束，移除战斗数据
            battleInfo.activeCharacters.Clear();
            _completedBattleInfos.Add(battleId, battleInfo);
            _activeBattleInfos.Remove(battleId);

            OnBattleFinished?.Invoke(battleInfo);
        }

        private void ClearBattleMusic(BattleInfo battleInfo)
        {
            _audioManager.RemoveBackgroundMusic(universalBattleMusic);
            battleInfo.activeCharacters.ForEach(activeCharacter =>
            {
                if (_playingBattleMusic.Remove(activeCharacter.Parameters.id, out var battleMusic))
                {
                    _audioManager.RemoveBackgroundMusic(battleMusic.MusicId);
                }
            });
        }

        private string GenerateUniqueId() => ++_battleIdSeed + "";
    }
}