using System.Collections.Generic;
using System.Linq;
using Action.Editor.Channel.Extension;
using Action.Editor.Channel.UI;
using Action.Editor.Track;
using Action.Editor.Track.Extension;
using Framework.Common.Debug;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel
{
    public class BaseActionChannel<T, Inspector> : IActionChannel
        where T : IActionTrack, new()
        where Inspector : ActionChannelInspectorSO
    {
        public virtual IActionChannelUI ChannelUI { get; }
        public virtual IActionChannelUI TrackChannelUI { get; }

        public IActionChannelGroup Group { get; private set; }

        public IActionTrack[] Tracks => _tracks.ToArray();

        public ActionChannelEditorData Data { get; private set; }

        private readonly List<IActionTrack> _tracks = new();

        public virtual void Init([CanBeNull] IActionChannelGroup group, VisualElement channelParent,
            VisualElement trackChannelParent)
        {
            Group = group;
            ChannelUI.CreateView(channelParent);
            TrackChannelUI.CreateView(trackChannelParent);
            ChannelUI.SetClickChannelCallback(ToClickChannel);
            ChannelUI.SetDropdownMenu(ToShowDropdownMenu);
        }

        public virtual void Bind(ActionChannelEditorData data)
        {
            Data = data;
            ChannelUI.BindView(new ActionChannelEditorUIData
            {
                Color = data.Color,
                Name = data.Name,
                ShowMoreButton = data.ShowMoreButton,
            });
            TrackChannelUI.BindView(new ActionChannelEditorUIData
            {
                Color = data.Color,
                Name = data.Name,
                ShowMoreButton = data.ShowMoreButton,
            });

            // 如果当前激活是当前轨道窗口，就更新数据
            if (Selection.activeObject is ActionChannelInspectorSO inspector && inspector.IsBindToChannel(this))
            {
                Selection.activeObject = GetChannelInspector();
            }
        }

        public virtual void Destroy()
        {
            ChannelUI.SetDropdownMenu(null);
            ChannelUI.SetClickChannelCallback(null);
            UnbindAllTracks();
            ChannelUI.DestroyView();
            TrackChannelUI.DestroyView();
        }

        public void InitTracks(List<ActionTrackEditorData> trackDataList)
        {
            // 清除旧轨道数据
            foreach (var oldTrack in _tracks)
            {
                oldTrack.Destroy();
            }

            _tracks.Clear();

            // 解决轨道数据冲突
            trackDataList.ResolveCollision();

            // 创建未绑定数据的轨道
            foreach (var trackData in trackDataList)
            {
                var track = new T();
                BindTrack(track, trackData);
            }
        }
        
        public T CreateTrack()
        {
            var track = new T();
            return track;
        }

        public virtual void BindTrack(IActionTrack track, ActionTrackEditorData trackData)
        {
            if (track is not T)
            {
                return;
            }

            if (!_tracks.Contains(track))
            {
                track.Init(TrackChannelUI.Root, this, Data.TrackSupportAbilities);
                _tracks.Add(track);
            }

            track.Bind(trackData);
        }

        public virtual void UnbindTrack(IActionTrack track)
        {
            if (track is not T)
            {
                return;
            }

            if (!_tracks.Contains(track)) return;

            track.Destroy();
            _tracks.Remove(track);
        }

        public bool AllowMoveTrackToPosition(IActionTrack track, int targetFrame)
        {
            return this.AllowMoveToTargetFrame(track.Data, targetFrame, ActionEditorWindow.Instance.TotalFrames);
        }

        public bool TryMoveTrackToPosition(IActionTrack track, int targetFrame, out int moveToFrame)
        {
            moveToFrame = targetFrame;
            return this.AllowMoveToTargetFrame(track.Data, targetFrame, ActionEditorWindow.Instance.TotalFrames);
        }

        protected virtual GenericDropdownMenu ToShowDropdownMenu() => null;

        protected virtual void ToClickChannel()
        {
            Selection.activeObject = GetChannelInspector();
        }

        private ActionChannelInspectorSO GetChannelInspector()
        {
            var inspector = ScriptableObject.CreateInstance<Inspector>();
            inspector.Init(this, UpdateChannelData);
            SynchronizeToInspector(inspector);
            return inspector;
        }

        private void UpdateChannelData(ActionChannelInspectorSO inspector)
        {
            SynchronizeToTrackData(inspector as Inspector);
            Bind(Data);
        }

        protected virtual void SynchronizeToInspector(Inspector inspector)
        {
            inspector.id = Data.Id;
            inspector.name = Data.Name;
        }

        protected virtual void SynchronizeToTrackData(Inspector inspector)
        {
            Data.Id = inspector.id;
            Data.Name = inspector.name;
        }

        private void UnbindAllTracks()
        {
            for (var i = _tracks.Count - 1; i >= 0; i--)
            {
                var comboTrack = _tracks[i];
                comboTrack.Destroy();
                _tracks.Remove(comboTrack);
            }
        }
    }
}