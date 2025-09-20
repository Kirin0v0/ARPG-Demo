using System;
using Character;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Camera.Data
{
    [Serializable]
    public struct CameraLockData
    {
        public bool @lock;
        [CanBeNull] public CharacterObject lockTarget;
    }
}