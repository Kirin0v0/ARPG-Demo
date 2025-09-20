using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using Common;
using Dialogue;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.Util;
using Inputs;
using NodeCanvas.DialogueTrees;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

namespace Features.Game.UI.Dialogue
{
    [Serializable]
    public class DialogueDelays
    {
        public float characterDelay = 0.05f;
        public float commaDelay = 0.1f;
        public float sentenceDelay = 0.3f;
        public float finalDelay = 1.2f;
    }

    public class GameDialoguePanel : BaseUGUIPanel
    {
        [Inject] private GameManager _gameManager;
        [Inject] private DialogueManager _dialogueManager;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;

        [Title("对话输入配置")] [SerializeField] private bool allowSkipDialogueEffect = true;
        [SerializeField] private bool showTypewriterEffect = false;
        [SerializeField] private DialogueDelays delays = new();
        [SerializeField] private List<AudioClip> typingSounds = new();
        [SerializeField] private bool waitUntilInput = true;

        [Title("对话选项配置")] [SerializeField] private int maxVisibleOptionCount = 3;
        [SerializeField] private GameDialogueOptionListAdapter adapter;
        [SerializeField] private RecyclerViewLinearLayoutManager layoutManager;

        private TextMeshProUGUI _textName;
        private VerticalLayoutGroup _dialogueTextLayout;
        private TextMeshProUGUI _textDialogue;
        private TextMeshProUGUI _textWaitInput;
        private RecyclerView _rvDialogueOptionList;
        private HorizontalLayoutGroup _optionTipLayout;

        private CharacterObject _dialogueCharacter;
        private List<int> _dialogueSoundIds;

        private bool IsPlayerInDialogue => _gameManager.Player && _gameManager.Player.Parameters.inDialogue;
        private bool WasSkipInput => _playerInputManager.WasPerformedThisFrame(InputConstants.Submit) ||
                                     _playerInputManager.WasPerformedThisFrame(InputConstants.Click) ||
                                     _playerInputManager.WasPerformedThisFrame(InputConstants.RightClick);

        protected override void OnInit()
        {
            _textName = GetWidget<TextMeshProUGUI>("TextName");
            _dialogueTextLayout = GetWidget<VerticalLayoutGroup>("DialogueTextLayout");
            _textDialogue = GetWidget<TextMeshProUGUI>("TextDialogue");
            _textWaitInput = GetWidget<TextMeshProUGUI>("TextWaitInput");
            _rvDialogueOptionList = GetWidget<RecyclerView>("RvDialogueOptionList");
            _rvDialogueOptionList.Init();
            _rvDialogueOptionList.LayoutManager = layoutManager;
            _rvDialogueOptionList.Adapter = adapter;
            _optionTipLayout = GetWidget<HorizontalLayoutGroup>("OptionTipLayout");
        }

        protected override void OnShow(object payload)
        {
            // 监听玩家进入对话和退出对话
            _dialogueManager.OnPlayerEnterDialogue += OnDialogueStarted;
            _dialogueManager.OnPlayerExitDialogue += OnDialogueFinished;
            
            // 监听对话树当前对话的更新
            DialogueTree.OnSubtitlesRequest += OnSubtitlesRequest;
            DialogueTree.OnMultipleChoiceRequest += OnMultipleChoiceRequest;

            gameObject.SetActive(false);
        }

        protected override void OnShowingUpdate(bool focus)
        {
        }

        protected override void OnHide()
        {
            // 解除监听玩家进入对话和退出对话
            _dialogueManager.OnPlayerEnterDialogue -= OnDialogueStarted;
            _dialogueManager.OnPlayerExitDialogue -= OnDialogueFinished;
            
            // 解除监听对话树当前对话的更新
            DialogueTree.OnSubtitlesRequest -= OnSubtitlesRequest;
            DialogueTree.OnMultipleChoiceRequest -= OnMultipleChoiceRequest;
        }

        private void OnDialogueStarted(DialogueTree dialogueTree)
        {
            gameObject.SetActive(true);
            ClearDialogueText();
            ClearOptionList();
            _gameUIModel.DialogueShowing.SetValue(true);
        }

        private void OnDialogueFinished(DialogueTree dialogueTree)
        {
            StopAllCoroutines();
            ClearDialogueAudio();
            gameObject.SetActive(false);
            _gameUIModel.DialogueShowing.SetValue(false);
        }

        private void OnSubtitlesRequest(SubtitlesRequestInfo info)
        {
            // 如果玩家不处于对话，就直接跳过该对话
            if (!IsPlayerInDialogue)
            {
                info.Continue();
                return;
            }
            
            var text = info.statement.text;
            var audio = info.statement.audio;
            var actor = info.actor;

            gameObject.SetActive(true);
            _textName.text = actor.name;
            _textDialogue.color = actor.dialogueColor;
            _dialogueTextLayout.gameObject.SetActive(true);
            ClearOptionList();
            _optionTipLayout.gameObject.SetActive(false);

            StartCoroutine(OnSubtitlesRequestInfoInternal());
            return;

            IEnumerator OnSubtitlesRequestInfoInternal()
            {
                // 展示对话时停顿一帧，再执行后续逻辑
                _textWaitInput.gameObject.SetActive(allowSkipDialogueEffect);
                _textDialogue.text = "";
                yield return null;
                
                _dialogueCharacter = actor.transform.GetComponent<CharacterObject>();
                _dialogueSoundIds = new List<int>();
                // 如果对话存在音频，则直接展示全部文本，等待输入或音频结束后继续执行
                if (audio && _dialogueCharacter)
                {
                    // 播放音频
                    _dialogueSoundIds.Add(_dialogueCharacter.AudioAbility?.PlaySound(audio, false, 1f) ?? -1);
                    // 展示全部文本
                    _textDialogue.text = text;
                    var timer = 0f;
                    while (timer < audio.length)
                    {
                        // 在等待音频播放结束期间允许跳过效果且玩家输入按键，则立即停止音频
                        if (allowSkipDialogueEffect && WasSkipInput)
                        {
                            ClearDialogueAudio();
                            break;
                        }

                        timer += Time.deltaTime;
                        yield return null;
                    }

                    ClearDialogueAudio();
                }

                // 如果对话不存在音频，如果设置逐字就逐字展示文本（同时播放逐字音频），否则就直接展示全部文本（这里不会播放音频）
                if (!audio)
                {
                    if (!showTypewriterEffect)
                    {
                        _textDialogue.text = text;
                    }
                    else
                    {
                        var tempText = "";
                        var skip = false;
                        // 这里新开协程来检查输入
                        if (allowSkipDialogueEffect)
                        {
                            StartCoroutine(CheckInput(() => skip = true));
                        }

                        // 逐字展示文本
                        for (var i = 0; i < text.Length; i++)
                        {
                            if (allowSkipDialogueEffect && skip)
                            {
                                _textDialogue.text = text;
                                yield return null;
                                UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(gameObject);
                                break;
                            }

                            var c = text[i];
                            tempText += c;
                            yield return StartCoroutine(DelayPrint(delays.characterDelay));
                            PlayTypeSound(actor.transform);
                            if (c == '.' || c == '!' || c == '?' || c == '。' || c == '！' || c == '？')
                            {
                                yield return StartCoroutine(DelayPrint(delays.sentenceDelay));
                                PlayTypeSound(actor.transform);
                            }

                            if (c == ',' || c == '，')
                            {
                                yield return StartCoroutine(DelayPrint(delays.commaDelay));
                                PlayTypeSound(actor.transform);
                            }

                            _textDialogue.text = tempText;
                        }
                    }

                    if (!waitUntilInput)
                    {
                        yield return StartCoroutine(DelayPrint(delays.finalDelay));
                    }
                }

                if (waitUntilInput)
                {
                    _textWaitInput.gameObject.SetActive(true);
                    while (!WasSkipInput)
                    {
                        yield return null;
                    }

                    _textWaitInput.gameObject.SetActive(false);
                }

                StopAllCoroutines();
                gameObject.SetActive(false);
                _dialogueTextLayout.gameObject.SetActive(false);
                _textWaitInput.gameObject.SetActive(false);
                info.Continue();
            }

            IEnumerator CheckInput(System.Action toDo)
            {
                while (!WasSkipInput)
                {
                    yield return null;
                }

                toDo();
            }

            IEnumerator DelayPrint(float time)
            {
                var timer = 0f;
                while (timer < time)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            void PlayTypeSound(Transform target)
            {
                if (typingSounds.Count <= 0) return;
                ClearDialogueAudio();
                var sound = typingSounds[Random.Range(0, typingSounds.Count)];
                if (sound && _dialogueCharacter)
                {
                    // 播放音频
                    _dialogueSoundIds.Add(_dialogueCharacter.AudioAbility?.PlaySound(audio, false, 1f) ?? -1);
                }
            }
        }

        private void OnMultipleChoiceRequest(MultipleChoiceRequestInfo info)
        {
            // 如果玩家不处于对话，就直接跳过该对话
            if (!IsPlayerInDialogue)
            {
                info.SelectOption(0);
                return;
            }
            
            var actor = info.actor;
            
            gameObject.SetActive(true);
            // 展示选项时将非本人文本额外添加其发言人名称
            if (!string.IsNullOrEmpty(_textName.text) && _textName.text != actor.name)
            {
                _textDialogue.text = $"{_textName.text}：{_textDialogue.text}";
            }

            _textName.text = actor.name;
            _textDialogue.color = actor.dialogueColor;
            _optionTipLayout.gameObject.SetActive(true);

            // 如果展示之前的对话，就重新显示
            _dialogueTextLayout.gameObject.SetActive(info.showLastStatement);

            // 将选项转为UI数据并展示列表
            List<GameDialogueOptionUIData> optionUIDataList = new List<GameDialogueOptionUIData>();
            optionUIDataList.AddRange(info.options.Select((pair, index) =>
                new GameDialogueOptionUIData
                {
                    Index = pair.Value,
                    Message = pair.Key.text,
                    Focused = index == 0,
                    OnNavigationMoved = HandleOptionNavigationMoved,
                    OnClicked = HandleOptionClicked
                }
            ));
            SetOptionList(optionUIDataList);

            // // 这里使用协程来监听输入
            // StartCoroutine(OnMultipleChoiceRequestInternal());

            return;

            void HandleOptionNavigationMoved(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
            {
                if (viewHolder is not GameDialogueOptionItemViewHolder itemViewHolder ||
                    itemViewHolder.Position == RecyclerView.NoPosition)
                {
                    return;
                }

                switch (moveDirection)
                {
                    case MoveDirection.Up:
                    {
                        if (itemViewHolder.Position <= 0)
                        {
                            return;
                        }

                        var index = itemViewHolder.Position - 1;
                        var data = optionUIDataList[index];
                        data.Focused = true;
                        adapter.RefreshItem(index, data);
                        _rvDialogueOptionList.FocusItem(index, true);
                    }
                        break;
                    case MoveDirection.Down:
                    {
                        if (itemViewHolder.Position >= optionUIDataList.Count - 1)
                        {
                            return;
                        }

                        var index = itemViewHolder.Position + 1;
                        var data = optionUIDataList[index];
                        data.Focused = true;
                        adapter.RefreshItem(index, data);
                        _rvDialogueOptionList.FocusItem(index, true);
                    }
                        break;
                }
            }

            void HandleOptionClicked(RecyclerViewHolder viewHolder)
            {
                StartCoroutine(HandleOptionClickedInternal(viewHolder));
            }
            
            IEnumerator HandleOptionClickedInternal(RecyclerViewHolder viewHolder)
            {
                if (viewHolder is not GameDialogueOptionItemViewHolder itemViewHolder ||
                    itemViewHolder.Position == RecyclerView.NoPosition)
                {
                    yield break;
                }

                var index = itemViewHolder.Data.Index;
                _dialogueTextLayout.gameObject.SetActive(false);
                ClearOptionList();
                StopAllCoroutines();
                _optionTipLayout.gameObject.SetActive(false);
                gameObject.SetActive(false);
                info.SelectOption(index);
            }

            // IEnumerator OnMultipleChoiceRequestInternal()
            // {
            //     while (true)
            //     {
            //         if (!Focus)
            //         {
            //             yield return null;
            //             continue;
            //         }
            //
            //         // 检测确认输入
            //         if (WasSkipInput)
            //         {
            //             var focusedIndex = optionUIDataList.FindIndex(x => x.Focused);
            //             var index = optionUIDataList[focusedIndex].Index;
            //             _dialogueTextLayout.gameObject.SetActive(false);
            //             ClearOptionList();
            //             yield return null;
            //             StopAllCoroutines();
            //             gameObject.SetActive(false);
            //             info.SelectOption(index);
            //             break;
            //         }
            //
            //         yield return null;
            //     }
            // }
        }

        private void ClearDialogueAudio()
        {
            if (_dialogueCharacter == null || _dialogueSoundIds == null)
            {
                return;
            }

            _dialogueSoundIds.ForEach(id => _dialogueCharacter.AudioAbility?.StopSound(id));
        }

        private void ClearDialogueText()
        {
            _dialogueTextLayout.gameObject.SetActive(false);
            _textWaitInput.gameObject.SetActive(false);
            _textDialogue.text = "";
        }

        private void ClearOptionList()
        {
            SetOptionList(new List<GameDialogueOptionUIData>());
        }

        private void SetOptionList(List<GameDialogueOptionUIData> list)
        {
            var layoutElement = _rvDialogueOptionList.GetComponent<LayoutElement>();
            var rectTransform = _rvDialogueOptionList.GetComponent<RectTransform>();
            if (list.Count <= maxVisibleOptionCount)
            {
                var height = adapter.GetItemHeight() * list.Count +
                             Mathf.Max(list.Count - 1, 0) * layoutManager.GetSpacing() +
                             layoutManager.GetPadding().vertical;
                layoutElement.preferredHeight = height;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
            }
            else
            {
                var height = adapter.GetItemHeight() * maxVisibleOptionCount +
                             maxVisibleOptionCount * layoutManager.GetSpacing()
                             + layoutManager.GetPadding().vertical
                             + adapter.GetItemHeight() / 2f;
                layoutElement.preferredHeight = height;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
            }

            _rvDialogueOptionList.Adapter.SetData(list);
        }
    }
}