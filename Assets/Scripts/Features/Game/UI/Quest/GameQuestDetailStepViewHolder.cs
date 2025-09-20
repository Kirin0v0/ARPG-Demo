using System;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Quest.Config.Step;
using TMPro;
using UnityEngine;
using TextUtil = Framework.Common.Util.TextUtil;

namespace Features.Game.UI.Quest
{
    public class GameQuestDetailStepViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textDescription;
        [SerializeField] private TextMeshProUGUI textTip;

        public void BindData(GameQuestDetailStepUIData data)
        {
            var indexText = data.ShowIndex
                ? "<size=44><color=#ffffed87>" + TextUtil.ConvertToChineseNumeral(data.Index + 1) + "、</color></size>"
                : "";
            textDescription.text = indexText + data.Description;
            textTip.text = data.Relation switch
            {
                QuestStepGoalRelation.Linear => "逐步完成以下目标：",
                QuestStepGoalRelation.ParallelAnd => "完成以下目标：",
                QuestStepGoalRelation.ParallelOr => "完成以下任一目标：",
                _ => "完成以下目标："
            };
        }

        public void UnbindData()
        {
        }
    }
}