using System;
using System.Collections.Generic;
using Character;
using Common;
using Events;
using Features.Game.Data;
using Features.Game.UI;
using Framework.Common.Debug;
using Framework.Core.LiveData;
using Player;
using Skill;
using Skill.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Game
{
    public class GameModel : IGameModel, IStartable, ITickable, IDisposable
    {
        [Inject] private IGameUIModel _gameUIModel;

        // 游戏时间设置引用计数字典，用于多处同时设置时间模式后解除自身设置却波及到其他地方设置的问题
        private int _pauseTimeReferenceCount = 0;

        private readonly MutableLiveData<GameTimeData> _gameTimeLiveData =
            new(value: new GameTimeData(), mode: LiveDataMode.Debounce);

        public LiveData<GameTimeData> GetGameTime() => _gameTimeLiveData;

        // 禁止玩家输入计数，用于多模块的输入控制
        private int _banPlayerInputCount = 0;

        private readonly MutableLiveData<bool> _allowPlayerInput =
            new MutableLiveData<bool>(value: true, mode: LiveDataMode.Debounce);

        public LiveData<bool> AllowPlayerInput() => _allowPlayerInput;

        // 游标展示计数，用于多模块的游标展示
        private int _visibleCursorCount = 0;
        private readonly MutableLiveData<bool> _showCursor = new(value: false, mode: LiveDataMode.Debounce);
        public LiveData<bool> ShowCursor() => _showCursor;

        // 玩家魔女时间激活倒计时，这里设计的前提是玩家魔女时间缩放固定，且存在同一时间段多次激活的可能性，业务仅关心是否激活魔女时间
        private readonly List<float> _playerWitchTimeCountdowns = new();

        private readonly MutableLiveData<bool> _playerWitchTimeActive =
            new MutableLiveData<bool>(value: false, LiveDataMode.Debounce);

        public LiveData<bool> IsPlayerWitchTimeActive() => _playerWitchTimeActive;

        public void Start()
        {
            // 监听系统UI展示导致的时间模式设置
            _gameUIModel.GetLoadingUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetCutsceneUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetMenuUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().ObserveForever(SetTimeModeWhenSystemUIDataChanged);
            // 监听系统UI展示导致的玩家输入控制切换
            _gameUIModel.GetLoadingUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetCutsceneUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetMenuUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetDeathUI().ObserveForever(SwitchPlayerInputWhenSystemUIDataChanged);
            // 监听系统UI展示导致的游标显示度切换
            _gameUIModel.GetMenuUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetDeathUI().ObserveForever(SwitchCursorVisibilityWhenSystemUIDataChanged);
            // 监听战斗命令列表UI展示导致的时间模式设置、玩家输入切换和游标显示度切换
            _gameUIModel.IsBattleCommandExpanding().ObserveForever(OnBattleCommandExpanding);
            // 监听对话展示导致的玩家输入切换和游标显示度切换
            _gameUIModel.IsDialogueShowing().ObserveForever(OnDialogue);
            // 监听技能释放过程导致的玩家输入切换
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.CompletePlayerSkill,
                OnCompletePlayerSkill);
            // 监听是否允许GUI显示
            _gameUIModel.AllowGUIShowing().ObserveForever(CheckAllowGUIShow);
        }

        public void Tick()
        {
            // 更新倒计时
            for (var i = 0; i < _playerWitchTimeCountdowns.Count; i++)
            {
                _playerWitchTimeCountdowns[i] -= Time.deltaTime;
            }

            // 删除到时的记录
            var index = 0;
            while (index < _playerWitchTimeCountdowns.Count)
            {
                var countdown = _playerWitchTimeCountdowns[index];
                if (countdown <= 0f)
                {
                    _playerWitchTimeCountdowns.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // 更新魔女时间是否处于激活状态
            _playerWitchTimeActive.SetValue(_playerWitchTimeCountdowns.Count != 0);
        }

        public void Dispose()
        {
            // 解除监听系统UI展示导致的时间模式设置
            _gameUIModel.GetLoadingUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetCutsceneUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetMenuUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().RemoveObserver(SetTimeModeWhenSystemUIDataChanged);
            // 解除监听系统UI展示导致的玩家输入控制切换
            _gameUIModel.GetLoadingUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetCutsceneUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetMenuUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            _gameUIModel.GetDeathUI().RemoveObserver(SwitchPlayerInputWhenSystemUIDataChanged);
            // 解除监听系统UI展示导致的游标显示度切换
            _gameUIModel.GetMenuUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetMapUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetCharacterUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetPackageUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetQuestUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetArchiveUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            _gameUIModel.GetDeathUI().RemoveObserver(SwitchCursorVisibilityWhenSystemUIDataChanged);
            // 解除监听战斗命令列表UI展示导致的时间模式设置、玩家输入切换和游标显示度切换
            _gameUIModel.IsBattleCommandExpanding().RemoveObserver(OnBattleCommandExpanding);
            // 解除监听对话展示导致的玩家输入切换和游标显示度切换
            _gameUIModel.IsDialogueShowing().RemoveObserver(OnDialogue);
            // 解除监听技能释放过程导致的玩家输入切换
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.CompletePlayerSkill,
                OnCompletePlayerSkill);
            // 解除监听是否允许GUI显示
            _gameUIModel.AllowGUIShowing().RemoveObserver(CheckAllowGUIShow);
        }

        public void ActivePlayerWitchTime(float fixedDuration)
        {
            _playerWitchTimeCountdowns.Add(fixedDuration);
            _playerWitchTimeActive.SetValue(true);
        }

        public void ClearPlayerWitchTime()
        {
            _playerWitchTimeCountdowns.Clear();
            _playerWitchTimeActive.SetValue(false);
        }
        
        private void SetTimeModeWhenSystemUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                PauseTime();
            }
            else
            {
                ResumeTime();
            }
        }

        private void SwitchPlayerInputWhenSystemUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                ClosePlayerInput();
            }
            else
            {
                OpenPlayerInput();
            }
        }

        private void SwitchCursorVisibilityWhenSystemUIDataChanged(GameUIData data)
        {
            if (data.Visible)
            {
                VisibleCursor();
            }
            else
            {
                InvisibleCursor();
            }
        }

        private void OnBattleCommandExpanding(bool expanding)
        {
            if (expanding)
            {
                ClosePlayerInput();
                VisibleCursor();
            }
            else
            {
                OpenPlayerInput();
                InvisibleCursor();
            }
        }

        private void OnDialogue(bool inDialogue)
        {
            if (inDialogue)
            {
                ClosePlayerInput();
                VisibleCursor();
            }
            else
            {
                OpenPlayerInput();
                InvisibleCursor();
            }
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            ClosePlayerInput();
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            OpenPlayerInput();
        }

        private void CheckAllowGUIShow(bool allow)
        {
            GameApplication.Instance?.EventCenter.TriggerEvent(allow
                ? GameEvents.AllowGUIShow
                : GameEvents.BanGUIShow);
        }

        private void PauseTime()
        {
            _pauseTimeReferenceCount = Math.Max(_pauseTimeReferenceCount + 1, 0);
            RefreshGameTime();
        }

        private void ResumeTime()
        {
            _pauseTimeReferenceCount = Math.Max(_pauseTimeReferenceCount - 1, 0);
            RefreshGameTime();
        }

        private void RefreshGameTime()
        {
            var gameTimeData = _gameTimeLiveData.Value;
            gameTimeData = _pauseTimeReferenceCount > 0 ? gameTimeData.PauseTime() : gameTimeData.ResumeTime();
            _gameTimeLiveData.SetValue(gameTimeData);
        }

        private void OpenPlayerInput()
        {
            _banPlayerInputCount = Math.Max(_banPlayerInputCount - 1, 0);
            RefreshPlayerInput();
        }

        private void ClosePlayerInput()
        {
            _banPlayerInputCount = Math.Max(_banPlayerInputCount + 1, 0);
            RefreshPlayerInput();
        }

        private void RefreshPlayerInput()
        {
            _allowPlayerInput.SetValue(_banPlayerInputCount <= 0);
        }

        private void VisibleCursor()
        {
            _visibleCursorCount = Math.Max(_visibleCursorCount + 1, 0);
            RefreshCursorVisibility();
        }

        private void InvisibleCursor()
        {
            _visibleCursorCount = Math.Max(_visibleCursorCount - 1, 0);
            RefreshCursorVisibility();
        }

        private void RefreshCursorVisibility()
        {
            _showCursor.SetValue(_visibleCursorCount > 0);
        }
    }
}