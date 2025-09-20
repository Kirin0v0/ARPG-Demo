using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Dialogue
{
    public class GameDialogueOptionListAdapter : RecyclerViewAdapter
    {
        [SerializeField] private GameDialogueOptionItemViewHolder template;

        private readonly List<GameDialogueOptionUIData> _dataList = new();

        public float GetItemHeight() => template.RectTransform.sizeDelta.y;

        protected override IList GetData() => _dataList;

        public override int GetItemViewType(int position) => 0;

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => template;

        public override Vector2 MeasureViewHolderTemplate(RecyclerViewHolder viewHolder, int position,
            Vector2 constraintSize)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Bind(_dataList[position]);
            return base.MeasureViewHolderTemplate(itemViewHolder, position, constraintSize);
        }

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var itemViewHolder = GameObject.Instantiate(viewHolderTemplate) as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Init();
            return itemViewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Bind(_dataList[position]);
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Show();
        }

        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Hide();
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Unbind();
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameDialogueOptionItemViewHolder;
            itemViewHolder!.Destroy();
        }
    }
}