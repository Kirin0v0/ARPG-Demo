using System;

namespace Combo.Blackboard
{
    [Serializable]
    public class ComboBlackboardColliderGroupSetting
    {
        public string groupId;
        public float detectionInterval = 10f;
        public int detectionMaximum = 100;
    }
}