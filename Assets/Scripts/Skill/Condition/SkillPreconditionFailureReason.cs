using System;

namespace Skill.Condition
{
    public abstract class BaseSkillPreconditionFailureReason
    {
        public abstract string Message { get; }
    }

    public static partial class SkillPreconditionFailureReasons
    {
        public static readonly BaseSkillPreconditionFailureReason Unknown = new SkillPreconditionUnknownFailureReason();
        public static readonly BaseSkillPreconditionFailureReason ResourceNotEnough = new SkillPreconditionResourceNotEnoughFailureReason();
        public static readonly BaseSkillPreconditionFailureReason SceneNotMatchLand = new SkillPreconditionSceneNotMatchLandFailureReason();
        public static readonly BaseSkillPreconditionFailureReason SceneNotMatchAirborne = new SkillPreconditionSceneNotMatchAirborneFailureReason();
    }

    public class SkillPreconditionUnknownFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "因未知原因无法释放该技能";
    }
    
    public class SkillPreconditionResourceNotEnoughFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "当前角色资源不足，无法释放该技能";
    }
    
    public class SkillPreconditionSceneNotMatchLandFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "该技能仅能在地面上释放";
    }
    
    public class SkillPreconditionSceneNotMatchAirborneFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "该技能仅能在空中释放";
    }
}