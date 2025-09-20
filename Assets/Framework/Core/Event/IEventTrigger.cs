namespace Framework.Core.Event
{
    public interface IEventTrigger
    {
        public void TriggerEvent(EventIdentity eventIdentity);
        public void TriggerEvent<T>(EventIdentity eventIdentity, T info);
    }
}