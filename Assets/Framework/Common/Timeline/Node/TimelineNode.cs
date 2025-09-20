using Framework.Common.Timeline.Data;

namespace Framework.Common.Timeline.Node
{
    /// <summary>
    /// 时间轴节点类，之所以是抽象类，是为了支持后续自定义参数
    /// </summary>
    public abstract class TimelineNode
    {
        public readonly float TimeElapsed; // 节点位于时间轴时间位置

        protected TimelineNode(float timeElapsed)
        {
            TimeElapsed = timeElapsed;
        }

        /// <summary>
        /// 节点执行函数，在时间轴执行到对应位置后执行该函数
        /// </summary>
        /// <param name="timelineInfo"></param>
        public abstract void Execute(TimelineInfo timelineInfo);
    }
}