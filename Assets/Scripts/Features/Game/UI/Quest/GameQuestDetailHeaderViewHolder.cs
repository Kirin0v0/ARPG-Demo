using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.Quest
{
    public class GameQuestDetailHeaderViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textRequirement;
        [SerializeField] private TextMeshProUGUI textDescription;

        public void BindData(GameQuestDetailHeaderUIData data)
        {
            textTitle.text = data.Title;
            textDescription.text = data.Description;
            if (data.Requirements.Count == 0)
            {
                textRequirement.gameObject.SetActive(false);
                textRequirement.text = "";
            }
            else
            {
                textRequirement.gameObject.SetActive(true);
                textRequirement.text = "前置条件：" + string.Join("/", data.Requirements);
            }
        }

        public void UnbindData()
        {
        }
    }
}