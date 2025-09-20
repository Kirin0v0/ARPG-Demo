namespace Framework.Core.Event
{
    /// <summary>
    /// 事件身份类，用于鉴别事件身份，不同系统事件可通过继承生产不同对象，避免跨系统的事件重名导致冲突
    /// </summary>
    public class EventIdentity
    {
        private readonly string _eventName;

        public EventIdentity(string eventName)
        {
            _eventName = eventName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventIdentity)obj);
        }

        public override int GetHashCode()
        {
            return (_eventName != null ? _eventName.GetHashCode() : 0);
        }

        private bool Equals(EventIdentity other)
        {
            return _eventName == other._eventName;
        }
    }
}