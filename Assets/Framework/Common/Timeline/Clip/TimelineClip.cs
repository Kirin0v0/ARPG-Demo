using Framework.Common.Timeline.Data;

namespace Framework.Common.Timeline.Clip
{
    /// <summary>
    /// 时间轴片段类，与节点类不同在于其设计是为了持续执行函数，内置生命周期函数
    /// 虽然其需要配置时长，但不能与时间轴本身混用，时间轴仅用来管理轴上的数据，该类仅用于持续性执行函数
    /// </summary>
    public abstract class TimelineClip
    {
        public readonly float StartTime; // 片段在时间轴中起始时间
        public readonly int TotalTicks; // 片段总帧数
        public readonly float TickTime; // 片段每帧执行时间

        private bool _playing;
        public bool Playing => _playing;

        private int _tick;
        public int Tick => _tick;
        
        private float _time;
        public float Time => _time;

        protected TimelineClip(float startTime, int totalTicks, float tickTime)
        {
            StartTime = startTime;
            TotalTicks = totalTicks;
            TickTime = tickTime;
        }

        /// <summary>
        /// 片段播放函数，时间轴执行时调用
        /// 核心逻辑都在OnTick中实现，OnStart和OnStop仅用于资源、引用创建和销毁
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="timelineInfo"></param>
        public void Play(float deltaTime, TimelineInfo timelineInfo)
        {
            if (!_playing)
            {
                _tick = 0;
                _time = 0f;
                _playing = true;
                OnStart(timelineInfo);
            }

            // 如果总帧数设置不合理，则直接结束
            if (TotalTicks <= 0)
            {
                Stop(timelineInfo);
            }
            else
            {
                _time += deltaTime;
                // 过滤同一帧并处理帧追赶机制
                var newTick = GetTick(_time);
                while (_tick < newTick)
                {
                    _tick++;
                    OnTick(timelineInfo);
                    // 如果当前帧到达总帧数，则结束片段
                    if (_tick >= TotalTicks)
                    {
                        Stop(timelineInfo);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 片段停止函数
        /// </summary>
        /// <param name="timelineInfo"></param>
        public void Stop(TimelineInfo timelineInfo)
        {
            if (!_playing)
            {
                return;
            }

            _playing = false;
            OnStop(timelineInfo);
        }

        protected abstract void OnStart(TimelineInfo timelineInfo);

        protected abstract void OnTick(TimelineInfo timelineInfo);

        protected abstract void OnStop(TimelineInfo timelineInfo);

        private int GetTick(float time)
        {
            return (int)(time / TickTime);
        }
    }
}