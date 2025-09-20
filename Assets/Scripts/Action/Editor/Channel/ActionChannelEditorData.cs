using System;
using System.Collections.Generic;
using Action.Editor.Track;
using Action.Editor.Track.UI;
using Framework.Common.Debug;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Action.Editor.Channel
{
    [Flags]
    public enum ActionChannelTrackSupportAbility
    {
        None = 0,
        MoveToSelectedFrame = 1 << 0,
        DeleteTrack = 1 << 1,
        CopyToSelectedFrame = 1 << 2,
    }

    public class ActionChannelEditorData
    {
        public string Id;
        public string Name;
        public Color Color;
        public bool ShowMoreButton;

        public Color TrackNormalColor;
        public Color TrackSelectedColor;
        public ActionChannelTrackSupportAbility TrackSupportAbilities;
    }
}