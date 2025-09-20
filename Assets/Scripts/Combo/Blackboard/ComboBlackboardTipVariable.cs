using System;
using System.Collections.Generic;
using Framework.Common.Blackboard;
using Inputs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combo.Blackboard
{
    [Serializable]
    public class ComboInputTip
    {
        public InputDeviceType deviceType;
        [FormerlySerializedAs("text")] public string prefixText;
        public Sprite image;
        [ShowInInspector] public string ImageName => image ? image.name : "";
        public string suffixText;
    }

    [Serializable]
    public class ComboBlackboardTipVariable : BlackboardVariable
    {
        public List<ComboInputTip> tips;
        public bool unnecessaryCondition;
    }
}