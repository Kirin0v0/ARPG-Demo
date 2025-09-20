using UnityEngine;

namespace Events.Data
{
    public struct CutsceneEventParameter
    {
        public string Title;
        public AudioClip Audio;
        public float Duration;
        public System.Action OnFinished;
    }
}