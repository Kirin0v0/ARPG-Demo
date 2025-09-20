using System;
using System.Collections.Generic;
using Archive;
using Archive.Data;
using Camera.Data;
using Character;
using Events;
using Framework.Common.Debug;
using Framework.Core.LiveData;
using Map;
using Skill;
using Skill.Runtime;
using VContainer;
using VContainer.Unity;

namespace Camera
{
    public class CameraModel : ICameraModel, IStartable, IDisposable
    {
        [Inject] private MapManager _mapManager;

        private readonly MutableLiveData<CameraSceneData>
            _sceneData = new(new CameraSceneData(), LiveDataMode.Debounce);

        public LiveData<CameraSceneData> GetScene() => _sceneData;

        private readonly MutableLiveData<CameraLockData> _lockData = new(
            new CameraLockData
            {
                @lock = false,
                lockTarget = null,
            }, LiveDataMode.Debounce
        );

        public void Start()
        {
            _mapManager.BeforeMapLoad += Reset;
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject[]>(GameEvents.StartTargetSelection,
                OnStartTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(GameEvents.FinishTargetSelection,
                OnFinishTargetSelection);
            // GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            // GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.FinishPlayerSkill, OnCompletePlayerSkill);
            // GameApplication.Instance.EventCenter.AddEventListener(GameEvents.EnterDialogue, OnEnterDialogue);
            // GameApplication.Instance.EventCenter.AddEventListener(GameEvents.ExitDialogue, OnExitDialogue);
        }

        public void Dispose()
        {
            _mapManager.BeforeMapLoad -= Reset;
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject[]>(GameEvents.StartTargetSelection,
                OnStartTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(GameEvents.FinishTargetSelection,
                OnFinishTargetSelection);
            // GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            // GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.FinishPlayerSkill, OnCompletePlayerSkill);
            // GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.EnterDialogue, OnEnterDialogue);
            // GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.ExitDialogue, OnExitDialogue);
        }

        public void SetLockData(CameraLockData lockData)
        {
            _lockData.SetValue(lockData);
        }

        public LiveData<CameraLockData> GetLock() => _lockData;

        private void ResetScene()
        {
            _sceneData.SetValue(new CameraSceneData());
        }

        private void OnStartTargetSelection(CharacterObject[] targets)
        {
            _sceneData.SetValue(_sceneData.Value.EnterSelection());
        }

        private void OnCancelTargetSelection()
        {
            _sceneData.SetValue(_sceneData.Value.ExitSelection());
        }

        private void OnFinishTargetSelection(CharacterObject target)
        {
            _sceneData.SetValue(_sceneData.Value.ExitSelection());
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _sceneData.SetValue(_sceneData.Value.EnterCustom());
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _sceneData.SetValue(_sceneData.Value.ExitCustom());
        }

        private void OnEnterDialogue()
        {
            _sceneData.SetValue(_sceneData.Value.EnterCustom());
        }

        private void OnExitDialogue()
        {
            _sceneData.SetValue(_sceneData.Value.ExitCustom());
        }

        private void Reset()
        {
            // 重置相机数据
            SetLockData(new CameraLockData
            {
                @lock = false,
                lockTarget = null,
            });
            ResetScene();
        }
    }
}