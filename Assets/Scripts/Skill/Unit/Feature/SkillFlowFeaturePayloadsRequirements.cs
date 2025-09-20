namespace Skill.Unit.Feature
{
    public static class SkillFlowFeaturePayloadsRequirements
    {
        public static BaseSkillFlowFeaturePayloadsRequire EmptyPayloads = new SkillFlowFeatureNonPayloadsRequire();

        public static BaseSkillFlowFeaturePayloadsRequire CharactersPayloads =
            new SkillFlowFeatureCharactersPayloadsRequire();
    }
}