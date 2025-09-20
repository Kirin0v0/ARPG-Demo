using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;

namespace Features.Game.UI.Quest
{
    public class GameQuestDetailFooterViewHolder:  RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textDescription;
        
        public void BindData(GameQuestDetailFooterUIData data)
        {
            if (string.IsNullOrEmpty(data.Description))
            {
                textDescription.text = "以上步骤已完成，请提交任务";
            }
            else
            {
                textDescription.text = data.Description;
            }
        }

        public void UnbindData()
        {
        }
    }
}