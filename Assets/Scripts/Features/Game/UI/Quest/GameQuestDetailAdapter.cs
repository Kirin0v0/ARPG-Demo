using System;
using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using UnityEngine;

namespace Features.Game.UI.Quest
{
    public class GameQuestDetailAdapter: RecyclerViewAdapter
    {
        private const int HeaderType = 0;
        private const int StepType = 1;
        private const int GoalType = 2;
        private const int FooterType = 3;

        [SerializeField] private GameQuestDetailHeaderViewHolder headerViewHolderTemplate;
        [SerializeField] private GameQuestDetailStepViewHolder stepViewHolderTemplate;
        [SerializeField] private GameQuestDetailGoalViewHolder goalViewHolderTemplate;
        [SerializeField] private GameQuestDetailFooterViewHolder footerViewHolderTemplate;
        
        private readonly List<object> _data = new();

        protected override IList GetData() => _data;

        public override int GetItemViewType(int position)
        {
            switch (_data[position])
            {
                case GameQuestDetailHeaderUIData headerData:
                    return HeaderType;
                case GameQuestDetailStepUIData stepData:
                    return StepType;
                case GameQuestDetailGoalUIData goalData:
                    return GoalType;
                case GameQuestDetailFooterUIData footerData:
                    return FooterType;
                default:
                    throw new Exception($"The type of the data({_data[position]}) is not supported");
            }
        }

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType)
        {
            switch (viewType)
            {
                case HeaderType:
                    return headerViewHolderTemplate;
                case StepType:
                    return stepViewHolderTemplate;
                case GoalType:
                    return goalViewHolderTemplate;
                case FooterType:
                    return footerViewHolderTemplate;
                default:
                    throw new Exception($"The type of the viewType({viewType}) is not supported");
            }
        }

        public override Vector2 MeasureViewHolderTemplate(RecyclerViewHolder viewHolder, int position, Vector2 constraintSize)
        {
            switch (viewHolder)
            {
                case GameQuestDetailHeaderViewHolder headerViewHolder:
                {
                    headerViewHolder.BindData((GameQuestDetailHeaderUIData)_data[position]);
                }
                    break;
                case GameQuestDetailStepViewHolder stepViewHolder:
                {
                    stepViewHolder.BindData((GameQuestDetailStepUIData)_data[position]);
                }
                    break;
                case GameQuestDetailGoalViewHolder goalViewHolder:
                {
                    goalViewHolder.BindData((GameQuestDetailGoalUIData)_data[position]);
                }
                    break;
                case GameQuestDetailFooterViewHolder footerViewHolder:
                {
                    footerViewHolder.BindData((GameQuestDetailFooterUIData)_data[position]);
                }
                    break;
            }
            return base.MeasureViewHolderTemplate(viewHolder, position, constraintSize);
        }

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            return GameObject.Instantiate(viewHolderTemplate);
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            switch (viewHolder)
            {
                case GameQuestDetailHeaderViewHolder headerViewHolder:
                {
                    headerViewHolder.BindData((GameQuestDetailHeaderUIData)_data[position]);
                }
                    break;
                case GameQuestDetailStepViewHolder stepViewHolder:
                {
                    stepViewHolder.BindData((GameQuestDetailStepUIData)_data[position]);
                }
                    break;
                case GameQuestDetailGoalViewHolder goalViewHolder:
                {
                    goalViewHolder.BindData((GameQuestDetailGoalUIData)_data[position]);
                }
                    break;
                case GameQuestDetailFooterViewHolder footerViewHolder:
                {
                    footerViewHolder.BindData((GameQuestDetailFooterUIData)_data[position]);
                }
                    break;
            }
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GameQuestDetailHeaderViewHolder headerViewHolder:
                {
                    headerViewHolder.UnbindData();
                }
                    break;
                case GameQuestDetailStepViewHolder stepViewHolder:
                {
                    stepViewHolder.UnbindData();
                }
                    break;
                case GameQuestDetailGoalViewHolder goalViewHolder:
                {
                    goalViewHolder.UnbindData();
                }
                    break;
                case GameQuestDetailFooterViewHolder footerViewHolder:
                {
                    footerViewHolder.UnbindData();
                }
                    break;
            }
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
        }
    }
}