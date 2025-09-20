using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Core.Attribute;
using UnityEngine;

namespace Framework.Common.Audio
{
    [CreateAssetMenu(fileName = "Audio Clip Randomizer", menuName = "Audio/Audio Clip Randomizer")]
    public class AudioClipRandomizer : ScriptableObject
    {
        public List<AudioClip> audioClips;
        [DisplayOnly] public float averageLength;

        public AudioClip Random()
        {
            if (audioClips.Count == 0)
            {
                return null;
            }
            
            return audioClips[UnityEngine.Random.Range(0, audioClips.Count)];
        }

        private void OnValidate()
        {
            var length = 0f;
            foreach (var audioClip in audioClips)
            {
                length += audioClip.length;
            }

            averageLength = audioClips.Count == 0 ? 0f : length / audioClips.Count;
        }
    }
}