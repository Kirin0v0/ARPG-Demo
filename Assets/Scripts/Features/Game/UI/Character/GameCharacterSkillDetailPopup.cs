using System;
using System.Text;
using Character.Data;
using Features.Game.Data;
using Sirenix.OdinInspector;
using Skill.Unit;
using TMPro;
using UnityEngine;

namespace Features.Game.UI.Character
{
    public class GameCharacterSkillDetailPopup : BasePopup<GameCharacterSkillUIData>
    {
        [Title("UI关联")] [SerializeField] private TextMeshProUGUI textDetailName;
        [SerializeField] private TextMeshProUGUI textDetailIntroduction;
        [SerializeField] private TextMeshProUGUI textDetailCost;
        [SerializeField] private TextMeshProUGUI textDetailTarget;

        protected override void UpdateContent()
        {
            // 重置所有UI
            textDetailName.gameObject.SetActive(false);
            textDetailIntroduction.gameObject.SetActive(false);
            textDetailCost.gameObject.SetActive(false);
            textDetailTarget.gameObject.SetActive(false);

            if (Data == null)
            {
                return;
            }

            textDetailName.gameObject.SetActive(true);
            textDetailName.text = Data.Skill.flow.Name;
            textDetailIntroduction.gameObject.SetActive(true);
            textDetailIntroduction.text = Data.Skill.flow.Description;
            textDetailCost.gameObject.SetActive(true);
            textDetailCost.text = GetSkillCostString(Data.Skill.flow.Cost);
            textDetailTarget.gameObject.SetActive(true);
            textDetailTarget.text = GetSkillTargetString(Data.Skill.flow.TargetGroup);
        }

        private string GetSkillCostString(SkillCost cost)
        {
            var builder = new StringBuilder();

            if (cost.hp > 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                builder.Append("生命值 ").Append(cost.hp);
            }
            else if (cost.hp < 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                builder.Append("补偿生命值 ").Append(Mathf.Abs(cost.hp));
            }

            if (cost.mp > 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                builder.Append("法力值 ").Append(cost.mp);
            }
            else if (cost.mp < 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                builder.Append("补偿法力值 ").Append(Mathf.Abs(cost.mp));
            }

            if (cost.atb > 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                var atbText = IsInteger(cost.atb)
                    ? ((int)Mathf.Round(cost.atb)).ToString()
                    : cost.atb.ToString("F2");
                builder.Append("ATB ").Append(atbText).Append("格");
            }
            else if (cost.atb < 0)
            {
                if (builder.Length != 0)
                {
                    builder.Append("\n");
                }

                var atbText = IsInteger(cost.atb)
                    ? ((int)Mathf.Abs(Mathf.Round(cost.atb))).ToString()
                    : Mathf.Round(cost.atb).ToString("F2");
                builder.Append("补偿ATB ").Append(atbText).Append("格");
            }

            var costText = builder.ToString();
            return costText.Length != 0 ? $"使用消耗\n{costText}" : "";

            bool IsInteger(float number)
            {
                const float epsilon = 0.0001f; // 定义一个很小的误差范围
                return Math.Abs(number - (float)Math.Round(number)) < epsilon;
            }
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