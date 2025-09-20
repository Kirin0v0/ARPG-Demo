using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Unity.VisualScripting;
using UnityEngine;

namespace Features.Game.UI.Buff
{
    public class GameBuffSimpleListAdapter: RecyclerViewAdapter
    {
        [SerializeField] private GameBuffSimpleItemViewHolder viewHolderTemplate;
        
        private readonly List<GameBuffSimpleUIData> _buffs = new();

        protected override IList GetData() => _buffs;

        public override int GetItemViewType(int position) => 0;

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => viewHolderTemplate;

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var viewHolder = Instantiate(viewHolderTemplate);
            viewHolder.GetComponent<RectTransform>().localPosition = Vector3.zero;
            // viewHolder.GetComponent<RectTransform>().localScale = 0.1f * Vector3.zero;
            return viewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            var itemViewHolder = viewHolder as GameBuffSimpleItemViewHolder;
            itemViewHolder!.Bind(_buffs[position]);
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameBuffSimpleItemViewHolder;
            itemViewHolder!.Unbind();
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
        }
    }
}