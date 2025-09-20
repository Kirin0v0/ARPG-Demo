using System;
using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;

namespace Features.Game.UI.Notification
{
    public class GameNotificationListAdapter : RecyclerViewAdapter
    {
        private const int ItemViewType = 0;
        private const int PlaceholderViewType = 1;
        
        [NonSerialized] public GameScene GameScene;
        
        [SerializeField] private GameNotificationItemViewHolder itemTemplate;
        [SerializeField] private GameNotificationPlaceholderViewHolder placeholderTemplate;

        private readonly List<object> _data = new();

        protected override IList GetData() => _data;

        public override int GetItemViewType(int position) => _data[position] switch
        {
            GameNotificationItemUIData itemData => ItemViewType,
            _ => PlaceholderViewType,
        };

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => viewType switch
        {
            ItemViewType => itemTemplate,
            _ => placeholderTemplate,
        };

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var viewHolder = GameObject.Instantiate(viewHolderTemplate);
            switch (viewHolder)
            {
                case GameNotificationItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Init(GameScene);
                }
                    break;
                case GameNotificationPlaceholderViewHolder placeholderViewHolder:
                {
                }
                    break;
            }
            return viewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            switch (viewHolder)
            {
                case GameNotificationItemViewHolder itemViewHolder:
                {
                    var itemData = (GameNotificationItemUIData)_data[position];
                    itemViewHolder.Bind(itemData);
                }
                    break;
                case GameNotificationPlaceholderViewHolder placeholderViewHolder:
                {
                }
                    break;
            }
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameNotificationItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Unbind();
                }
                    break;
                case GameNotificationPlaceholderViewHolder placeholderViewHolder:
                {
                }
                    break;
            }
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
        }
    }
}