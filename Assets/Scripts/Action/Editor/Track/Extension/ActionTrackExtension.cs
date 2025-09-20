using System.Collections.Generic;
using System.Linq;
using Action.Editor.Channel;
using Framework.Common.Debug;
using UnityEngine;

namespace Action.Editor.Track.Extension
{
    public static class ActionTrackExtension
    {
        /// <summary>
        /// 解决轨道列表的数据冲突
        /// </summary>
        /// <param name="tracks"></param>
        public static void ResolveCollision(this List<ActionTrackEditorData> tracks)
        {
            if (tracks.Count <= 1)
            {
                return;
            }

            // 将轨道数据按照时间顺序排序
            tracks.Sort((track1, track2) =>
            {
                var track1UsedTickRange = track1.UsedTickRange;
                var track2UsedTickRange = track2.UsedTickRange;
                if (Mathf.Approximately(track1UsedTickRange.start, track2UsedTickRange.start))
                {
                    return track1UsedTickRange.end <= track2UsedTickRange.end ? -1 : 1;
                }

                return track1UsedTickRange.start < track2UsedTickRange.start ? -1 : 1;
            });

            // 使用双指针遍历，检查相邻轨道的时间重叠
            var index = 0;
            while (index < tracks.Count - 1)
            {
                var currentTrack = tracks[index];
                var nextTrack = tracks[index + 1];

                var currentRange = currentTrack.UsedTickRange;
                var nextRange = nextTrack.UsedTickRange;

                // 检查两个轨道的时间范围是否重叠
                if (currentRange.end > nextRange.start)
                {
                    // 发现重叠，删除后一个元素，并不增加i，继续检查新的下一个元素
                    DebugUtil.LogWarning(
                        $"The track({nextTrack.Name}) is removed due to the time collided with the track({currentTrack.Name})");
                    tracks.RemoveAt(index + 1);
                }
                else
                {
                    // 没有重叠，移动到下一个元素
                    index++;
                }
            }
        }

        /// <summary>
        /// 判断轨道列表是否存在时间冲突
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool HasTimeCollisionWithTarget(List<ActionTrackEditorData> tracks,
            ActionTrackEditorData target)
        {
            if (tracks.Count == 0)
            {
                return false;
            }

            var targetRange = target.UsedTickRange;
            foreach (var track in tracks)
            {
                if (track == target)
                {
                    continue;
                }

                var trackRange = track.UsedTickRange;
                if (trackRange.start < targetRange.end && trackRange.end > targetRange.start)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否允许将指定轨道移动到指定帧
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="targetTrack"></param>
        /// <param name="targetFrame"></param>
        /// <param name="totalFrames"></param>
        /// <returns></returns>
        public static bool AllowMoveToTargetFrame(
            List<ActionTrackEditorData> tracks,
            ActionTrackEditorData targetTrack,
            int targetFrame,
            int totalFrames
        )
        {
            if (totalFrames < 0)
            {
                return false;
            }

            // 这里临时修改轨道数据，后续会恢复数据
            var tempTick = targetTrack.PivotTick;
            targetTrack.MoveTo(targetFrame, ActionEditorWindow.Instance.FrameTimeUnit);

            // 检查目标轨道与其他轨道片段时间是否冲突
            if (HasTimeCollisionWithTarget(tracks, targetTrack))
            {
                targetTrack.MoveTo(tempTick, ActionEditorWindow.Instance.FrameTimeUnit);
                return false;
            }

            // 检查是否匹配目标轨道的限制策略
            if (!MatchTrackRestrictionStrategy(targetTrack, totalFrames))
            {
                targetTrack.MoveTo(tempTick, ActionEditorWindow.Instance.FrameTimeUnit);
                return false;
            }

            targetTrack.MoveTo(tempTick, ActionEditorWindow.Instance.FrameTimeUnit);
            return true;
        }

        /// <summary>
        /// 是否匹配轨道限制策略
        /// </summary>
        /// <param name="track"></param>
        /// <param name="totalFrames"></param>
        /// <returns></returns>
        public static bool MatchTrackRestrictionStrategy(this ActionTrackEditorData track, int totalFrames)
        {
            if (track.PivotTick < 0)
            {
                return false;
            }

            switch (track)
            {
                case ActionTrackPointEditorData point:
                {
                    switch (point.RestrictionStrategy)
                    {
                        case ActionTrackRestrictionStrategy.RestrictInTotalFrames:
                        {
                            if (point.PivotTick > totalFrames)
                            {
                                return false;
                            }
                        }
                            break;
                        case ActionTrackRestrictionStrategy.None:
                        {
                        }
                            break;
                    }
                }
                    break;
                case ActionTrackFragmentEditorData fragment:
                {
                    switch (fragment.RestrictionStrategy)
                    {
                        case ActionTrackRestrictionStrategy.RestrictInTotalFrames:
                        {
                            if (fragment.PivotTick + fragment.DurationTicks > totalFrames)
                            {
                                return false;
                            }
                        }
                            break;
                        case ActionTrackRestrictionStrategy.None:
                        {
                        }
                            break;
                    }
                }
                    break;
            }

            return true;
        }

        /// <summary>
        /// 检查数据是否脏了，是就修改数据并返回true
        /// </summary>
        /// <param name="track"></param>
        /// <param name="totalFrames"></param>
        /// <returns></returns>
        public static bool CheckWhetherDirty(this ActionTrackEditorData track, int totalFrames)
        {
            var result = false;

            if (track.PivotTick < 0) // 判断当前起始帧小于零，直接置零
            {
                track.MoveTo(0, ActionEditorWindow.Instance.FrameTimeUnit);
                result = true;
            }

            switch (track)
            {
                case ActionTrackPointEditorData point:
                {
                    switch (point.RestrictionStrategy)
                    {
                        case ActionTrackRestrictionStrategy.RestrictInTotalFrames:
                        {
                            if (point.PivotTick > totalFrames)
                            {
                                point.Time = totalFrames * ActionEditorWindow.Instance.FrameTimeUnit;
                                point.Tick = totalFrames;
                                result = true;
                            }
                        }
                            break;
                        case ActionTrackRestrictionStrategy.None:
                        {
                        }
                            break;
                    }
                }
                    break;
                case ActionTrackFragmentEditorData fragment:
                {
                    switch (fragment.RestrictionStrategy)
                    {
                        case ActionTrackRestrictionStrategy.RestrictInTotalFrames:
                        {
                            if (fragment.PivotTick + fragment.DurationTicks > totalFrames)
                            {
                                var durationTicks = fragment.DurationTicks <= totalFrames
                                    ? fragment.DurationTicks
                                    : totalFrames;
                                var startTick = totalFrames - durationTicks;
                                fragment.StartTime = startTick * ActionEditorWindow.Instance.FrameTimeUnit;
                                fragment.StartTick = startTick;
                                fragment.Duration = durationTicks * ActionEditorWindow.Instance.FrameTimeUnit;
                                fragment.DurationTicks = durationTicks;
                                result = true;
                            }
                        }
                            break;
                        case ActionTrackRestrictionStrategy.None:
                        {
                        }
                            break;
                    }
                }
                    break;
            }

            return result;
        }
    }
}