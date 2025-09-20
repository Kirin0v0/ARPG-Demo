using Character;

namespace TimeScale.Command
{
    /// <summary>
    /// 单个时间缩放命令，面向某个角色，与角色身上的时间缩放进行乘算
    /// </summary>
    public class TimeScaleSingleCommand : BaseTimeScaleCommand
    {
        private readonly float _targetTimeScale;
        private readonly CharacterObject _target;

        public TimeScaleSingleCommand(string id, float duration, float targetTimeScale, CharacterObject target) : base(
            id, duration)
        {
            _targetTimeScale = targetTimeScale;
            _target = target;
        }

        public override void Execute(ITimeScaleEdit edit, CharacterObject character)
        {
            if (character != _target) return;
            var timeScale = edit.GetTimeScale(character);
            edit.SetTimeScale(character, timeScale * _targetTimeScale);
        }
    }
}