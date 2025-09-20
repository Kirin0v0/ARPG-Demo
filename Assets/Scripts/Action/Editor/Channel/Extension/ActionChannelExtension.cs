using System.Linq;
using Action.Editor.Track;
using Action.Editor.Track.Extension;

namespace Action.Editor.Channel.Extension
{
    public static class ActionChannelExtension
    {
        /// <summary>
        /// 判断通道是否存在时间冲突
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool HasTimeCollisionWithTarget(this IActionChannel channel, ActionTrackEditorData target)
        {
            return ActionTrackExtension.HasTimeCollisionWithTarget(channel.Tracks.Select(track => track.Data).ToList(), target);
        }

        /// <summary>
        /// 是否允许将指定轨道移动到指定帧
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="targetTrack"></param>
        /// <param name="targetFrame"></param>
        /// <param name="totalFrames"></param>
        /// <returns></returns>
        public static bool AllowMoveToTargetFrame(
            this IActionChannel channel,
            ActionTrackEditorData targetTrack,
            int targetFrame,
            int totalFrames
        )
        {
            return ActionTrackExtension.AllowMoveToTargetFrame(
                channel.Tracks.Select(track => track.Data).ToList(),
                targetTrack,
                targetFrame,
                totalFrames
            );
        }
    }
}