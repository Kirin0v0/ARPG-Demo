using UnityEngine;

namespace Features.Game.Data
{
    public class GameCutsceneUIData
    {
        public string Title;
        public AudioClip Audio;
        public float Duration;
        public System.Action OnFinished;
    }
}