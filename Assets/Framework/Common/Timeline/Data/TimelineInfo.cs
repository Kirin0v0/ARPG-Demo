using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.Timeline.Data
{
    /// <summary>
    /// 时间轴信息类，与外部框架交互使用的类
    /// </summary>
    public class TimelineInfo
    {
        public event System.Action OnStopped;
        public event System.Action OnCompleted;

        public readonly Timeline Timeline;
        public readonly GameObject Caster;

        private float _timescale;

        public float Timescale
        {
            get => _timescale;
            set => _timescale = Mathf.Max(0f, value);
        }

        public float TimeElapsed;

        public bool TryToStop;

        public bool Stopped;
        public bool Completed;
        public bool Finished => Stopped || Completed;

        private readonly Dictionary<string, object> _payloads = new();

        public TimelineInfo(Timeline timeline, GameObject caster)
        {
            Timeline = timeline;
            Caster = caster;
            _timescale = 1f;
            TimeElapsed = 0f;
            TryToStop = false;
            Stopped = false;
            Completed = false;
        }

        public void SetPayload(string key, object value)
        {
            _payloads[key] = value;
        }

        public object GetPayload(string key)
        {
            return !_payloads.ContainsKey(key) ? null : _payloads[key];
        }

        public void NotifyStop()
        {
            if (Finished)
            {
                return;
            }

            Stopped = true;
            OnStopped?.Invoke();
        }

        public void NotifyComplete()
        {
            if (Finished || TimeElapsed < Timeline.Duration)
            {
                return;
            }

            Completed = true;
            OnCompleted?.Invoke();
        }
    }
}