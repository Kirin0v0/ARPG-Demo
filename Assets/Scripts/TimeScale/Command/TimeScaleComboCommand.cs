using Character;

namespace TimeScale.Command
{
    /// <summary>
    /// 连招时间缩放命令，面向全部角色，仅在目标时间缩放比当前时间缩放小的情况下生效，否则就不生效
    /// </summary>
    public class TimeScaleComboCommand : BaseTimeScaleCommand
    {
        private readonly float _targetTimeScale;

        public TimeScaleComboCommand(string id, float duration, float targetTimeScale) :
            base(id, duration)
        {
            _targetTimeScale = targetTimeScale;
        }

        public override void Execute(ITimeScaleEdit edit, CharacterObject character)
        {
            var timeScale = edit.GetTimeScale(character);
            if (_targetTimeScale >= timeScale)
            {
                return;
            }

            edit.SetTimeScale(character, _targetTimeScale);
        }
    }
}