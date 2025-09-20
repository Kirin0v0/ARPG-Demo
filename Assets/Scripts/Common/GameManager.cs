using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AoE;
using AoE.Data;
using Archive.Data;
using Bullet;
using Bullet.Data;
using Character;
using Character.Data;
using Character.Data.Extension;
using Damage.Data;
using Events;
using Features.Appearance.Data;
using Framework.Common.Debug;
using Framework.Common.Resource;
using Framework.Core.Extension;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model.Data;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using Interact;
using Map.Data;
using NodeCanvas.DialogueTrees;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Player;
using Player.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill;
using Skill.Runtime;
using TimeScale;
using TimeScale.Command;
using Trade.Config;
using UnityEngine;
using UnityEngine.Events;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Common
{
    public interface IGameCharacterLifecycleOptimization
    {
        bool AllowCharacterExecuteLifecycle(CharacterObject character);
    }

    /// <summary>
    /// 游戏管理器，其职责如下：
    /// 1.管理游戏创建的角色、子弹、AOE和交互物体的创建以及销毁
    /// 2.负责角色、子弹和AOE的生命周期函数执行
    /// 3.控制角色、子弹和AOE的时间流逝，支持指令时间和子弹时间两种场景混合配置，最终时间间隔为(自定义自身时间缩放x时间管理器缩放x实际时间间隔)
    /// </summary>
    public class GameManager : SerializedMonoBehaviour
    {
        [Title("父对象配置")] [SerializeField] private Transform defaultParent;
        [SerializeField] private Transform characterParent;
        [SerializeField] private Transform aoeParent;
        [SerializeField] private Transform bulletParent;

        [Title("性能优化配置")] [SerializeField] private bool characterLifecycleOptimize = true;
        [SerializeField] private float characterLifecycleExecuteRadius = 30f;
        private readonly List<IGameCharacterLifecycleOptimization> _characterLifecycleOptimizeChain = new();

        public void AddCharacterLifecycleOptimization(IGameCharacterLifecycleOptimization optimization)
        {
            _characterLifecycleOptimizeChain.Add(optimization);
        }

        public void RemoveCharacterLifecycleOptimization(IGameCharacterLifecycleOptimization optimization)
        {
            _characterLifecycleOptimizeChain.Remove(optimization);
        }

        [Title("调试配置")] [SerializeField] private bool debugRenderTick = false;
        [SerializeField] private bool debugLogicTick = false;
        [SerializeField] private bool debugPostTick = false;

        [Inject] private IObjectResolver _objectResolver;
        [Inject] private AlgorithmManager _algorithmManager;
        [Inject] private HumanoidWeaponManager _weaponManager;

        public PlayerCharacterObject Player { get; private set; }
        public event Action<PlayerCharacterObject> OnPlayerCreated;
        public event Action<PlayerCharacterObject> OnPlayerDestroyed;

        private CharacterObject _god;

        /// <summary>
        /// 上帝角色，不加入角色列表中，也不执行角色生命周期函数
        /// </summary>
        public CharacterObject God
        {
            get
            {
                if (!_god)
                {
                    var godGameObject = new GameObject("God")
                    {
                        transform = { parent = characterParent }
                    };
                    var character = godGameObject.AddComponent<CharacterObject>();
                    _objectResolver.Inject(character);
                    character.Init();
                    character.SetCharacterParameters(
                        -999,
                        "God",
                        "上帝",
                        Vector3.zero,
                        0,
                        new CharacterProperty
                        {
                            maxHp = 100,
                            maxMp = 100,
                            stunMeter = 100,
                            breakMeter = 100,
                        },
                        0f,
                        0f,
                        0f,
                        DamageValueType.None,
                        DamageValueType.None,
                        CharacterSide.Neutral,
                        CharacterDrop.Empty
                    );
                    _god = character;
                }

                return _god;
            }
        }

        private readonly Dictionary<int, CharacterObject> _characters = new();
        public CharacterObject[] Characters => _characters.Values.ToArray();
        private readonly Dictionary<CharacterObject, string> _characterLoadPaths = new();

        private readonly Dictionary<int, InteractableRestSpotObject> _restSpots = new();
        public InteractableRestSpotObject[] RestSpots => _restSpots.Values.ToArray();

        private readonly Dictionary<int, InteractablePackageObject> _packages = new();
        public InteractablePackageObject[] Packages => _packages.Values.ToArray();

        public InteractableObject[] InteractableObjects
        {
            get
            {
                var interactableObjects = new List<InteractableObject>();
                interactableObjects.AddRange(_packages.Values);
                interactableObjects.AddRange(_restSpots.Values);
                return interactableObjects.ToArray();
            }
        }

        private readonly Dictionary<InteractableObject, string> _interactableLoadPaths = new();

        private readonly List<AoEObject> _aoes = new();
        private AoEObject[] AoEs => _aoes.ToArray();

        private readonly List<BulletObject> _bullets = new();
        private BulletObject[] Bullets => _bullets.ToArray();

        private readonly TimeScaleManager _timeScaleManager = new(); // 时间缩放管理器，仅影响游戏中的角色、AoE、子弹和技能，不是广义的游戏时间缩放
        private readonly Dictionary<int, float> _customCharacterTimeScales = new(); // 自定义角色时间缩放字典，仅测试使用

        private readonly List<(Type type, string key)> _addressablesLoads = new(); // Addressables资源加载记录

        private void Awake()
        {
            DebugUtil.LogGrey("游戏管理器初始化");
            // 注册角色时间缩放改变监听，这里之所以要通过回调设置时间缩放，是因为角色的部分能力也需要设置时间缩放，但帧函数中设置一是没有时效性，二是过于复杂
            _timeScaleManager.OnTimeScaleChanged += HandleCharacterTimeScaleChanged;
        }

        private void Update()
        {
            var totalStartTime = Time.realtimeSinceStartup;
            // 对所有角色执行渲染更新
            Characters.ForEach(character =>
            {
                // 性能优化角色执行生命周期的范围，节省性能
                if (AllowCharacterExecuteLifecycle(character))
                {
                    var startTime = Time.realtimeSinceStartup;
                    character.RenderUpdate(Time.deltaTime * GetCharacterFinalTimeScale(character.Parameters.id));
                    var endTime = Time.realtimeSinceStartup;
                    if (debugRenderTick)
                    {
                        DebugUtil.LogGrey(
                            $"角色({character.Parameters.DebugName})执行渲染更新时间: {(endTime - startTime) * 1000}ms");
                    }
                }
            });
            var totalEndTime = Time.realtimeSinceStartup;
            if (debugRenderTick)
            {
                DebugUtil.LogGrey($"游戏管理器执行渲染更新时间: {(totalEndTime - totalStartTime) * 1000}ms");
            }
        }

        private void FixedUpdate()
        {
            var totalStartTime = Time.realtimeSinceStartup;
            // 更新时间缩放管理器
            _timeScaleManager.Update(Time.fixedDeltaTime);

            // 对所有角色执行逻辑更新和销毁检查
            Characters.ForEach(character =>
            {
                // 性能优化角色执行生命周期的范围，节省性能
                if (AllowCharacterExecuteLifecycle(character))
                {
                    var startTime = Time.realtimeSinceStartup;
                    character.LogicUpdate(Time.fixedDeltaTime * GetCharacterFinalTimeScale(character.Parameters.id));
                    var endTime = Time.realtimeSinceStartup;
                    if (debugLogicTick)
                    {
                        DebugUtil.LogGrey(
                            $"角色({character.Parameters.DebugName})执行逻辑更新时间: {(endTime - startTime) * 1000}ms");
                    }
                }

                // 如果角色死亡删除角色时间缩放，否则添加角色时间缩放
                if (character.Parameters.dead)
                {
                    _timeScaleManager.RemoveCharacter(character);
                }
                else if (!_timeScaleManager.ContainsCharacter(character))
                {
                    _timeScaleManager.AddCharacter(character);
                }

                // 如果角色死亡且已允许销毁，就销毁角色
                if (character.Parameters.dead && character.StateAbility.ShouldDestroy())
                {
                    DestroyCharacter(character);
                }
            });

            // 对所有AoE执行逻辑更新和实际销毁检查
            AoEs.ForEach(aoe =>
            {
                var startTime = Time.realtimeSinceStartup;
                aoe.Tick(Time.fixedDeltaTime * GetCharacterFinalTimeScale(aoe.caster.Parameters.id));
                var endTime = Time.realtimeSinceStartup;
                if (debugLogicTick)
                {
                    DebugUtil.LogGrey($"AoE({aoe.info.id})执行逻辑更新时间: {(endTime - startTime) * 1000}ms");
                }

                // 如果AoE持续时间已经超过实际销毁时间，就实际销毁AoE
                if (aoe.timeElapsed >= aoe.duration + aoe.destroyDelay)
                {
                    _aoes.Remove(aoe);
                    GameObject.Destroy(aoe.gameObject);
                }
            });

            // 对所有子弹执行逻辑更新和实际销毁检查
            Bullets.ForEach(bullet =>
            {
                var startTime = Time.realtimeSinceStartup;
                bullet.Tick(Time.fixedDeltaTime * GetCharacterFinalTimeScale(bullet.caster.Parameters.id));
                var endTime = Time.realtimeSinceStartup;
                if (debugLogicTick)
                {
                    DebugUtil.LogGrey($"子弹({bullet.info.id})执行逻辑更新时间: {(endTime - startTime) * 1000}ms");
                }

                // 如果子弹持续时间已经超过实际销毁时间，就实际销毁子弹
                if (bullet.timeElapsed >= bullet.duration + bullet.destroyDelay)
                {
                    _bullets.Remove(bullet);
                    GameObject.Destroy(bullet.gameObject);
                }
            });

            var totalEndTime = Time.realtimeSinceStartup;
            if (debugLogicTick)
            {
                DebugUtil.LogGrey($"游戏管理器执行逻辑更新时间: {(totalEndTime - totalStartTime) * 1000}ms");
            }
        }

        private void LateUpdate()
        {
            var totalStartTime = Time.realtimeSinceStartup;
            // 对所有角色执行后更新
            Characters.ForEach(character =>
            {
                // 性能优化角色执行生命周期的范围，节省性能
                if (AllowCharacterExecuteLifecycle(character))
                {
                    var startTime = Time.realtimeSinceStartup;
                    var timeScale = GetCharacterFinalTimeScale(character.Parameters.id);
                    character.PostUpdate(Time.deltaTime * timeScale, Time.fixedDeltaTime * timeScale);
                    var endTime = Time.realtimeSinceStartup;
                    if (debugPostTick)
                    {
                        DebugUtil.LogGrey(
                            $"角色({character.Parameters.DebugName})执行后更新时间: {(endTime - startTime) * 1000}ms");
                    }
                }
            });
            var totalEndTime = Time.realtimeSinceStartup;
            if (debugPostTick)
            {
                DebugUtil.LogGrey($"游戏管理器执行后更新时间: {(totalEndTime - totalStartTime) * 1000}ms");
            }
        }

        private void OnDestroy()
        {
            DebugUtil.LogGrey("游戏管理器销毁");
            _timeScaleManager.OnTimeScaleChanged -= HandleCharacterTimeScaleChanged;
            ClearScene();
            _addressablesLoads.ForEach(tuple =>
            {
                GameApplication.Instance?.AddressablesManager.ReleaseAsset(tuple.key, tuple.type);
            });
        }

        private bool AllowCharacterExecuteLifecycle(CharacterObject character)
        {
            // 未开启性能优化则全部允许执行
            if (!characterLifecycleOptimize)
            {
                return true;
            }

            // 玩家角色一定能够执行
            if (character == Player)
            {
                return true;
            }

            // 可见角色能够执行
            if (character.Parameters.visible)
            {
                return true;
            }

            // 角色处于玩家为中心的一定半径内能够执行
            if (Player && Vector3.Distance(character.Parameters.position, Player.Parameters.position) <=
                characterLifecycleExecuteRadius)
            {
                return true;
            }

            // 最后交给优化链判断，要求全部返回true才能执行
            return _characterLifecycleOptimizeChain.All(optimization =>
                optimization.AllowCharacterExecuteLifecycle(character));
        }

        public Task<CharacterObject> CreateCharacter(
            string prefabPath,
            Vector3 position,
            float forwardAngle,
            int id,
            string prototype,
            string name,
            CharacterProperty baseProperty,
            float normalDamageMultiplier,
            float defenceDamageMultiplier,
            float brokenDamageMultiplier,
            DamageValueType weakness,
            DamageValueType immunity,
            CharacterSide side,
            CharacterDrop drop,
            object[] appearanceParameters = null,
            DialogueTree dialogueTree = null,
            TradeConfig tradeConfig = null,
            float reduceStunTime = 5f,
            bool destroyAfterDead = true,
            float destroyDelay = 2f,
            string[] tags = null,
            string[] abilitySkills = null,
            UnityAction<CharacterObject> callback = null
        )
        {
            var source = new TaskCompletionSource<CharacterObject>();
            // 如果已经存在角色，就直接返回角色
            if (_characters.TryGetValue(id, out var character))
            {
                character.Init();
                character.SetCharacterParameters(
                    id: id,
                    prototype: prototype,
                    name: name,
                    spawnPoint: position,
                    spawnAngle: forwardAngle,
                    baseProperty: baseProperty,
                    normalDamageMultiplier: normalDamageMultiplier,
                    defenceDamageMultiplier: defenceDamageMultiplier,
                    brokenDamageMultiplier: brokenDamageMultiplier,
                    weakness: weakness,
                    immunity: immunity,
                    side: side,
                    drop: drop,
                    appearanceParameters: appearanceParameters,
                    dialogueTree: dialogueTree,
                    tradeConfig: tradeConfig,
                    reduceStunTime: reduceStunTime,
                    destroyAfterDead: destroyAfterDead,
                    destroyDelay: destroyDelay,
                    tags: tags,
                    abilitySkills: abilitySkills
                );
                callback?.Invoke(character);
                source.SetResult(character);
            }
            else
            {
                CreateGameObjectFromPrefab(
                    prefabPath,
                    position,
                    Quaternion.AngleAxis(forwardAngle, Vector3.up),
                    instance =>
                    {
                        instance.transform.parent = characterParent;
                        var character = instance.GetComponent<CharacterObject>();
                        if (!character)
                        {
                            throw new Exception(
                                $"The prefab whose path is {prefabPath} must bind the CharacterObject component");
                        }

                        character.Init();
                        character.SetCharacterParameters(
                            id: id,
                            prototype: prototype,
                            name: name,
                            spawnPoint: position,
                            spawnAngle: forwardAngle,
                            baseProperty: baseProperty,
                            normalDamageMultiplier: normalDamageMultiplier,
                            defenceDamageMultiplier: defenceDamageMultiplier,
                            brokenDamageMultiplier: brokenDamageMultiplier,
                            weakness: weakness,
                            immunity: immunity,
                            side: side,
                            drop: drop,
                            appearanceParameters: appearanceParameters,
                            dialogueTree: dialogueTree,
                            tradeConfig: tradeConfig,
                            reduceStunTime: reduceStunTime,
                            destroyAfterDead: destroyAfterDead,
                            destroyDelay: destroyDelay,
                            tags: tags,
                            abilitySkills: abilitySkills
                        );
                        _characters.Add(character.Parameters.id, character);
                        _customCharacterTimeScales.Add(character.Parameters.id, 1f);
                        _characterLoadPaths.Add(character, prefabPath);
                        _timeScaleManager.AddCharacter(character);
                        callback?.Invoke(character);
                        source.SetResult(character);
                    });
            }

            return source.Task;
        }

        public Task<HumanoidCharacterObject> CreateHumanoidCharacter(
            string prefabPath,
            Vector3 position,
            float forwardAngle,
            int id,
            string prototype,
            string name,
            CharacterProperty baseProperty,
            float normalDamageMultiplier,
            float defenceDamageMultiplier,
            float brokenDamageMultiplier,
            DamageValueType weakness,
            DamageValueType immunity,
            CharacterSide side,
            HumanoidCharacterRace race,
            object[] appearanceParameters,
            List<PackageGroup> weapons,
            List<PackageGroup> gears,
            CharacterDrop drop,
            DialogueTree dialogueTree = null,
            TradeConfig tradeConfig = null,
            float reduceStunTime = 5f,
            bool destroyAfterDead = true,
            float destroyDelay = 3f,
            string[] tags = null,
            string[] abilitySkills = null,
            UnityAction<HumanoidCharacterObject> callback = null
        )
        {
            var source = new TaskCompletionSource<HumanoidCharacterObject>();

            CreateCharacter(
                prefabPath: prefabPath,
                position: position,
                forwardAngle: forwardAngle,
                id: id,
                prototype: prototype,
                name: name,
                baseProperty: baseProperty,
                normalDamageMultiplier: normalDamageMultiplier,
                defenceDamageMultiplier: defenceDamageMultiplier,
                brokenDamageMultiplier: brokenDamageMultiplier,
                weakness: weakness,
                immunity: immunity,
                side: side,
                drop: drop,
                appearanceParameters: appearanceParameters,
                dialogueTree: dialogueTree,
                tradeConfig: tradeConfig,
                reduceStunTime: reduceStunTime,
                destroyAfterDead: destroyAfterDead,
                destroyDelay: destroyDelay,
                tags: tags,
                abilitySkills: abilitySkills,
                callback: character =>
                {
                    if (character is not HumanoidCharacterObject humanoidCharacter)
                    {
                        throw new Exception($"The prefab whose path is {prefabPath} is not HumanoidCharacterObject");
                    }

                    humanoidCharacter.SetHumanoidCharacterParameters(race, weapons, gears);
                    callback?.Invoke(humanoidCharacter);
                    source.SetResult(humanoidCharacter);
                }
            );

            return source.Task;
        }

        public Task<PlayerCharacterObject> CreatePlayerCharacter(
            string prefabPath,
            Vector3 position,
            float forwardAngle,
            string name,
            int level,
            int hp,
            int mp,
            HumanoidCharacterRace race,
            HumanoidAppearanceData appearance,
            List<PackageGroup> weapons,
            List<PackageGroup> gears,
            string[] tags = null,
            string[] abilitySkills = null,
            UnityAction<PlayerCharacterObject> callback = null
        )
        {
            var source = new TaskCompletionSource<PlayerCharacterObject>();
            CreateHumanoidCharacter(
                prefabPath: prefabPath,
                position: position,
                forwardAngle: forwardAngle,
                id: -1,
                prototype: "Player",
                name: name,
                baseProperty: _algorithmManager.PlayerLevelPropertyRuleSO.CalculateLevelProperty(level),
                normalDamageMultiplier: 1f,
                defenceDamageMultiplier: 1f,
                brokenDamageMultiplier: 1f,
                weakness: DamageValueType.None,
                immunity: DamageValueType.None,
                side: CharacterSide.Player,
                race: race,
                appearanceParameters: new object[] { appearance },
                weapons: weapons,
                gears: gears,
                drop: CharacterDrop.Empty,
                dialogueTree: null,
                tradeConfig: null,
                reduceStunTime: 5f,
                destroyAfterDead: false,
                destroyDelay: 0f,
                tags: tags,
                abilitySkills: abilitySkills,
                callback: character =>
                {
                    if (character is not PlayerCharacterObject playerCharacter)
                    {
                        throw new Exception($"The prefab whose path is {prefabPath} is not PlayerCharacterObject");
                    }

                    playerCharacter.SetPlayerCharacterParameters(hp, mp);

                    if (Player != null)
                    {
                        DestroyCharacter(Player);
                    }

                    Player = playerCharacter;
                    OnPlayerCreated?.Invoke(Player);
                    callback?.Invoke(playerCharacter);
                    source.SetResult(playerCharacter);
                }
            );

            return source.Task;
        }

        public Task<CharacterObject> CreateCharacterByConfigurations(
            CharacterInfoData characterInfoData,
            MapCharacterInfoData mapCharacterInfoData,
            HumanoidAppearanceWeaponInfoContainer weaponInfoContainer,
            HumanoidAppearanceGearInfoContainer gearInfoContainer,
            PackageInfoContainer packageInfoContainer,
            UnityAction<CharacterObject> callback = null
        )
        {
            var source = new TaskCompletionSource<CharacterObject>();
            var name = !string.IsNullOrEmpty(mapCharacterInfoData.Name)
                ? mapCharacterInfoData.Name
                : characterInfoData.Name;
            var baseProperty = new CharacterProperty
            {
                maxHp = mapCharacterInfoData.MaxHp,
                maxMp = mapCharacterInfoData.MaxMp,
                stunMeter = mapCharacterInfoData.StunMeter,
                stunReduceSpeed = mapCharacterInfoData.StunReduceSpeed,
                breakMeter = mapCharacterInfoData.BreakMeter,
                breakReduceSpeed = mapCharacterInfoData.BreakReduceSpeed,
                atbLimit = mapCharacterInfoData.AtbLimit,
                stamina = mapCharacterInfoData.Stamina,
                strength = mapCharacterInfoData.Strength,
                magic = mapCharacterInfoData.Magic,
                reaction = mapCharacterInfoData.Reaction,
                luck = mapCharacterInfoData.Luck,
            };
            var weakness = GetDamageValueType(characterInfoData.Weakness);
            var immunity = GetDamageValueType(characterInfoData.Immunity);
            var side = characterInfoData.Side switch
            {
                "player" => CharacterSide.Player,
                "neutral" => CharacterSide.Neutral,
                "enemy" => CharacterSide.Enemy,
                _ => CharacterSide.Player,
            };
            var drop = new CharacterDrop
            {
                experience = mapCharacterInfoData.Experience,
                money = mapCharacterInfoData.Money,
                packages = string.IsNullOrEmpty(mapCharacterInfoData.Packages)
                    ? new List<int>()
                    : mapCharacterInfoData.Packages.Split(",").Select(int.Parse).ToList(),
            };
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(characterInfoData.Tag))
            {
                tags.AddRange(characterInfoData.Tag.Split(","));
            }

            if (!string.IsNullOrEmpty(mapCharacterInfoData.Type))
            {
                tags.Add(mapCharacterInfoData.Type);
            }

            CreateCharacterInternal(
                GetDialogueTree(characterInfoData.DialogueTreePath),
                GetTradeConfig(characterInfoData.TradeConfigPath)
            );

            return source.Task;

            DamageValueType GetDamageValueType(string str)
            {
                var damageValueType = DamageValueType.None;
                str.Split(",").ForEach(type =>
                {
                    switch (type)
                    {
                        case "physics":
                            damageValueType |= DamageValueType.Physics;
                            break;
                        case "fire":
                            damageValueType |= DamageValueType.Fire;
                            break;
                        case "ice":
                            damageValueType |= DamageValueType.Ice;
                            break;
                        case "wind":
                            damageValueType |= DamageValueType.Wind;
                            break;
                        case "lightning":
                            damageValueType |= DamageValueType.Lightning;
                            break;
                    }
                });
                return damageValueType;
            }

            Task<DialogueTree> GetDialogueTree(string dialogueTreePath)
            {
                var localSource = new TaskCompletionSource<DialogueTree>();
                if (string.IsNullOrEmpty(dialogueTreePath))
                {
                    localSource.SetResult(null);
                    return localSource.Task;
                }

                CreatePrefab<DialogueTree>(dialogueTreePath, dialogueTree => { localSource.SetResult(dialogueTree); });
                return localSource.Task;
            }

            Task<TradeConfig> GetTradeConfig(string tradeConfigPath)
            {
                var localSource = new TaskCompletionSource<TradeConfig>();
                if (string.IsNullOrEmpty(tradeConfigPath))
                {
                    localSource.SetResult(null);
                    return localSource.Task;
                }

                CreatePrefab<TradeConfig>(tradeConfigPath, tradeConfig => { localSource.SetResult(tradeConfig); });
                return localSource.Task;
            }

            async void CreateCharacterInternal(Task<DialogueTree> dialogueTreeTask, Task<TradeConfig> tradeConfigTask)
            {
                await Task.WhenAll(dialogueTreeTask, tradeConfigTask);

                // 判断是否为人形角色，是则创建人形角色，否则仅创建为普通角色
                if (characterInfoData.IsHumanoid)
                {
                    var weaponPackageIds = !string.IsNullOrEmpty(characterInfoData.HumanoidWeaponIds)
                        ? characterInfoData.HumanoidWeaponIds.Split(",").Select(id => int.Parse(id))
                            .ToList()
                        : new List<int>();
                    var gearPackageIds = !string.IsNullOrEmpty(characterInfoData.HumanoidGearIds)
                        ? characterInfoData.HumanoidGearIds.Split(",").Select(id => int.Parse(id))
                            .ToList()
                        : new List<int>();
                    var weapons = new List<PackageGroup>();
                    weaponPackageIds.ForEach(packageId =>
                    {
                        var packageInfoData = packageInfoContainer.Data[packageId];
                        var weaponPackageData = packageInfoData.ToPackageData(
                            HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                            _weaponManager.GetWeaponAttackConfiguration,
                            _weaponManager.GetWeaponDefenceConfiguration,
                            weaponInfoContainer,
                            gearInfoContainer
                        );
                        var packageGroup = PackageGroup.CreateNew(weaponPackageData, 1);
                        weapons.Add(packageGroup);
                    });
                    var gears = new List<PackageGroup>();
                    gearPackageIds.ForEach(packageId =>
                    {
                        var packageInfoData = packageInfoContainer.Data[packageId];
                        var gearPackageData = packageInfoData.ToPackageData(
                            HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                            _weaponManager.GetWeaponAttackConfiguration,
                            _weaponManager.GetWeaponDefenceConfiguration,
                            weaponInfoContainer,
                            gearInfoContainer
                        );
                        var packageGroup = PackageGroup.CreateNew(gearPackageData, 1);
                        gears.Add(packageGroup);
                    });

                    CreateHumanoidCharacter(
                        prefabPath: characterInfoData.LoadPath,
                        position: new Vector3(mapCharacterInfoData.PositionX, mapCharacterInfoData.PositionY,
                            mapCharacterInfoData.PositionZ),
                        forwardAngle: mapCharacterInfoData.ForwardAngle,
                        id: mapCharacterInfoData.Id,
                        prototype: characterInfoData.Id,
                        name: name,
                        baseProperty: baseProperty,
                        normalDamageMultiplier: mapCharacterInfoData.NormalDamageMultiplier,
                        defenceDamageMultiplier: mapCharacterInfoData.DefenceDamageMultiplier,
                        brokenDamageMultiplier: mapCharacterInfoData.BrokenDamageMultiplier,
                        weakness: weakness,
                        immunity: immunity,
                        side: side,
                        race: characterInfoData.HumanoidGender switch
                        {
                            "male" => HumanoidCharacterRace.HumanMale,
                            "female" => HumanoidCharacterRace.HumanFemale,
                            _ => HumanoidCharacterRace.HumanMale,
                        },
                        appearanceParameters: characterInfoData.AppearanceParameters.Split(","),
                        weapons: weapons,
                        gears: gears,
                        drop: drop,
                        dialogueTree: dialogueTreeTask.Result,
                        tradeConfig: tradeConfigTask.Result,
                        destroyAfterDead: characterInfoData.DestroyAfterDead,
                        destroyDelay: characterInfoData.DestroyDelay,
                        tags: tags.ToArray(),
                        abilitySkills: characterInfoData.AbilitySkillIds.Split(","),
                        callback: humanoidCharacter =>
                        {
                            callback?.Invoke(humanoidCharacter);
                            source.SetResult(humanoidCharacter);
                        }
                    );
                }
                else
                {
                    CreateCharacter(
                        prefabPath: characterInfoData.LoadPath,
                        position: new Vector3(mapCharacterInfoData.PositionX, mapCharacterInfoData.PositionY,
                            mapCharacterInfoData.PositionZ),
                        forwardAngle: mapCharacterInfoData.ForwardAngle,
                        id: mapCharacterInfoData.Id,
                        prototype: characterInfoData.Id,
                        name: name,
                        baseProperty: baseProperty,
                        normalDamageMultiplier: mapCharacterInfoData.NormalDamageMultiplier,
                        defenceDamageMultiplier: mapCharacterInfoData.DefenceDamageMultiplier,
                        brokenDamageMultiplier: mapCharacterInfoData.BrokenDamageMultiplier,
                        weakness: weakness,
                        immunity: immunity,
                        side: side,
                        drop: drop,
                        appearanceParameters: characterInfoData.AppearanceParameters.Split(","),
                        dialogueTree: dialogueTreeTask.Result,
                        tradeConfig: tradeConfigTask.Result,
                        destroyAfterDead: characterInfoData.DestroyAfterDead,
                        destroyDelay: characterInfoData.DestroyDelay,
                        tags: tags.ToArray(),
                        abilitySkills: characterInfoData.AbilitySkillIds.Split(","),
                        callback: character =>
                        {
                            callback?.Invoke(character);
                            source.SetResult(character);
                        }
                    );
                }
            }
        }

        public bool IsCharacterDeadOrNonexistent(int id)
        {
            if (_characters.TryGetValue(id, out var character))
            {
                return character.Parameters.dead;
            }

            return true;
        }

        public void DestroyCharacter(CharacterObject character)
        {
            if (Player != null && Player == character)
            {
                OnPlayerDestroyed?.Invoke(Player);
                Player = null;
            }

            _characters.Remove(character.Parameters.id);
            _customCharacterTimeScales.Remove(character.Parameters.id);
            _timeScaleManager.RemoveCharacter(character);
            character.Destroy();
            if (!character.IsGameObjectDestroyed())
            {
                GameObject.Destroy(character.gameObject);
            }

            if (_characterLoadPaths.Remove(character, out var path))
            {
                // GameApplication.Instance.ResourcesManager.UnloadAsset<GameObject>(path, null);
            }

            #region 销毁该角色发射的AoE和子弹

            // 直接删除AoE，注意，这里不走逻辑删除，防止逻辑中使用角色相关功能导致出现bug
            var aoeIndex = 0;
            while (aoeIndex < _aoes.Count)
            {
                var aoe = _aoes[aoeIndex];
                if (aoe.caster == character)
                {
                    _aoes.RemoveAt(aoeIndex);
                    GameObject.Destroy(aoe.gameObject);
                }
                else
                {
                    aoeIndex++;
                }
            }

            // 直接删除子弹，注意，这里不走逻辑删除，防止逻辑中使用角色相关功能导致出现bug
            var bulletIndex = 0;
            while (bulletIndex < _bullets.Count)
            {
                var bullet = _bullets[bulletIndex];
                if (bullet.caster == character)
                {
                    _bullets.RemoveAt(bulletIndex);
                    GameObject.Destroy(bullet.gameObject);
                }
                else
                {
                    bulletIndex++;
                }
            }

            #endregion
        }

        public Task<InteractablePackageObject> CreatePackage(
            string prefabPath,
            Vector3 position,
            Quaternion rotation,
            int id,
            int packageId,
            int number,
            UnityAction<InteractablePackageObject> callback = null)
        {
            var source = new TaskCompletionSource<InteractablePackageObject>();

            if (_packages.TryGetValue(id, out var package))
            {
                package.Init(id, packageId, number);
                callback?.Invoke(package);
                source.SetResult(package);
            }
            else
            {
                CreateGameObjectFromPrefab(prefabPath, position, rotation, instance =>
                {
                    instance.transform.parent = defaultParent.transform;
                    var package = instance.GetComponent<InteractablePackageObject>();
                    if (!package)
                    {
                        throw new Exception(
                            $"The prefab whose path is {prefabPath} must bind the InteractablePackageObject component");
                    }

                    package.Init(id, packageId, number);
                    _packages.Add(id, package);
                    _interactableLoadPaths.Add(package, prefabPath);
                    callback?.Invoke(package);
                    source.SetResult(package);
                });
            }

            return source.Task;
        }

        public bool IsPackageNonexistent(int id)
        {
            return !_packages.ContainsKey(id);
        }

        public void DestroyPackage(InteractablePackageObject package)
        {
            _packages.Remove(package.Id);
            if (!package.IsGameObjectDestroyed())
            {
                GameObject.Destroy(package.gameObject);
            }

            if (_interactableLoadPaths.Remove(package, out var path))
            {
                // GameApplication.Instance.ResourcesManager.UnloadAsset<GameObject>(path, null);
            }
        }

        public Task<InteractableRestSpotObject> CreateRestSpot(
            string prefabPath,
            Vector3 position,
            Quaternion rotation,
            int id,
            UnityAction<InteractableRestSpotObject> callback = null)
        {
            var source = new TaskCompletionSource<InteractableRestSpotObject>();

            if (_restSpots.TryGetValue(id, out var restSpot))
            {
                restSpot.Init(id);
                callback?.Invoke(restSpot);
                source.SetResult(restSpot);
            }
            else
            {
                CreateGameObjectFromPrefab(prefabPath, position, rotation, instance =>
                {
                    instance.transform.parent = defaultParent.transform;
                    var restSpot = instance.GetComponent<InteractableRestSpotObject>();
                    if (!restSpot)
                    {
                        throw new Exception(
                            $"The prefab whose path is {prefabPath} must bind the InteractableRestSpotObject component");
                    }

                    restSpot.Init(id);
                    _restSpots.Add(id, restSpot);
                    _interactableLoadPaths.Add(restSpot, prefabPath);
                    callback?.Invoke(restSpot);
                    source.SetResult(restSpot);
                });
            }

            return source.Task;
        }

        public void DestroyRestSpot(InteractableRestSpotObject restSpot)
        {
            _restSpots.Remove(restSpot.Id);
            if (!restSpot.IsGameObjectDestroyed())
            {
                GameObject.Destroy(restSpot.gameObject);
            }

            if (_interactableLoadPaths.Remove(restSpot, out var path))
            {
                // GameApplication.Instance.ResourcesManager.UnloadAsset<GameObject>(path, null);
            }
        }

        public AoEObject CreateAoE(AoELauncher launcher, AoEInfo info)
        {
            var aoeObject = launcher.Launch(_objectResolver, info, aoeParent);
            _aoes.Add(aoeObject);
            aoeObject.Init();
            aoeObject.SetTimeScale(GetCharacterFinalTimeScale(aoeObject.caster.Parameters.id));
            return aoeObject;
        }

        public AoEObject GetAoE(string aoeId)
        {
            return _aoes.Find(aoe => aoe.info.id == aoeId);
        }

        public void DestroyAoE(string aoeId)
        {
            // 执行逻辑销毁，注意，实际销毁逻辑不是在这里执行
            _aoes.ForEach(aoe =>
            {
                if (aoe.info.id == aoeId)
                {
                    aoe.DestroyOnLogic();
                }
            });
        }

        public void DestroyAoE(AoEObject aoe)
        {
            // 执行逻辑销毁，注意，实际销毁逻辑不是在这里执行
            aoe.DestroyOnLogic();
        }

        public BulletObject CreateBullet(BulletLauncher launcher, BulletInfo info)
        {
            var bulletObject = launcher.Launch(_objectResolver, info, bulletParent);
            _bullets.Add(bulletObject);
            bulletObject.Init();
            bulletObject.SetTimeScale(GetCharacterFinalTimeScale(bulletObject.caster.Parameters.id));
            return bulletObject;
        }

        public BulletObject GetBullet(string bulletId)
        {
            return _bullets.Find(bullet => bullet.info.id == bulletId);
        }

        public void DestroyBullet(string bulletId)
        {
            // 执行逻辑销毁，注意，实际销毁逻辑不是在这里执行
            _bullets.ForEach(bullet =>
            {
                if (bullet.info.id == bulletId)
                {
                    bullet.DestroyOnLogic();
                }
            });
        }

        public void DestroyBullet(BulletObject bullet)
        {
            // 执行逻辑销毁，注意，实际销毁逻辑不是在这里执行
            bullet.DestroyOnLogic();
        }

        public void AddTimeScaleSingleCommand(string id, float targetTimeScale, CharacterObject target)
        {
            _timeScaleManager.AddCommand(new TimeScaleSingleCommand(id, float.MaxValue, targetTimeScale, target));
        }

        public void AddTimeScaleGlobalCommand(string id, float targetTimeScale)
        {
            _timeScaleManager.AddCommand(new TimeScaleGlobalCommand(id, float.MaxValue, targetTimeScale));
        }

        public void AddTimeScaleComboCommand(string id, float duration, float targetTimeScale)
        {
            _timeScaleManager.AddCommand(new TimeScaleComboCommand(id, duration, targetTimeScale));
        }

        public void AddTimeScaleWitchCommand(
            string id,
            float duration,
            float slowDownTimeScale,
            float hurryUpTimeScale,
            CharacterObject witch
        )
        {
            _timeScaleManager.AddCommand(new TimeScaleWitchCommand(id, duration, slowDownTimeScale, hurryUpTimeScale,
                witch));
        }

        public void AddTimeScaleSkillCommand(
            string id,
            float casterTimeScale,
            float othersTimeScale,
            CharacterObject caster
        )
        {
            _timeScaleManager.AddCommand(new TimeScaleSkillCommand(id, float.MaxValue, casterTimeScale, othersTimeScale,
                caster));
        }

        public void RemoveTimeScaleCommand(string commandId)
        {
            _timeScaleManager.RemoveCommand(commandId);
        }

        public void ClearScene()
        {
            // 清空时间缩放命令
            _timeScaleManager.ClearAllCommands();

            // 销毁全部角色、AoE和子弹
            if (_god)
            {
                if (!_god.IsGameObjectDestroyed())
                {
                    GameObject.Destroy(_god.gameObject);
                }

                _god = null;
            }

            Characters.ForEach(DestroyCharacter);
            Packages.ForEach(DestroyPackage);
            RestSpots.ForEach(DestroyRestSpot);
            AoEs.ForEach(aoe =>
            {
                DestroyAoE(aoe);
                GameObject.Destroy(aoe.gameObject);
            });
            _aoes.Clear();
            Bullets.ForEach(bullet =>
            {
                DestroyBullet(bullet);
                GameObject.Destroy(bullet.gameObject);
            });
            _bullets.Clear();
        }

        private void CreateGameObjectFromPrefab(string prefabPath, Vector3 position, Quaternion rotation,
            UnityAction<GameObject> callback)
        {
            CreatePrefab<GameObject>(prefabPath, prefab =>
            {
                var instance = _objectResolver.Instantiate(prefab);
                var characterController = instance.GetComponent<CharacterController>();
                if (characterController)
                {
                    characterController.enabled = false;
                }

                instance.transform.position = position;
                instance.transform.rotation = rotation;
                if (characterController)
                {
                    characterController.enabled = true;
                }

                callback.Invoke(instance);
            });
        }

        private void CreatePrefab<T>(string prefabPath, UnityAction<T> callback) where T : UnityEngine.Object
        {
            _addressablesLoads.Add(new() { type = typeof(T), key = prefabPath });
            GameApplication.Instance.AddressablesManager.LoadAssetAsync<T>(prefabPath, callback.Invoke);
        }

        public void SetCustomCharacterTimeScale(CharacterObject character, float factor)
        {
            _customCharacterTimeScales[character.Parameters.id] = factor;
            SetCharacterTimeScale(character);
        }

        private float GetCharacterFinalTimeScale(int characterId)
        {
            var timeScale = _timeScaleManager.GetTimeScale(characterId);
            if (_customCharacterTimeScales.TryGetValue(characterId, out var customTimeScale))
            {
                return customTimeScale * timeScale;
            }

            return timeScale;
        }

        private void SetCharacterTimeScale(CharacterObject character)
        {
            var finalTimeScale = GetCharacterFinalTimeScale(character.Parameters.id);
            DebugUtil.LogMagenta($"角色({character.Parameters.DebugName})时间缩放设置为{finalTimeScale}");
            // 设置角色自身的时间缩放
            character.AnimationAbility?.SetAnimancerSpeed(finalTimeScale);
            character.AudioAbility?.SetSoundSpeed(finalTimeScale);
            character.SkillAbility?.SetSkillSpeed(finalTimeScale);
            // 设置角色发射的AoE的时间缩放
            AoEs.ForEach(aoe =>
            {
                if (aoe.caster.Parameters.id == character.Parameters.id)
                {
                    aoe.SetTimeScale(finalTimeScale);
                }
            });
            // 设置角色发射的子弹的时间缩放
            Bullets.ForEach(bullet =>
            {
                if (bullet.caster.Parameters.id == character.Parameters.id)
                {
                    bullet.SetTimeScale(finalTimeScale);
                }
            });
        }

        private void HandleCharacterTimeScaleChanged(int characterId, float timeScale)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                SetCharacterTimeScale(character);
            }
        }
    }
}