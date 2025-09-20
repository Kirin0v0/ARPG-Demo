using Framework.Common.Timeline.Data;

namespace Framework.Common.Timeline.Clip
{
    public class TimelineDelegateClip : TimelineClip
    {
        public event System.Action<TimelineClip, TimelineInfo> StartEvent;
        public event System.Action<TimelineClip, TimelineInfo> TickEvent;
        public event System.Action<TimelineClip, TimelineInfo> StopEvent;

        public TimelineDelegateClip(
            float startTime,
            int totalTicks,
            float tickTime,
            System.Action<TimelineClip, TimelineInfo> startDelegate = null,
            System.Action<TimelineClip, TimelineInfo> tickDelegate = null,
            System.Action<TimelineClip, TimelineInfo> stopDelegate = null
        ) : base(startTime, totalTicks,
            tickTime)
        {
            if (startDelegate != null)
            {
                StartEvent += startDelegate;
            }

            if (tickDelegate != null)
            {
                TickEvent += tickDelegate;
            }

            if (stopDelegate != null)
            {
                StopEvent += stopDelegate;
            }
        }

        protected override void OnStart(TimelineInfo timelineInfo)
        {
            StartEvent?.Invoke(this, timelineInfo);
        }

        protected override void OnTick(TimelineInfo timelineInfo)
        {
            TickEvent?.Invoke(this, timelineInfo);
        }

        protected override void OnStop(TimelineInfo timelineInfo)
        {
            StopEvent?.Invoke(this, timelineInfo);
        }
    }
}