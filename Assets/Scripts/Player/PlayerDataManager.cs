using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Character;
using Character.Data;
using Character.Data.Extension;
using Common;
using Damage.Data;
using Events;
using Events.Data;
using Framework.DataStructure;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model.Data;
using Map;
using Package;
using Package.Data;
using Player.Data;
using Sirenix.OdinInspector;
using Skill;
using Skill.Runtime;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

namespace Player
{
    /// <summary>
    /// 玩家数据管理器，用于如下：
    /// 1.运行时从存档中读取数据
    /// 2.随时更新存档数据
    /// 3.存档时将最新数据存入
    /// </summary>
    public class PlayerDataManager : MonoBehaviour, IArchivable
    {
        [Title("资源图标设置")] [SerializeField] private string moneyThumbnailAtlas;
        [SerializeField] private string moneyThumbnailName;
        [SerializeField] private string experienceThumbnailAtlas;
        [SerializeField] private string experienceThumbnailName;
        [SerializeField] private string levelUpThumbnailAtlas;
        [SerializeField] private string levelUpThumbnailName;

        #region 玩家角色参数

        [ShowInInspector, ReadOnly] public string Name { private set; get; }
        [ShowInInspector, ReadOnly] public int Hp { private set; get; }
        [ShowInInspector, ReadOnly] public int Mp { private set; get; }
        [ShowInInspector, ReadOnly] public HumanoidCharacterRace Race { private set; get; }
        [ShowInInspector, ReadOnly] public HumanoidAppearanceData Appearance { private set; get; }
        [ShowInInspector, ReadOnly] public Vector3 Position { private set; get; }
        [ShowInInspector, ReadOnly] public Quaternion Rotation { private set; get; }

        private readonly List<string> _skills = new();
        [ShowInInspector, ReadOnly] public string[] Skills => _skills.ToArray();

        #endregion

        #region 玩家特有参数

        [ShowInInspector, ReadOnly] public int Level { private set; get; }
        [ShowInInspector, ReadOnly] public int Money { private set; get; }
        [ShowInInspector, ReadOnly] public int Experience { private set; get; }
        [ShowInInspector, ReadOnly] public int LevelUpExperience { private set; get; }

        [ShowInInspector, ReadOnly]
        private readonly Dictionary<string, int> _killPrototypeRecords = new(); // 击杀原型记录，键为原型字符串，值为数量

        [ShowInInspector, ReadOnly]
        private readonly Dictionary<string, int> _killMapCharacterRecords = new(); // 击杀地图角色记录，键为(地图id-地图角色id)，值为数量

        #endregion

        [Inject] private PackageManager _packageManager;
        [Inject] private GameManager _gameManager;
        [Inject] private AlgorithmManager _algorithmManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private MapManager _mapManager;
        [Inject] private SkillManager _skillManager;

        private void Awake()
        {
            GameApplication.Instance.ArchiveManager.Register(this);
            _battleManager.OnPlayerKillEnemyInBattle += HandlePlayerKillEnemy;
        }

        private void FixedUpdate()
        {
            if (!_gameManager.Player)
            {
                return;
            }

            // 同步玩家角色数据
            UpdatePlayerDataImmediately();
        }

        private void OnDestroy()
        {
            GameApplication.Instance?.ArchiveManager.Unregister(this);
            _battleManager.OnPlayerKillEnemyInBattle -= HandlePlayerKillEnemy;
        }

        private void UpdatePlayerDataImmediately()
        {
            Name = _gameManager.Player.Parameters.name;
            Hp = _gameManager.Player.Parameters.resource.hp;
            Mp = _gameManager.Player.Parameters.resource.mp;
            Race = _gameManager.Player.HumanoidParameters.race;
            Appearance = _gameManager.Player.HumanoidParameters.appearance;
            Position = _gameManager.Player.Parameters.position;
            Rotation = _gameManager.Player.Parameters.rotation;
            _skills.Clear();
            _skills.AddRange(_gameManager.Player.Parameters.skills.Where(skill => skill.group == SkillGroup.Static)
                .Select(skill => skill.id).ToList());
        }

        public bool EarnMoney(int earn, bool notifyPlayer = false)
        {
            if (earn <= 0) return false;
            Money += earn;
            if (notifyPlayer)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationGetEventParameter>(
                    GameEvents.NotificationGet,
                    new NotificationGetEventParameter
                    {
                        ThumbnailAtlas = moneyThumbnailAtlas,
                        ThumbnailName = moneyThumbnailName,
                        Name = "金币",
                        Number = earn
                    });
            }

            return true;
        }

        public bool CostMoney(int cost, bool notifyPlayer = false)
        {
            if (cost <= 0) return false;
            if (Money < cost) return false;
            Money -= cost;
            if (notifyPlayer)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationLostEventParameter>(
                    GameEvents.NotificationLost,
                    new NotificationLostEventParameter
                    {
                        ThumbnailAtlas = moneyThumbnailAtlas,
                        ThumbnailName = moneyThumbnailName,
                        Name = "金币",
                        Number = cost
                    });
            }

            return true;
        }

        public void AddExperience(int experience)
        {
            Experience += Mathf.Max(experience, 0);
            // GameApplication.Instance.EventCenter.TriggerEvent<TipGetEventParameter>(GameEvents.TipGet,
            //     new TipGetEventParameter
            //     {
            //         ThumbnailAtlas = experienceThumbnailAtlas,
            //         ThumbnailName = experienceThumbnailName,
            //         Name = "经验",
            //         Number = experience
            //     });

            if (Experience < LevelUpExperience)
            {
                return;
            }

            // 下面是升级逻辑
            while (Experience >= LevelUpExperience)
            {
                Level++;
                Experience -= LevelUpExperience;
                LevelUpExperience = _algorithmManager.PlayerLevelPropertyRuleSO.CalculateLevelUpExperience(Level);
                // 玩家升级后重新设置属性并自动补充资源
                if (_gameManager.Player)
                {
                    _gameManager.Player.PropertyAbility.BaseProperty =
                        _algorithmManager.PlayerLevelPropertyRuleSO.CalculateLevelProperty(Level);
                    _gameManager.Player.ResourceAbility.FillResource(true);
                }

                // 发送事件
                GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(
                    GameEvents.Tip,
                    new TipEventParameter
                    {
                        Tip = "角色等级提升"
                    }
                );
            }
        }

        public void LearnAbilitySkill(string skillId)
        {
            if (!_skillManager.TryGetSkillPrototype(skillId, out var skillPrototype))
            {
                return;
            }

            // 添加玩家技能
            _skills.Add(skillId);
            if (_gameManager.Player)
            {
                _gameManager.Player.SkillAbility?.AddSkill(skillId, SkillGroup.Static);
            }

            // 发送事件
            GameApplication.Instance.EventCenter.TriggerEvent<TipEventParameter>(
                GameEvents.Tip,
                new TipEventParameter
                {
                    Tip = $"角色习得能力【{skillPrototype.Name}】"
                }
            );
        }

        public bool TryGetKillMapCharacterRecord(int mapId, int characterId, out int count)
        {
            var mapCharacterKey = GetMapCharacterRecordKey(_mapManager.MapId, characterId);
            return _killMapCharacterRecords.TryGetValue(mapCharacterKey, out count);
        }

        public bool TryGetKillPrototypeRecord(string characterPrototype, out int count)
        {
            return _killPrototypeRecords.TryGetValue(characterPrototype, out count);
        }

        public void Save(ArchiveData archiveData)
        {
            UpdatePlayerDataImmediately();
            archiveData.player.name = Name;
            archiveData.player.hp = Hp;
            archiveData.player.mp = Mp;
            archiveData.player.race = Race;
            archiveData.player.appearance = Appearance.ToArchiveData();
            archiveData.player.map = new CharacterMapArchiveData
            {
                id = _mapManager.MapId,
                position = new SerializableVector3(Position),
                forwardAngle = Quaternion.Angle(Quaternion.AngleAxis(0, Vector3.up), Rotation)
            };
            archiveData.player.skills = _skills;
            archiveData.player.level = Level;
            archiveData.player.money = Money;
            archiveData.player.experience = Experience;
            archiveData.player.killPrototypeRecords = _killPrototypeRecords;
            archiveData.player.killMapCharacterRecords = _killMapCharacterRecords;
        }

        public void Load(ArchiveData archiveData)
        {
            Name = archiveData.player.name;
            Hp = archiveData.player.hp;
            Mp = archiveData.player.mp;
            Race = archiveData.player.race;
            Appearance = archiveData.player.appearance.ToAppearanceData(GameApplication.Instance.ExcelBinaryManager
                .GetContainer<HumanoidModelInfoContainer>());
            Position = archiveData.player.map.position.ToVector3();
            Rotation = Quaternion.AngleAxis(archiveData.player.map.forwardAngle, Vector3.up);
            _skills.Clear();
            _skills.AddRange(archiveData.player.skills);
            Level = archiveData.player.level;
            Money = archiveData.player.money;
            Experience = archiveData.player.experience;
            LevelUpExperience = _algorithmManager.PlayerLevelPropertyRuleSO.CalculateLevelUpExperience(Level);
            _killPrototypeRecords.Clear();
            _killPrototypeRecords.AddRange(archiveData.player.killPrototypeRecords);
            _killMapCharacterRecords.Clear();
            _killMapCharacterRecords.AddRange(archiveData.player.killMapCharacterRecords);
        }

        private void HandlePlayerKillEnemy(CharacterObject enemy)
        {
            // 接受敌人掉落物
            AddExperience(enemy.Parameters.drop.experience);
            EarnMoney(enemy.Parameters.drop.money, true);
            enemy.Parameters.drop.packages.ForEach(packageId => _packageManager.AddPackage(packageId, 1, false));

            // 记录击杀原型数量
            if (_killPrototypeRecords.TryGetValue(enemy.Parameters.prototype, out var killPrototypeRecord))
            {
                _killPrototypeRecords[enemy.Parameters.prototype] = killPrototypeRecord + 1;
            }
            else
            {
                _killPrototypeRecords.Add(enemy.Parameters.prototype, 1);
            }

            // 记录击杀地图角色数量
            var mapCharacterKey = GetMapCharacterRecordKey(_mapManager.MapId, enemy.Parameters.id);
            if (_killMapCharacterRecords.TryGetValue(mapCharacterKey, out var killMapCharacterRecord))
            {
                _killMapCharacterRecords[mapCharacterKey] = killMapCharacterRecord + 1;
            }
            else
            {
                _killMapCharacterRecords.Add(mapCharacterKey, 1);
            }
        }

        private static string GetMapCharacterRecordKey(int mapId, int characterId)
        {
            return $"{mapId}-{characterId}";
        }
    }
}