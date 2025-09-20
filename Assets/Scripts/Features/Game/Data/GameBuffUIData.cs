using UnityEngine;

namespace Features.Game.Data
{
    public class GameBuffSimpleUIData
    {
        public string Id; 
        public string Name; 
        public Sprite Icon;
        public bool Permanent;
        public float ExpectTime;
        public float Duration;
        public int MaxStack;
        public int Stack;
    }

    public class GameBuffDetailUIData
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public string Description;
        public bool Permanent;
        public float Duration;
        public int MaxStack;
        public int Stack;
        public string CasterName;
        public bool Focused;
    }

    public static class GameBuffUIDataExtension
    {
        public static GameBuffSimpleUIData ToSimpleUIData(this Buff.Runtime.Buff buff)
        {
            return new GameBuffSimpleUIData
            {
                Id = buff.info.id,
                Name = buff.info.name,
                Icon = buff.info.icon,
                Permanent = buff.permanent,
                ExpectTime = buff.expectTime,
                Duration = buff.duration,
                MaxStack = buff.info.maxStack,
                Stack = buff.stack
            };
        }
    } 
}