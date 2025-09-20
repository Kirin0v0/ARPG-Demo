using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Action.Editor.Track.UI
{
    public struct ActionTrackEditorUIData
    {
        public string Name;
        public float StartFrame;
        public float DurationFrames;
        public Color NormalColor;
        public Color SelectedColor;
    }
}