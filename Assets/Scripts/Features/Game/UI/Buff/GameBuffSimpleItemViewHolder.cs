using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.Buff
{
    public class GameBuffSimpleItemViewHolder: RecyclerViewHolder
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgMask;
        [SerializeField] private TextMeshProUGUI textStack;
        
        public void Bind(GameBuffSimpleUIData data)
        {
            imgIcon.sprite = data.Icon;
            imgMask.fillAmount = data.Permanent ? 0 : 1 - data.Duration / data.ExpectTime;
            textStack.text = data.MaxStack > 1 ? $"x{data.Stack}" : "";
        }

        public void Unbind()
        {
            imgIcon.sprite = null;
            imgMask.fillAmount = 0;
            textStack.text = "";
        }
    }
}