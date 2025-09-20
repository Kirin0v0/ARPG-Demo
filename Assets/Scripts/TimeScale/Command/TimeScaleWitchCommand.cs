using Character;

namespace TimeScale.Command
{
    /// <summary>
    /// 魔女时间缩放命令，对于非目标角色覆盖为迟缓的时间缩放，对于目标角色则覆盖为加速的时间缩放
    /// </summary>
    public class TimeScaleWitchCommand : BaseTimeScaleCommand
    {
        private readonly float _slowDownTimeScale;
        private readonly float _hurryUpTimeScale;
        private readonly CharacterObject _witch;

        public TimeScaleWitchCommand(string id, float duration, float slowDownTimeScale, float hurryUpTimeScale,
            CharacterObject witch) : base(id, duration)
        {
            _slowDownTimeScale = slowDownTimeScale;
            _hurryUpTimeScale = hurryUpTimeScale;
            _witch = witch;
        }

        public override void Execute(ITimeScaleEdit edit, CharacterObject character)
        {
            edit.SetTimeScale(character, character == _witch ? _hurryUpTimeScale : _slowDownTimeScale);
        }
    }
}