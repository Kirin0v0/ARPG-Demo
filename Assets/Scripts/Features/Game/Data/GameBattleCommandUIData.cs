using System;
using System.Collections.Generic;
using System.Text;
using Character.Ability;
using Character.Data;
using Skill;
using Skill.Runtime;
using Skill.Unit;
using UnityEngine;

namespace Features.Game.Data
{
    public abstract class GameBattleCommandPageData
    {
    }

    public class GameBattleCommandCollapsePageData : GameBattleCommandPageData
    {
    }

    public abstract class GameBattleCommandExpandPageData : GameBattleCommandPageData
    {
        public string Title;
    }

    public class GameBattleCommandExpandGroupPageData : GameBattleCommandExpandPageData
    {
        public List<GameBattleCommandGroupUIData> Groups;
    }

    public class GameBattleCommandExpandItemPageData : GameBattleCommandExpandPageData
    {
        public List<GameBattleCommandItemUIData> Items;
    }

    public class GameBattleCommandHiddenPageData : GameBattleCommandPageData
    {
    }

    public class GameBattleCommandGroupUIData
    {
        public string Name;
        public List<Skill.Runtime.Skill> Skills;
        public bool Enable;
        public bool Selected;
    }

    public class GameBattleCommandItemUIData
    {
        public SkillGroup SkillGroup;
        public Skill.Runtime.Skill Skill;
        public bool Selected;

        public string Name => Skill.flow.Name;
        public string Description => Skill.flow.Description;
        public bool NeedTarget => Skill.flow.NeedTarget;
        public SkillTargetGroup TargetGroup => Skill.flow.TargetGroup;

        public string Cost
        {
            get
            {
                var cost = Skill.flow.Cost;
                var builder = new StringBuilder();

                if (cost.hp > 0)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(" + ");
                    }

                    builder.Append("生命值 ").Append(cost.hp);
                }
                else if (cost.hp < 0)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(" + ");
                    }

                    builder.Append("补偿生命值 ").Append(Mathf.Abs(cost.hp));
                }

                if (cost.mp > 0)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(" + ");
                    }

                    builder.Append("法力值 ").Append(cost.mp);
                }
                else if (cost.mp < 0)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(" + ");
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
                return costText;

                bool IsInteger(float number)
                {
                    const float epsilon = 0.0001f; // 定义一个很小的误差范围
                    return Math.Abs(number - (float)Math.Round(number)) < epsilon;
                }
            }
        }
    }
}