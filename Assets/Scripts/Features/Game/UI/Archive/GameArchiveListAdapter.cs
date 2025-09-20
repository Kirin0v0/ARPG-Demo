using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Archive
{
    public class GameArchiveListAdapter : RecyclerViewAdapter
    {
        private const int ItemViewType = 0;
        private const int PlaceHolderViewType = 1;

        [SerializeField] private GameArchiveItemViewHolder itemTemplate;
        [SerializeField] private GameArchivePlaceholderViewHolder placeholderTemplate;

        private readonly List<object> _data = new();

        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<GameArchiveItemUIData> _clickCallback;
        private System.Action<GameArchiveItemUIData> _deleteCallback;

        protected override IList GetData() => _data;

        public void Init(
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<GameArchiveItemUIData> clickCallback,
            System.Action<GameArchiveItemUIData> deleteCallback
        )
        {
            _navigationMoveCallback = navigationMoveCallback;
            _clickCallback = clickCallback;
            _deleteCallback = deleteCallback;
        }

        public override int GetItemViewType(int position)
        {
            return _data[position] switch
            {
                GameArchiveItemUIData => ItemViewType,
                _ => PlaceHolderViewType,
            };
        }

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType)
        {
            return viewType switch
            {
                ItemViewType => itemTemplate,
                _ => placeholderTemplate,
            };
        }

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var viewHolder = GameObject.Instantiate(viewHolderTemplate);
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Init();
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Init();
                }
                    break;
            }
            return viewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    var itemData = (GameArchiveItemUIData)_data[position];
                    itemViewHolder.Bind(
                        itemData,
                        _navigationMoveCallback,
                        _clickCallback,
                        _deleteCallback
                    );
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    var placeholderData = (GameArchivePlaceholderUIData)_data[position];
                    placeholderViewHolder.Bind(placeholderData, _navigationMoveCallback);
                }
                    break;
            }
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Show();
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Show();
                }
                    break;
            }
        }

        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Hide();
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Hide();
                }
                    break;
            }
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Unbind();
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Unbind();
                }
                    break;
            }
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Destroy();
                }
                    break;
                case GameArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Destroy();
                }
                    break;
            }
        }
    }
}