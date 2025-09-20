using System.Collections.Generic;
using Animancer.TransitionLibraries;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.SO
{
    [CreateAssetMenu(menuName = "Character/Player/Common Config")]
    public class PlayerCharacterCommonConfigSO : SerializedScriptableObject
    {
        [Title("转身及旋转")] public float turn90StartAngle = 60;
        public float turn180StartAngle = 150;
        public float rotationFactorWhenUnlock = 5;
        public float rotationFactorWhenLock = 2;

        [Title("跳跃")] public float jumpHeight = 2f;
    }
}