using Framework.Common.Timeline.Clip;
using Framework.Common.Timeline.Node;

namespace Framework.Common.Timeline
{
    /// <summary>
    /// 时间轴类，用于封装时间轴数据
    /// </summary>
    public class Timeline
    {
        public string Id; // 时间轴id
        public TimelineClip[] Clips; // 时间轴片段
        public TimelineNode[] Nodes; // 时间轴节点
        public float Duration; // 时间轴时长
    }
}