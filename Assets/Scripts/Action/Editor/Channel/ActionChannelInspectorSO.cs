using Sirenix.OdinInspector;
using UnityEngine;

namespace Action.Editor.Channel
{
    public class ActionChannelInspectorSO : ScriptableObject
    {
        [ReadOnly, LabelText("通道id")] public string id;

        [LabelText("通道名称"), Delayed, OnValueChanged("Update")]
        public new string name;

        private IActionChannel _bindChannel;
        private System.Action<ActionChannelInspectorSO> _updateCallback;

        public void Init(IActionChannel channel, System.Action<ActionChannelInspectorSO> updateCallback)
        {
            _bindChannel = channel;
            _updateCallback = updateCallback;
        }

        public bool IsBindToChannel(IActionChannel channel)
        {
            return _bindChannel == channel;
        }

        public void Update()
        {
            _updateCallback?.Invoke(this);
        }
    }
}