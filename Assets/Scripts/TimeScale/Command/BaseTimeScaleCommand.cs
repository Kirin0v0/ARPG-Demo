using Character;

namespace TimeScale.Command
{
    public abstract class BaseTimeScaleCommand
    {
        private readonly string _id;
        public string Id => _id;

        private readonly float _duration;
        public float Duration => _duration;

        public float Time;

        protected BaseTimeScaleCommand(string id, float duration)
        {
            _id = id;
            _duration = duration;
            Time = 0;
        }

        public abstract void Execute(ITimeScaleEdit edit, CharacterObject character);
    }
}