using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Framework.Common.Debug;
using Quest.Data;
using Quest.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Quest
{
    public delegate void OnQuestReceived(Runtime.Quest quest, bool mute);

    public delegate void OnQuestSubmitted(Runtime.Quest quest, bool mute);

    public class QuestManager : MonoBehaviour, IArchivable
    {
        [FormerlySerializedAs("questPool")] [LabelText("任务池")] [SerializeField] private QuestPool questPoolTemplate;
        private QuestPool _runningQuestPool;
        public QuestPool QuestPool => _runningQuestPool;

        [LabelText("调试")] [SerializeField] private bool debug;

        [Inject] private IObjectResolver _objectResolver;

        [ShowInInspector, ReadOnly] private readonly Dictionary<string, Runtime.Quest> _quests = new(); // 全部任务列表
        [ShowInInspector, ReadOnly] private readonly HashSet<string> _playerQuestIds = new(); // 玩家任务id集合

        public Runtime.Quest[] Quests => _quests.Values.ToArray();

        public Runtime.Quest[] PlayerQuests
        {
            get { return _playerQuestIds.Select(questId => _quests[questId]).ToArray(); }
        }

        public event OnQuestReceived OnQuestReceived;
        public event OnQuestSubmitted OnQuestSubmitted;

        private void Awake()
        {
            _runningQuestPool = questPoolTemplate.Clone();
            GameApplication.Instance.ArchiveManager.Register(this);
        }

        private void FixedUpdate()
        {
            // 每个逻辑帧更新未完成的任务状态
            Quests.ForEach(quest =>
            {
                // 如果任务已经完成就不再更新该任务
                if (quest.state.IsQuestCompleted())
                {
                    return;
                }

                UpdateQuest(quest, Time.fixedDeltaTime);
            });
        }

        private void OnDestroy()
        {
            _quests.Clear();
            _playerQuestIds.Clear();
            _runningQuestPool.Clear();
            GameObject.Destroy(_runningQuestPool);
            _runningQuestPool = null;
            GameApplication.Instance?.ArchiveManager.Unregister(this);
        }

        public bool TryGetQuest(string questId, out Runtime.Quest quest)
        {
            return _quests.TryGetValue(questId, out quest);
        }

        public void ReceiveQuest(string questId, bool mute = false)
        {
            if (!_quests.TryGetValue(questId, out var quest))
            {
                DebugUtil.LogError($"The Quest(id={questId}) is not found in the quest list");
                return;
            }

            if (quest.state != QuestState.RequirementMeet)
            {
                if (debug)
                {
                    DebugUtil.LogPurple($"不满足任务《{quest.info.title}》({quest.info.id})的接受条件");
                }

                return;
            }

            if (debug)
            {
                DebugUtil.LogPurple($"接受任务《{quest.info.title}》({quest.info.id})");
            }

            quest.state = QuestState.InProgress;
            _playerQuestIds.Add(questId);
            // 接受任务后立即更新任务
            UpdateQuest(quest, 0);
            // 调用事件
            OnQuestReceived?.Invoke(quest, mute);
        }

        public void SubmitQuest(string questId, bool mute = false)
        {
            if (!_quests.TryGetValue(questId, out var quest))
            {
                DebugUtil.LogError($"The Quest(id={questId}) is not found in the quest list");
                return;
            }

            if (quest.state != QuestState.AllStepsComplete)
            {
                if (debug)
                {
                    DebugUtil.LogPurple($"不满足任务《{quest.info.title}》({quest.info.id})的提交条件");
                }

                return;
            }

            if (debug)
            {
                DebugUtil.LogPurple($"提交任务《{quest.info.title}》({quest.info.id})");
            }

            quest.state = QuestState.Completed;
            _playerQuestIds.Add(questId);
            // 完成任务后立即更新任务
            UpdateQuest(quest, 0);
            // 调用事件
            OnQuestSubmitted?.Invoke(quest, mute);
        }

        public void Save(ArchiveData archiveData)
        {
            // 序列化存储任务到存档数据
            archiveData.quest.quests = PlayerQuests.Select(GetArchiveFromQuest).ToList();
        }

        public void Load(ArchiveData archiveData)
        {
            // 建立任务列表
            BuildQuests();
            _playerQuestIds.Clear();
            // 反序列化读取存档并设置任务数据
            archiveData.quest.quests.ForEach(UpdateQuestFromArchive);
        }

        private void BuildQuests()
        {
            // 将未完成的任务统一中断
            Quests.ForEach(quest =>
            {
                if (!quest.state.IsQuestInProgress())
                {
                    return;
                }

                // 中断正在执行的任务步骤
                quest.steps.ForEach(step =>
                {
                    if (step.triggered && !step.completed)
                    {
                        step.info.Interrupt?.Invoke(step);
                    }
                });
            });
            // 清空任务列表，并从预设任务池中重新构造列表
            _quests.Clear();
            _quests.AddRange(_runningQuestPool.QuestConfigs.ToDictionary(
                questConfig => questConfig.id,
                questConfig =>
                {
                    // 依赖注入
                    questConfig.requirements.ForEach(requirement => _objectResolver.Inject(requirement));
                    questConfig.steps.ForEach(step =>
                    {
                        _objectResolver.Inject(step);
                        step.goals.ForEach(goal => _objectResolver.Inject(goal));
                    });
                    questConfig.rewards.ForEach(reward => _objectResolver.Inject(reward));

                    // SO数据=>静态数据=>运行时数据
                    var questInfo = questConfig.ToQuestInfo();
                    var quest = new Runtime.Quest
                    {
                        info = questInfo,
                        state = QuestState.RequirementNotMeet,
                        requirements = questInfo.requirements.Select(requirementInfo => new QuestRequirement
                        {
                            info = requirementInfo,
                            description = "",
                            meet = false,
                        }).ToArray(),
                        steps = questInfo.steps.Select(stepInfo => new QuestStep
                        {
                            info = stepInfo,
                            triggered = false,
                            completed = false,
                            goals = stepInfo.goals.Select(goalInfo => new QuestGoal
                            {
                                info = goalInfo,
                                description = "",
                                triggered = false,
                                completed = false,
                                state = "",
                            }).ToArray(),
                        }).ToArray(),
                        rewards = questInfo.rewards.Select(rewardInfo => new QuestReward
                        {
                            info = rewardInfo,
                            given = false,
                        }).ToArray(),
                    };
                    return quest;
                }));
        }

        /// <summary>
        /// 更新任务状态
        /// 检查是否满足需求=>满足需求后接受任务=>检查任务进度=>检查任务完成=>领取任务奖励
        /// </summary>
        /// <param name="quest">运行时任务数据</param>
        /// <param name="deltaTime">执行间隔</param>
        /// <param name="autoReceive">是否自动接受任务</param>
        /// <param name="autoSubmit">是否自动提交任务</param>
        private void UpdateQuest(
            Runtime.Quest quest,
            float deltaTime,
            bool autoReceive = false,
            bool autoSubmit = false
        )
        {
            if (debug)
            {
                DebugUtil.LogPurple($"更新任务《{quest.info.title}》({quest.info.id})");
            }

            CheckQuestRequirement();

            if (autoReceive)
            {
                ReceiveQuest(quest.info.id, true);
            }

            UpdateQuestInProgress();

            if (autoSubmit)
            {
                SubmitQuest(quest.info.id, true);
            }

            TryToGiveQuestReward();

            return;

            void CheckQuestRequirement()
            {
                // 如果任务已经开始，就不再更新任务需求
                if (!quest.state.IsQuestNotStart())
                {
                    return;
                }

                // 更新任务需求数据
                quest.requirements.ForEach(requirement =>
                {
                    if (debug)
                    {
                        DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})执行需求({requirement.info.id})的更新函数");
                    }

                    requirement.info.Update?.Invoke(requirement, deltaTime);
                });

                // 如果任务不存在需求或全部需求均满足，则切换到需求满足状态，否则回退到需求不满足状态
                quest.state = quest.requirements.Length == 0 || quest.requirements.All(requirement => requirement.meet)
                    ? QuestState.RequirementMeet
                    : QuestState.RequirementNotMeet;
                if (debug)
                {
                    DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})" +
                                        (quest.state == QuestState.RequirementMeet ? "满足接受需求" : "不满足接受需求"));
                }
            }

            void UpdateQuestInProgress()
            {
                // 跳过不处于流程的任务
                if (quest.state != QuestState.InProgress)
                {
                    return;
                }

                // 如果发现当前任务没有步骤，也推进任务状态
                if (quest.steps.Length == 0)
                {
                    if (debug)
                    {
                        DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})完成所有步骤");
                    }

                    quest.state = QuestState.AllStepsComplete;
                    return;
                }

                // 依次遍历任务步骤执行未触发的步骤的对应节点函数
                for (var index = 0; index < quest.steps.Length; index++)
                {
                    var step = quest.steps[index];
                    // 如果步骤已经完成，跳过该任务步骤
                    var triggered = step.triggered;
                    if (step.completed)
                    {
                        // 未触发的任务步骤标记为触发
                        if (!triggered)
                        {
                            step.triggered = true;
                            step.goals.ForEach(goal => goal.triggered = true);
                        }

                        // 步骤完成就跳到下一个步骤
                        if (debug)
                        {
                            DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})完成步骤({step.info.id})");
                        }

                        continue;
                    }

                    // 步骤未触发则需要执行步骤的开始节点函数
                    if (!triggered)
                    {
                        if (debug)
                        {
                            DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})执行步骤({step.info.id})的开始函数");
                        }

                        step.info.Start?.Invoke(step);
                        step.triggered = true;
                    }

                    // 执行步骤的更新节点函数
                    if (debug)
                    {
                        DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})执行步骤({step.info.id})的更新函数");
                    }

                    step.info.Update?.Invoke(step, triggered ? deltaTime : 0);

                    // 在更新后判断步骤是否完成，如果未完成则直接返回，即任务卡在这一步骤了
                    if (!step.completed)
                    {
                        return;
                    }

                    // 完成了就执行步骤的完成节点函数，并跳到下一步骤
                    if (debug)
                    {
                        DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})执行步骤({step.info.id})的完成函数");
                    }

                    step.info.Complete?.Invoke(step);
                }

                // 到达这里说明全部任务步骤已经完成，推进任务状态
                quest.state = QuestState.AllStepsComplete;
                if (debug)
                {
                    DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})完成所有步骤");
                }
            }

            void TryToGiveQuestReward()
            {
                // 跳过非完成状态的任务
                if (quest.state != QuestState.Completed)
                {
                    return;
                }

                // 遍历奖励列表，执行其奖励函数
                quest.rewards.ForEach(reward =>
                {
                    if (reward.given) return;
                    if (debug)
                    {
                        DebugUtil.LogPurple($"任务《{quest.info.title}》({quest.info.id})给予奖励({reward.info.id})");
                    }

                    reward.info.GiveReward?.Invoke();
                    reward.given = true;
                });
                // 推进任务状态
                quest.state = QuestState.RewardGiven;
                if (debug)
                {
                    DebugUtil.LogPurple($"完成任务《{quest.info.title}》({quest.info.id})");
                }
            }
        }

        private QuestItemArchiveData GetArchiveFromQuest(Runtime.Quest quest)
        {
            return new QuestItemArchiveData
            {
                id = quest.info.id,
                state = quest.state,
                requirements = quest.requirements.ToDictionary(requirement => requirement.info.id, requirement =>
                    new QuestRequirementArchiveData
                    {
                        id = requirement.info.id,
                        description = requirement.description,
                        meet = requirement.meet
                    }),
                steps = quest.steps.ToDictionary(step => step.info.id, step => new QuestStepArchiveData
                    {
                        id = step.info.id,
                        completed = step.completed,
                        goals = step.goals.ToDictionary(goal => goal.info.id, goal => new QuestGoalArchiveData
                            {
                                id = goal.info.id,
                                description = goal.description,
                                completed = goal.completed,
                                state = goal.state,
                            }
                        )
                    }
                ),
                rewards = quest.rewards.ToDictionary(reward => reward.info.id, reward => new QuestRewardArchiveData
                    {
                        id = reward.info.id,
                        given = reward.given
                    }
                )
            };
        }

        private void UpdateQuestFromArchive(QuestItemArchiveData itemArchiveData)
        {
            if (!_quests.TryGetValue(itemArchiveData.id, out var quest))
            {
                return;
            }

            // 设置匹配的需求
            quest.requirements.ForEach(requirement =>
            {
                if (!itemArchiveData.requirements.TryGetValue(requirement.info.id, out var requirementArchiveData))
                    return;
                requirement.description = requirementArchiveData.description;
                requirement.meet = requirementArchiveData.meet;
            });
            // 设置匹配的步骤
            quest.steps.ForEach(step =>
            {
                if (!itemArchiveData.steps.TryGetValue(step.info.id, out var stepArchiveData)) return;
                step.completed = stepArchiveData.completed;
                step.goals.ForEach(goal =>
                {
                    if (!stepArchiveData.goals.TryGetValue(goal.info.id, out var goalArchiveData)) return;
                    goal.description = goalArchiveData.description;
                    goal.completed = goalArchiveData.completed;
                    goal.state = goalArchiveData.state;
                });
            });
            // 设置匹配的奖励
            quest.rewards.ForEach(reward =>
            {
                if (!itemArchiveData.rewards.TryGetValue(reward.info.id, out var rewardArchiveData)) return;
                reward.given = rewardArchiveData.given;
            });

            // 最后根据存档任务状态更新任务
            UpdateQuest(quest, 0, !itemArchiveData.state.IsQuestNotStart(), itemArchiveData.state.IsQuestCompleted());
        }
    }
}