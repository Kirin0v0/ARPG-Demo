using Framework.Common.Timeline.Data;

namespace Framework.Common.Timeline.Node
{
    public class TimelineDelegateNode: TimelineNode
    {
        public event System.Action<TimelineInfo> OnExecute;
        
        public TimelineDelegateNode(float timeElapsed, System.Action<TimelineInfo> executeDelegate = null) : base(timeElapsed)
        {
            if (executeDelegate != null)
            {
                OnExecute += executeDelegate;
            }
        }

        public override void Execute(TimelineInfo timelineInfo)
        {
            OnExecute?.Invoke(timelineInfo);
        }
    }
}