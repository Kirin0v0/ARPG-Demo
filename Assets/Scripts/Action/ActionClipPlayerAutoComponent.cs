using System;
using Action;
using Character;
using Features;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.Function;
using UnityEngine;
using UnityEngine.Serialization;

namespace Action
{
    public class ActionClipPlayerAutoComponent : MonoBehaviour
    {
        public ActionClip actionClip;
        public CharacterObject character;

        private ActionClipPlayer _actionClipPlayer;

        private void Awake()
        {
            _actionClipPlayer = new ActionClipPlayer(
                actionClip,
                character,
                -1,
                null
            );
        }

        private void OnEnable()
        {
            _actionClipPlayer.Start();
        }

        private void Update()
        {
            _actionClipPlayer.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            _actionClipPlayer.Stop();
        }
    }
}