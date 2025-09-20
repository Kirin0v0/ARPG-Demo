using System;
using UnityEngine;

namespace Events.Data
{
    public struct NotificationEventParameter
    {
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public string Title;
    }

    public struct NotificationGetEventParameter
    {
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public string Name;
        public int Number;
    }

    public struct NotificationLostEventParameter
    {
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public string Name;
        public int Number;
    }

    public struct NotificationNotGetMoreEventParameter
    {
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public string Name;
    }
}