using System.Collections.Generic;
using System.Linq;
using Events;
using Events.Data;
using Features.Game.Data;
using Framework.Core.LiveData;

namespace Features.Game.UI.Notification
{
    public class GameNotificationModel
    {
        private readonly int _minNumber;

        private readonly List<GameNotificationItemUIData> _newNotifications = new();

        private readonly MutableLiveData<List<object>> _notifications = new(new List<object>());
        public LiveData<List<object>> GetNotifications() => _notifications;

        public GameNotificationModel(int minNumber)
        {
            _minNumber = minNumber;
            GameApplication.Instance.EventCenter.AddEventListener<NotificationEventParameter>(GameEvents.Notification,
                HandleNotificationEvent);
            GameApplication.Instance.EventCenter.AddEventListener<NotificationGetEventParameter>(
                GameEvents.NotificationGet,
                HandleNotificationGetEvent);
            GameApplication.Instance.EventCenter.AddEventListener<NotificationLostEventParameter>(
                GameEvents.NotificationLost,
                HandleNotificationLostEvent);
            GameApplication.Instance.EventCenter.AddEventListener<NotificationNotGetMoreEventParameter>(
                GameEvents.NotificationNotGetMore,
                HandleNotificationNotGetMoreEvent);
        }

        public void UpdateNewestNotifications()
        {
            if (_newNotifications.Count == 0)
            {
                return;
            }

            var newestList = new LinkedList<object>();
            foreach (var oldNotification in _notifications.Value)
            {
                newestList.AddLast(oldNotification);
            }

            foreach (var newNotification in _newNotifications)
            {
                newestList.AddLast(newNotification);
            }

            while (newestList.Count < _minNumber)
            {
                newestList.AddFirst(new GamePackagePlaceholderUIData());
            }

            _newNotifications.Clear();
            _notifications.SetValue(newestList.ToList());
        }

        public void MakeNotificationScrollUp()
        {
            if (_notifications.Value.Count == 0)
            {
                return;
            }
            
            // 如果通知展示元素全是占位符，我们就直接清空通知，避免多余的性能占用在UI上
            if (_notifications.Value.Count >= _minNumber)
            {
                if (_notifications.Value.Skip(_notifications.Value.Count - _minNumber).All(x => x is GamePackagePlaceholderUIData))
                {
                    _notifications.SetValue(new List<object>());
                    return;
                }
            }

            var newestList = new List<object>(_notifications.Value);
            newestList.Add(new GamePackagePlaceholderUIData());
            _notifications.SetValue(newestList);
        }

        public void Destroy()
        {
            GameApplication.Instance?.EventCenter.RemoveEventListener<NotificationEventParameter>(
                GameEvents.Notification,
                HandleNotificationEvent);
            GameApplication.Instance?.EventCenter.RemoveEventListener<NotificationGetEventParameter>(
                GameEvents.NotificationGet,
                HandleNotificationGetEvent);
            GameApplication.Instance?.EventCenter.RemoveEventListener<NotificationLostEventParameter>(
                GameEvents.NotificationLost,
                HandleNotificationLostEvent);
            GameApplication.Instance?.EventCenter.RemoveEventListener<NotificationNotGetMoreEventParameter>(
                GameEvents.NotificationNotGetMore,
                HandleNotificationNotGetMoreEvent);
        }

        private void HandleNotificationEvent(NotificationEventParameter parameter)
        {
            AddNewNotification(new GameNotificationItemUIData
            {
                ThumbnailLoadType = GameNotificationThumbnailLoadType.Atlas,
                ThumbnailAtlas = parameter.ThumbnailAtlas,
                ThumbnailName = parameter.ThumbnailName,
                Title = parameter.Title,
            });
        }

        private void HandleNotificationGetEvent(NotificationGetEventParameter parameter)
        {
            AddNewNotification(new GameNotificationItemUIData
            {
                ThumbnailLoadType = GameNotificationThumbnailLoadType.Atlas,
                ThumbnailAtlas = parameter.ThumbnailAtlas,
                ThumbnailName = parameter.ThumbnailName,
                Title = parameter.Name,
                Description = $"获得{parameter.Number}个"
            });
        }

        private void HandleNotificationLostEvent(NotificationLostEventParameter parameter)
        {
            AddNewNotification(new GameNotificationItemUIData
            {
                ThumbnailLoadType = GameNotificationThumbnailLoadType.Atlas,
                ThumbnailAtlas = parameter.ThumbnailAtlas,
                ThumbnailName = parameter.ThumbnailName,
                Title = parameter.Name,
                Description = $"失去{parameter.Number}个"
            });
        }

        private void HandleNotificationNotGetMoreEvent(NotificationNotGetMoreEventParameter parameter)
        {
            AddNewNotification(new GameNotificationItemUIData
            {
                ThumbnailLoadType = GameNotificationThumbnailLoadType.Atlas,
                ThumbnailAtlas = parameter.ThumbnailAtlas,
                ThumbnailName = parameter.ThumbnailName,
                Title = parameter.Name,
                Description = $"无法获得更多"
            });
        }

        private void AddNewNotification(GameNotificationItemUIData data)
        {
            _newNotifications.Add(data);
        }
    }
}