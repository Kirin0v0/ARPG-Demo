using UnityEngine;

namespace Features.Game.Data
{
    public enum GameNotificationThumbnailLoadType
    {
        Sprite,
        Atlas,
    }
    
    public class GameNotificationItemUIData
    {
        public GameNotificationThumbnailLoadType ThumbnailLoadType;
        public Sprite Thumbnail;
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public string Title;
        public string Description;
    }

    public class GameNotificationPlaceholderUIData
    {
    }
}