using System;
using System.Collections.Generic;
using Inputs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.StateMachine.Base
{
    public enum PlayerStateTransitionOperatorType
    {
        And,
        Or,
    }

    [Serializable]
    public class PlayerStateTransition
    {
        public string name = "";
        public List<PlayerStateTransitionOperatorTip> operatorTips = new();
        public List<string> replaceTransitionNames = new();
        public bool commonTransition = true;
    }

    [Serializable]
    public class PlayerStateTransitionOperatorTip
    {
        public PlayerStateTransitionOperatorType operatorType = PlayerStateTransitionOperatorType.And;
        public List<PlayerStateTransitionTip> tips = new();
    }

    [Serializable]
    public class PlayerStateTransitionTip
    {
        public InputDeviceType deviceType;
        [FormerlySerializedAs("text")] public string prefixText;
        public Sprite image;
        [ShowInInspector] public string ImageName => image ? image.name : "";
        public string suffixText;
    }
}