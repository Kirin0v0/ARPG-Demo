using Character;

namespace TimeScale
{
    public interface ITimeScaleEdit
    {
        public float GetTimeScale(CharacterObject character);
        public void SetTimeScale(CharacterObject character, float timeScale);
        public void MultiplyTimeScale(CharacterObject character, float multiplier);
    }
}