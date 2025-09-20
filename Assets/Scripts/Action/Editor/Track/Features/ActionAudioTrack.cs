using System;
using System.Collections.Generic;
using Animancer;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Action.Editor.Track.Features
{
    public class ActionAudioTrackEditorData : ActionTrackFragmentEditorData
    {
        public ActionAudioType Type;
        public AudioClip AudioClip;
        public AudioClipRandomizer AudioClipRandomizer;
        public float Volume;

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionAudioTrackEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                StartTime = targetTick * tickTime,
                StartTick = targetTick,
                Duration = 1 * tickTime,
                DurationTicks = 1,
                Type = Type,
                AudioClip = AudioClip,
                AudioClipRandomizer = AudioClipRandomizer,
                Volume = Volume,
            };
        }
    }

    public class ActionAudioTrackInspectorSO : ActionTrackFragmentInspectorSO
    {
        [LabelText("音频类型"), Delayed, OnValueChanged("UpdateType")]
        public ActionAudioType type;

        [LabelText("音频片段"), Delayed, OnValueChanged("UpdateAudioClip")] [ShowIf("type", ActionAudioType.Specified)]
        public AudioClip audioClip;

        [LabelText("音频随机片段"), Delayed, OnValueChanged("UpdateAudioClipRandomizer")]
        [ShowIf("type", ActionAudioType.Random)]
        public AudioClipRandomizer audioClipRandomizer;

        [LabelText("音频音量"), Delayed, OnValueChanged("Update")]
        public float volume;

        private void UpdateType()
        {
            name = type switch
            {
                ActionAudioType.Specified => audioClip != null ? audioClip.name : "",
                ActionAudioType.Random => audioClipRandomizer != null ? audioClipRandomizer.name : "",
                _ => ""
            };
            Update();
        }

        private void UpdateAudioClip()
        {
            if (type == ActionAudioType.Specified)
            {
                name = audioClip != null ? audioClip.name : "";
            }

            Update();
        }

        private void UpdateAudioClipRandomizer()
        {
            if (type == ActionAudioType.Random)
            {
                name = audioClipRandomizer != null ? audioClipRandomizer.name : "";
            }

            Update();
        }
    }

    public class ActionAudioTrack : BaseActionTrack<ActionAudioTrackInspectorSO>
    {
        public override void Bind(ActionTrackEditorData data)
        {
            base.Bind(data);

            // 检查资源是否包含音频资源，不是则弹出错误提示
            if (data is ActionAudioTrackEditorData actionAudioTrackEditorData)
            {
                switch (actionAudioTrackEditorData.Type)
                {
                    case ActionAudioType.Specified:
                    {
                        if (!actionAudioTrackEditorData.AudioClip)
                        {
                            DebugUtil.LogWarning($"The track({this}) has no audio clip");
                        }
                    }
                        break;
                    case ActionAudioType.Random:
                    {
                        if (!actionAudioTrackEditorData.AudioClipRandomizer ||
                            actionAudioTrackEditorData.AudioClipRandomizer.audioClips.Count == 0)
                        {
                            DebugUtil.LogWarning($"The track({this}) has no audio clip");
                        }
                    }
                        break;
                }
            }
        }

        protected override void SynchronizeToInspector(ActionAudioTrackInspectorSO inspector)
        {
            base.SynchronizeToInspector(inspector);
            if (Data is ActionAudioTrackEditorData data)
            {
                inspector.type = data.Type;
                inspector.audioClip = data.AudioClip;
                inspector.audioClipRandomizer = data.AudioClipRandomizer;
                inspector.volume = data.Volume;
            }
        }

        protected override void SynchronizeToTrackData(ActionAudioTrackInspectorSO inspector)
        {
            base.SynchronizeToTrackData(inspector);
            if (Data is ActionAudioTrackEditorData data)
            {
                data.Type = inspector.type;
                data.AudioClip = inspector.audioClip;
                data.AudioClipRandomizer = inspector.audioClipRandomizer;
                data.Volume = inspector.volume;
            }
        }
    }
}