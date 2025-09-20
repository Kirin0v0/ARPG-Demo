using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Character
{
    public class GameCharacterSkillListAdapter: RecyclerViewAdapter
    {
        [SerializeField] private GameCharacterSkillItemViewHolder viewHolderTemplate;
        
        private readonly List<GameCharacterSkillUIData> _data = new();
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        protected override IList GetData() => _data;

        public void Init(System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback)
        {
            _navigationMoveCallback = navigationMoveCallback;
        }

        public override int GetItemViewType(int position) => 0;

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => viewHolderTemplate;

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var itemViewHolder = Instantiate(viewHolderTemplate) as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Init();
            return itemViewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            var itemViewHolder = viewHolder as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Bind(_data[position], _navigationMoveCallback);
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Show();
        }
        
        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Hide();
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Unbind();
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            var itemViewHolder = viewHolder as GameCharacterSkillItemViewHolder;
            itemViewHolder!.Destroy();
        }
    }
}