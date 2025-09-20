using System;
using System.Collections.Generic;
using Character;

namespace Skill.Unit.Feature
{
    public interface ISkillFlowFeaturePayloadsRequire
    {
        object[] GetPayloads(SkillFlowFeaturePayloadContext context);
    }
    
    public abstract class BaseSkillFlowFeaturePayloadsRequire: ISkillFlowFeaturePayloadsRequire
    {
        public abstract object[] GetPayloads(SkillFlowFeaturePayloadContext context);
    }

    public class SkillFlowFeatureNonPayloadsRequire : BaseSkillFlowFeaturePayloadsRequire
    {
        public SkillFlowFeaturePayloadContext ProvideContext()
        {
            return new SkillFlowFeaturePayloadContext();
        }

        public override object[] GetPayloads(SkillFlowFeaturePayloadContext context)
        {
            return Array.Empty<object>();
        }
    }

    public class SkillFlowFeatureCharactersPayloadsRequire : BaseSkillFlowFeaturePayloadsRequire
    {
        public SkillFlowFeaturePayloadContext ProvideContext(List<CharacterObject> characters)
        {
            var context = new SkillFlowFeaturePayloadContext();
            context.SetPayload("Characters", characters);
            return context;
        }

        public override object[] GetPayloads(SkillFlowFeaturePayloadContext context)
        {
            var payloads = new object[1];
            payloads[0] = context.GetPayload("Characters", out var characters)
                ? characters
                : new List<CharacterObject>();
            return payloads;
        }
    }
}