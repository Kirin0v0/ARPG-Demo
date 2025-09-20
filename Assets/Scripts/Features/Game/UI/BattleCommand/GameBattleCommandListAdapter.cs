using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Player;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.BattleCommand
{
    public class GameBattleCommandListAdapter : RecyclerViewAdapter
    {
        private const int UnknownViewType = -1;
        private const int GroupViewType = 0;
        private const int ItemViewType = 1;

        [SerializeField] private GameBattleCommandListGroupViewHolder groupViewHolderTemplate;
        [SerializeField] private GameBattleCommandListItemViewHolder itemViewHolderTemplate;

        private readonly List<object> _dataList = new();

        private PlayerCharacterObject _player;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<object> _clickCallback;

        protected override IList GetData() => _dataList;

        public void Init(
            PlayerCharacterObject player,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<object> clickCallback
        )
        {
            _player = player;
            _navigationMoveCallback = navigationMoveCallback;
            _clickCallback = clickCallback;
        }

        public override int GetItemViewType(int position)
        {
            var data = _dataList[position];
            switch (data)
            {
                case GameBattleCommandGroupUIData groupData:
                    return GroupViewType;
                case GameBattleCommandItemUIData itemData:
                    return ItemViewType;
                default:
                    return UnknownViewType;
            }
        }

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType)
        {
            return viewType switch
            {
                GroupViewType => groupViewHolderTemplate,
                ItemViewType => itemViewHolderTemplate,
                _ => null,
            };
        }

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var recyclerViewHolder = GameObject.Instantiate(viewHolderTemplate);
            switch (recyclerViewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    groupViewHolder.Init();
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Init();
                }
                    break;
            }

            return recyclerViewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            switch (viewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    var groupData = (GameBattleCommandGroupUIData)_dataList[position];
                    groupViewHolder.Bind(groupData, _navigationMoveCallback, _clickCallback);
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    var itemData = (GameBattleCommandItemUIData)_dataList[position];
                    itemViewHolder.Bind(itemData, _player, _navigationMoveCallback, _clickCallback);
                }
                    break;
            }
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    groupViewHolder.Show();
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Show();
                }
                    break;
            }
        }

        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    groupViewHolder.Hide();
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Hide();
                }
                    break;
            }
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    groupViewHolder.Unbind();
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Unbind();
                }
                    break;
            }
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameBattleCommandListGroupViewHolder groupViewHolder:
                {
                    groupViewHolder.Destroy();
                }
                    break;
                case GameBattleCommandListItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Destroy();
                }
                    break;
            }
        }
    }
}