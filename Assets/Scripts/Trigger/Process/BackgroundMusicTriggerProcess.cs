using Character;
using Framework.Common.Audio;
using Framework.Common.Trigger;
using Framework.Common.Trigger.Chain;
using UnityEngine;
using VContainer;

namespace Trigger.Process
{
    public class BackgroundMusicTriggerProcess : BaseTriggerProcess<CharacterObject>
    {
        [SerializeField] private AudioClip music;
        [SerializeField] private int priority;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Inject] private AudioManager _audioManager;

        private int _musicId;

        public override void EnterTriggerChain(CharacterObject target)
        {
            _musicId = -1;
            _musicId = _audioManager.AddBackgroundMusic(music, priority, volume);
        }

        public override void StayTriggerChain(CharacterObject target)
        {
        }

        public override void ExitTriggerChain(CharacterObject target)
        {
            _audioManager.RemoveBackgroundMusic(_musicId);
            _musicId = -1;
        }

        public override BaseTriggerLogic Clone(GameObject gameObject)
        {
            gameObject.name = "Background Music Trigger Process";
            var triggerProcess = gameObject.AddComponent<BackgroundMusicTriggerProcess>();
            triggerProcess.music = music;
            triggerProcess.priority = priority;
            triggerProcess.volume = volume;
            triggerProcess._audioManager = _audioManager;
            return triggerProcess;
        }
    }
}