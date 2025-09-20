using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using UnityEngine;

namespace Framework.Common.Timeline
{
    /// <summary>
    /// 时间轴管理器，负责执行时间轴上的节点和片段
    /// </summary>
    public class TimelineManager : MonoBehaviour
    {
        private readonly List<TimelineInfo> _timelines = new();

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            _timelines.Clear();
        }

        private void Tick(float deltaTime)
        {
            if (_timelines.Count == 0)
            {
                return;
            }

            var index = 0;
            while (index < _timelines.Count)
            {
                var timelineInfo = _timelines[index];

                // 如果时间轴施法者销毁或时间轴被停止，此时认为是时间轴停止
                if (!timelineInfo.Caster || timelineInfo.TryToStop)
                {
                    DebugUtil.LogOrange($"删除时间轴：{timelineInfo.Caster?.gameObject.name ?? ""}——{timelineInfo.Timeline.Id}");
                    _timelines.RemoveAt(index);

                    // 如果仍然有处于播放的片段，就停止该片段
                    foreach (var clip in timelineInfo.Timeline.Clips)
                    {
                        if (clip.Playing)
                        {
                            clip.Stop(timelineInfo);
                        }
                    }

                    timelineInfo.NotifyStop();

                    continue;
                }

                // 执行时间轴帧函数
                TickTimeline(deltaTime, timelineInfo);

                // 如果超过时间轴时长，此时认为是时间轴完成
                if (timelineInfo.TimeElapsed >= timelineInfo.Timeline.Duration)
                {
                    _timelines.RemoveAt(index);

                    // 如果仍然有处于播放的片段，就停止该片段
                    foreach (var clip in timelineInfo.Timeline.Clips)
                    {
                        if (clip.Playing)
                        {
                            clip.Stop(timelineInfo);
                        }
                    }

                    timelineInfo.NotifyComplete();

                    continue;
                }

                index++;
            }
        }

        public bool ContainsTimeline(string timelineId)
        {
            return _timelines.Find(timelineInfo => timelineInfo.Timeline.Id == timelineId) != null;
        }

        public TimelineInfo StartTimeline(Timeline model, GameObject caster)
        {
            DebugUtil.LogOrange($"添加时间轴：{caster.gameObject.name}——{model.Id}");
            var timelineInfo = new TimelineInfo(model, caster);
            _timelines.Add(timelineInfo);
            return timelineInfo;
        }

        public void StartTimeline(TimelineInfo timelineInfo)
        {
            _timelines.Add(timelineInfo);
        }

        public void StopTimeline(string timelineId)
        {
            var index = _timelines.FindIndex(timelineInfo => timelineInfo.Timeline.Id == timelineId);
            if (index == -1)
            {
                return;
            }

            var timelineInfo = _timelines[index];
            timelineInfo.TryToStop = true;
        }

        public void StopAllTimelines()
        {
            foreach (var timelineInfo in _timelines)
            {
                timelineInfo.TryToStop = true;
            }
        }

        public TimelineInfo GetTimelineInfo(string timelineId)
        {
            return _timelines.FirstOrDefault(timelineInfo => timelineInfo.Timeline.Id == timelineId);
        }

        public void SetTimelineTimeScale(string timelineId, float timeScale)
        {
            var timelineInfo = GetTimelineInfo(timelineId);
            if (timelineInfo == null)
            {
                return;
            }

            timelineInfo.Timescale = timeScale;
        }

        /// <summary>
        /// 时间轴帧函数，执行时间区域是(上一帧时间, 当前帧时间]区间，但是如果当前时间轴上一帧时间刚好是0，为了弥补无起始函数的不足，会修改为[0, 当前帧时间]区间
        /// 举例：
        /// 1.当前时间轴上一帧时间是0，当前帧时间是0.016，则会执行[0, 0.016]区间的节点和片段
        /// 2.当前时间轴上一帧时间是0.016，当前帧时间是0.032，则会执行(0.016, 0.032]区间的节点和片段
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="timelineInfo"></param>
        private void TickTimeline(float deltaTime, TimelineInfo timelineInfo)
        {
            var previousTimeElapsed = timelineInfo.TimeElapsed;
            var deltaTimeInternal = deltaTime * timelineInfo.Timescale;
            timelineInfo.TimeElapsed += deltaTimeInternal;
            // // 这里限制每个时间轴的时间最大为预设时长，避免超过后执行在时间轴外的节点和片段
            // timelineInfo.TimeElapsed = Mathf.Clamp(timelineInfo.TimeElapsed + deltaTimeInternal, timelineInfo.TimeElapsed, timelineInfo.Model.Duration);

            foreach (var node in timelineInfo.Timeline.Nodes)
            {
                if ((previousTimeElapsed == 0f && node.TimeElapsed >= previousTimeElapsed &&
                     node.TimeElapsed <= timelineInfo.TimeElapsed) ||
                    (node.TimeElapsed > previousTimeElapsed && node.TimeElapsed <= timelineInfo.TimeElapsed))
                {
                    node.Execute(timelineInfo);
                }
            }

            foreach (var clip in timelineInfo.Timeline.Clips)
            {
                if (clip.Playing) // 如果是已经开始播放的，传入完整间隔时间，具体结束逻辑在其内部实现
                {
                    clip.Play(deltaTimeInternal, timelineInfo);
                }
                else if ((previousTimeElapsed == 0f && clip.StartTime >= previousTimeElapsed &&
                          clip.StartTime <= timelineInfo.TimeElapsed) ||
                         (clip.StartTime > previousTimeElapsed &&
                          clip.StartTime <= timelineInfo.TimeElapsed)) // 如果是刚好在区间内开始播放的，仅传入时间差值
                {
                    clip.Play(timelineInfo.TimeElapsed - clip.StartTime, timelineInfo);
                }
            }
        }
    }
}