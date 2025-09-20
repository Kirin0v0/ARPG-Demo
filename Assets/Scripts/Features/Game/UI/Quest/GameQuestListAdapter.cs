using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Quest
{
    public class GameQuestListAdapter : RecyclerViewAdapter
    {
        [SerializeField] private GameQuestItemViewHolder itemTemplate;

        private readonly List<GameQuestItemUIData> _quests = new();
        private System.Action<RecyclerViewHolder> _navigationSelectedCallback;
        private System.Action<RecyclerViewHolder> _navigationDeselectedCallback;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        protected override IList GetData() => _quests;

        public void Init(
            System.Action<RecyclerViewHolder> navigationSelectedCallback,
            System.Action<RecyclerViewHolder> navigationDeselectedCallback,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            _navigationSelectedCallback = navigationSelectedCallback;
            _navigationDeselectedCallback = navigationDeselectedCallback;
            _navigationMoveCallback = navigationMoveCallback;
        }

        public override int GetItemViewType(int position) => 0;

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => itemTemplate;

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var questItemViewHolder = GameObject.Instantiate(viewHolderTemplate) as GameQuestItemViewHolder;
            questItemViewHolder!.Init();
            return questItemViewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            var questItemViewHolder = viewHolder as GameQuestItemViewHolder;
            questItemViewHolder!.Bind(_quests[position], _navigationSelectedCallback, _navigationDeselectedCallback,
                _navigationMoveCallback);
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            var questItemViewHolder = viewHolder as GameQuestItemViewHolder;
            questItemViewHolder!.Show();
        }

        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            var questItemViewHolder = viewHolder as GameQuestItemViewHolder;
            questItemViewHolder!.Hide();
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            var questItemViewHolder = viewHolder as GameQuestItemViewHolder;
            questItemViewHolder!.Unbind();
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            var questItemViewHolder = viewHolder as GameQuestItemViewHolder;
            questItemViewHolder!.Destroy();
        }
    }
}