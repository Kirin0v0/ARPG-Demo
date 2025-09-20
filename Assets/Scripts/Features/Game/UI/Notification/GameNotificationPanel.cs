using System.Collections;
using System.Collections.Generic;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using UnityEngine;
using VContainer;

namespace Features.Game.UI.Notification
{
    public class GameNotificationPanel : BaseUGUIPanel
    {
        [SerializeField] private int showNumber = 2;
        [SerializeField] private float scrollInterval = 2f;

        private RecyclerView _rvNotificationList;
        [SerializeField] private RecyclerViewLayoutManager layoutManager;
        [SerializeField] private GameNotificationListAdapter adapter;

        [Inject] private GameScene _gameScene;

        private GameNotificationModel _gameNotificationModel;
        private float _scrollCountdown;

        protected override void OnInit()
        {
            _rvNotificationList = GetWidget<RecyclerView>("RvNotificationList");
            _rvNotificationList.Init();
            _rvNotificationList.LayoutManager = layoutManager;
            _rvNotificationList.Adapter = adapter;
        }

        protected override void OnShow(object payload)
        {
            adapter.GameScene = _gameScene;
            _gameNotificationModel = new(showNumber);
            _gameNotificationModel.GetNotifications().ObserveForever(HandleNotificationListUpdate);
            _scrollCountdown = scrollInterval;
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 更新通知
            _gameNotificationModel.UpdateNewestNotifications();
            // 如果到了滚动时间则滚动
            if (_scrollCountdown <= 0)
            {
                _gameNotificationModel.MakeNotificationScrollUp();
                _scrollCountdown = scrollInterval;
            }
            else
            {
                _scrollCountdown -= Time.deltaTime;
            }
        }

        protected override void OnHide()
        {
            _gameNotificationModel.GetNotifications().RemoveObserver(HandleNotificationListUpdate);

            _gameNotificationModel.Destroy();
            _gameNotificationModel = null;
        }

        private void HandleNotificationListUpdate(List<object> list)
        {
            adapter.SetData(list);
            if (list.Count > 0)
            {
                _rvNotificationList.FocusItem(list.Count - 1, true);
                _scrollCountdown = scrollInterval;
            }
        }
    }
}