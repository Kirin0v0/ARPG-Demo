using System;
using System.Text;
using Features.Game.Data;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Skill.Unit;
using TMPro;
using UnityEngine;

namespace Features.Game.UI.BattleCommand
{
    public class GameBattleCommandDetailPopup : BasePopup<GameBattleCommandItemUIData>
    {
        [Title("UI关联")] [SerializeField] private TextMeshProUGUI textDetailName;
        [SerializeField] private TextMeshProUGUI textDetailIntroduction;
        [SerializeField] private TextMeshProUGUI textDetailTarget;

        protected override void UpdateContent()
        {
            // 重置所有UI
            textDetailName.gameObject.SetActive(false);
            textDetailIntroduction.gameObject.SetActive(false);

            if (Data == null)
            {
                return;
            }

            textDetailName.gameObject.SetActive(true);
            textDetailName.text = Data.Name;
            textDetailIntroduction.gameObject.SetActive(true);
            textDetailIntroduction.text = Data.Description.Length > 0 ? $"【{Data.Description}】" : "";
            textDetailTarget.gameObject.SetActive(true);
            textDetailTarget.text = GetSkillTargetString(Data.TargetGroup);
        }
        
        private string GetSkillTargetString(SkillTargetGroup targetGroup)
        {
            var builder = new StringBuilder();

            if ((targetGroup & SkillTargetGroup.Self) != 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("/");
                }

                builder.Append("自身");
            }

            if ((targetGroup & SkillTargetGroup.Ally) != 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("/");
                }

                builder.Append("友方");
            }

            if ((targetGroup & SkillTargetGroup.Enemy) != 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("/");
                }

                builder.Append("敌人");
            }

            var targetText = builder.ToString();
            return targetText.Length != 0 ? $"目标范围：{targetText}" : "无需目标";
        }
    }
}