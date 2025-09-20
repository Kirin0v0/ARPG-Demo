using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Action.Editor.Track
{
    public abstract class ActionTrackInspectorSO : SerializedScriptableObject
    {
        [ReadOnly, LabelText("轨道名称")] public new string name;

        private IActionTrack _bindTrack;
        private System.Action<ActionTrackInspectorSO> _updateCallback;

        public void Init(IActionTrack track, System.Action<ActionTrackInspectorSO> updateCallback)
        {
            _bindTrack = track;
            _updateCallback = updateCallback;
        }

        public bool IsBindToTrack(IActionTrack track)
        {
            return _bindTrack == track;
        }

        public void Update()
        {
            _updateCallback?.Invoke(this);
        }
    }

    public class ActionTrackPointInspectorSO : ActionTrackInspectorSO
    {
        [LabelText("轨道帧"), Delayed, OnValueChanged("UpdateTick")]
        public int tick;

        [ReadOnly, LabelText("轨道时间点")] public float time;

        private void UpdateTick()
        {
            time = tick * ActionEditorWindow.Instance.FrameTimeUnit;
            Update();
        }
    }

    public class ActionTrackFragmentInspectorSO : ActionTrackInspectorSO
    {
        [LabelText("轨道开始帧"), Delayed, OnValueChanged("UpdateStartTick")]
        public int startTick;

        [ReadOnly, LabelText("轨道开始时间点")] public float startTime;

        [LabelText("轨道持续帧数"), Delayed, OnValueChanged("UpdateDurationTicks")]
        public int durationTicks;

        [ReadOnly, LabelText("轨道持续时间")] public float duration;

        private void UpdateStartTick()
        {
            startTime = startTick * ActionEditorWindow.Instance.FrameTimeUnit;
            Update();
        }

        private void UpdateDurationTicks()
        {
            duration = durationTicks * ActionEditorWindow.Instance.FrameTimeUnit;
            Update();
        }
    }
}