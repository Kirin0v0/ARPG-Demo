using System.Collections;
using System.Collections.Generic;
using Archive;
using Features.Main.Data;
using Features.SceneGoto;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Main.Archive
{
    public class MainArchiveListAdapter : RecyclerViewAdapter
    {
        private const int ItemViewType = 0;
        private const int PlaceHolderViewType = 1;

        [SerializeField] private MainArchiveItemViewHolder itemTemplate;
        [SerializeField] private MainArchivePlaceholderViewHolder placeholderTemplate;

        private readonly List<object> _data = new();

        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<MainArchiveItemUIData> _clickCallback;
        private System.Action<MainArchiveItemUIData> _deleteCallback;

        protected override IList GetData() => _data;

        public void Init(
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<MainArchiveItemUIData> clickCallback,
            System.Action<MainArchiveItemUIData> deleteCallback
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
                MainArchiveItemUIData => ItemViewType,
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
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Init();
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
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
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    var itemData = (MainArchiveItemUIData)_data[position];
                    itemViewHolder.Bind(
                        itemData,
                        _navigationMoveCallback,
                        _clickCallback,
                        _deleteCallback
                    );
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    var placeholderData = (MainArchivePlaceholderUIData)_data[position];
                    placeholderViewHolder.Bind(placeholderData, _navigationMoveCallback);
                }
                    break;
            }
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Show();
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
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
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Hide();
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
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
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Unbind();
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
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
                case MainArchiveItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Destroy();
                }
                    break;
                case MainArchivePlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Destroy();
                }
                    break;
            }
        }
    }
}