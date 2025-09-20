using System;
using System.Collections.Generic;
using Archive;
using Archive.Data;
using Common;
using Framework.Common.Debug;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework.Internal;
using Player;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Dialogue
{
    /// <summary>
    /// 对话管理器，用于如下：
    /// 1.每个对话树开始时注入动态角色参数
    /// 2.对话图参数的持久化
    /// </summary>
    public class DialogueManager : SerializedMonoBehaviour, IArchivable
    {
        private const string Separator = "-";

        public event System.Action<DialogueTree> OnPlayerEnterDialogue;
        public event System.Action<DialogueTree> OnPlayerExitDialogue;

        [Inject] private GameManager _gameManager;

        [ShowInInspector, ReadOnly] private readonly Dictionary<string, string> _parameters = new(); // 存储对话树参数值

        private readonly List<DialogueTree> _ongoingDialogueTrees = new();

        private void Awake()
        {
            // 监听存档系统保存和读取
            GameApplication.Instance.ArchiveManager.Register(this);
            // 监听角色创建和销毁
            _gameManager.OnPlayerCreated += HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed += HandlePlayerDestroyed;
            // 监听对话树开始和结束
            DialogueTree.OnDialogueStarted += HandleDialogueStarted;
            DialogueTree.OnDialogueFinished += HandleDialogueFinished;
        }

        private void OnDestroy()
        {
            // 取消监听存档系统保存和读取
            GameApplication.Instance?.ArchiveManager.Unregister(this);
            // 取消监听角色创建和销毁
            _gameManager.OnPlayerCreated -= HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
            if (_gameManager.Player)
            {
                HandlePlayerDestroyed(_gameManager.Player);
            }

            // 取消监听对话树开始和结束
            DialogueTree.OnDialogueStarted -= HandleDialogueStarted;
            DialogueTree.OnDialogueFinished -= HandleDialogueFinished;
        }

        public void SetDialogueParameter(string key, string value)
        {
            if (_parameters.ContainsKey(key))
            {
                _parameters[key] = value;
            }
            else
            {
                _parameters.Add(key, value);
            }
        }

        public bool TryGetDialogueParameter(string key, out string value)
        {
            return _parameters.TryGetValue(key, out value);
        }

        public void Save(ArchiveData archiveData)
        {
            archiveData.dialogue.parameters.Clear();
            _parameters.ForEach(pair => { archiveData.dialogue.parameters.Add(pair.Key, pair.Value); });
        }

        public void Load(ArchiveData archiveData)
        {
            _parameters.Clear();
            archiveData.dialogue.parameters.ForEach(pair => { _parameters.Add(pair.Key, pair.Value); });
        }

        private void HandlePlayerCreated(PlayerCharacterObject player)
        {
            if (player.DialogueAbility)
            {
                player.DialogueAbility.OnEnterDialogue += HandlePlayerEnterDialogue;
                player.DialogueAbility.OnExitDialogue += HandlePlayerExitDialogue;
            }
        }

        private void HandlePlayerDestroyed(PlayerCharacterObject player)
        {
            if (player.DialogueAbility)
            {
                player.DialogueAbility.OnEnterDialogue -= HandlePlayerEnterDialogue;
                player.DialogueAbility.OnExitDialogue -= HandlePlayerExitDialogue;
            }
        }

        private void HandleDialogueStarted(DialogueTree dialogueTree)
        {
            DebugUtil.LogLightBlue($"开启对话树({dialogueTree.name})");
            _ongoingDialogueTrees.Add(dialogueTree);
            // 设置对话树涉及到的角色
            dialogueTree.actorParameters.ForEach(parameter =>
            {
                // 发起对话树的角色自身不设置
                if (parameter.name == DialogueTree.INSTIGATOR_NAME)
                {
                    return;
                }

                dialogueTree.SetActorReference(parameter.name, null);
                foreach (var character in _gameManager.Characters)
                {
                    if (character && character.Parameters.prototype == parameter.name)
                    {
                        dialogueTree.SetActorReference(parameter.name, character.DialogueAbility.DialogueActor);
                    }
                }
            });
            // 从存档中读取该对话树保存的参数，并设置到对话树中（如果不存在则采用对话树默认的值）
            dialogueTree.blackboard.variables.ForEach(parameter =>
            {
                if (TryGetDialogueParameter(dialogueTree.name + Separator + parameter.Key, out var value))
                {
                    if (parameter.Value.varType == typeof(string))
                    {
                        parameter.Value.value = value;
                    }
                    else if (parameter.Value.varType == typeof(bool))
                    {
                        parameter.Value.value = bool.Parse(value);
                    }
                    else if (parameter.Value.varType == typeof(int))
                    {
                        parameter.Value.value = int.Parse(value);
                    }
                    else if (parameter.Value.varType == typeof(float))
                    {
                        parameter.Value.value = float.Parse(value);
                    }
                }
            });
            // 检查对话树角色参数，并执行对应角色的进入逻辑
            foreach (var actorParameter in dialogueTree.definedActorParameterNames)
            {
                var dialogueActor = dialogueTree.GetActorReferenceByName(actorParameter);
                if (dialogueActor is DialogueCharacterActor characterActor && characterActor.Reference?.Value != null)
                {
                    var character = characterActor.Reference!.Value;
                    DebugUtil.LogLightBlue($"角色({character.Parameters.DebugName})加入对话树({dialogueTree.name})");
                    character.DialogueAbility?.EnterDialogue(dialogueTree);
                }
            }
        }

        private void HandleDialogueFinished(DialogueTree dialogueTree)
        {
            DebugUtil.LogLightBlue($"关闭对话树({dialogueTree.name})");
            _ongoingDialogueTrees.Remove(dialogueTree);
            // 将对话树最新的参数数据缓存到内存中
            dialogueTree.blackboard.variables.ForEach(parameter =>
            {
                SetDialogueParameter(dialogueTree.name + Separator + parameter.Key,
                    parameter.Value.value.ToString());
            });
            // 检查对话树角色参数，并执行对应角色的退出逻辑
            foreach (var actorParameter in dialogueTree.definedActorParameterNames)
            {
                var dialogueActor = dialogueTree.GetActorReferenceByName(actorParameter);
                if (dialogueActor is DialogueCharacterActor characterActor && characterActor.Reference?.Value != null)
                {
                    var character = characterActor.Reference!.Value;
                    DebugUtil.LogLightBlue($"角色({character.Parameters.DebugName})退出对话树({dialogueTree.name})");
                    character.DialogueAbility?.ExitDialogue(dialogueTree);
                }
            }
        }

        private void HandlePlayerEnterDialogue(DialogueTree dialogueTree)
        {
            OnPlayerEnterDialogue?.Invoke(dialogueTree);
        }

        private void HandlePlayerExitDialogue(DialogueTree dialogueTree)
        {
            OnPlayerExitDialogue?.Invoke(dialogueTree);
        }
    }
}