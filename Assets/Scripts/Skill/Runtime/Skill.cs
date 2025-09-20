using System;

namespace Skill.Runtime
{
    public enum SkillGroup
    {
        Static,
        Dynamic,
    }
    
    [Serializable]
    public class Skill
    {
        public string id; // 技能id
        public SkillFlow flow; // 技能流
        public SkillGroup group; // 技能组
        public bool releasing; // 技能是否正在释放
        public float cooldown = 0.1f; // 技能冷却时间
        public float time; // 技能当前时间，规定小等于0才能释放技能
    }
}