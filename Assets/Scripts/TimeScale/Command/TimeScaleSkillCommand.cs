using Character;

namespace TimeScale.Command
{
    /// <summary>
    /// 技能时间缩放命令，对施法角色设置时间缩放，对其他角色设置另一个时间缩放
    /// </summary>
    public class TimeScaleSkillCommand : BaseTimeScaleCommand
    {
        private readonly float _casterTimeScale;
        private readonly float _othersTimeScale;
        private readonly CharacterObject _caster;

        public TimeScaleSkillCommand(string id, float duration, float casterTimeScale, float othersTimeScale,
            CharacterObject caster) : base(id, duration)
        {
            _casterTimeScale = casterTimeScale;
            _othersTimeScale = othersTimeScale;
            _caster = caster;
        }

        public override void Execute(ITimeScaleEdit edit, CharacterObject character)
        {
            edit.SetTimeScale(character, character == _caster ? _casterTimeScale : _othersTimeScale);
        }
    }
}