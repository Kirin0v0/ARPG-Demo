namespace Skill.Condition
{
    public abstract class BaseSkillPostconditionFailureReason
    {
        public abstract string Message { get; }
    }

    public static partial class SkillPostconditionFailureReasons
    {
        public static readonly BaseSkillPostconditionFailureReason Unknown = new SkillPostconditionUnknownFailureReason();
        public static readonly BaseSkillPostconditionFailureReason NoTarget = new SkillPostconditionNoTargetFailureReason();
    }
    
    public class SkillPostconditionUnknownFailureReason : BaseSkillPostconditionFailureReason
    {
        public override string Message => "因未知原因无法释放该技能";
    }
    
    public class SkillPostconditionNoTargetFailureReason : BaseSkillPostconditionFailureReason
    {
        public override string Message => "丢失技能目标，无法释放该技能";
    }
}