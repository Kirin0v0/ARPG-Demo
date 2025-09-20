using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Trade
{
    public class GameTradeGoodsListAdapter : RecyclerViewAdapter
    {
        [SerializeField] private GameTradeGoodsItemViewHolder itemTemplate;

        private const int ItemViewType = 0;

        private readonly List<GameTradeGoodsUIData> _data = new();
        private System.Action<RecyclerViewHolder> _decreaseTargetNumberCallback;
        private System.Action<RecyclerViewHolder> _increaseTargetNumberCallback;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        protected override IList GetData() => _data;

        public void Init(System.Action<RecyclerViewHolder> decreaseTargetNumberCallback,
            System.Action<RecyclerViewHolder> increaseTargetNumberCallback,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback)
        {
            _decreaseTargetNumberCallback = decreaseTargetNumberCallback;
            _increaseTargetNumberCallback = increaseTargetNumberCallback;
            _navigationMoveCallback = navigationMoveCallback;
        }

        public override int GetItemViewType(int position)
        {
            return ItemViewType;
        }

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType)
        {
            return itemTemplate;
        }

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var itemViewHolder = GameObject.Instantiate(viewHolderTemplate) as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Init();
            return itemViewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            var itemViewHolder = viewHolder as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Bind(_data[position], _decreaseTargetNumberCallback, _increaseTargetNumberCallback,
                _navigationMoveCallback);
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Show();
        }
        
        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Hide();
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Unbind();
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameTradeGoodsItemViewHolder;
            itemViewHolder!.Destroy();
        }
    }
}