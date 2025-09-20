using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Common.Function
{
    /// <summary>
    /// 定时器管理类，用于提供定时器相关方法
    /// 注意由于定时器本身使用的是WaitForSeconds协程，不能保证精确定时，一般用于允许较大误差的定时场景，一般会有0.2秒误差，不适用移动位移的精确场景
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        /// <summary>
        /// 定时器数据子类，内置id/时间/委托等，并提供单个定时器相关功能
        /// </summary>
        private class TimerItem
        {
            private int _id;

            private int _totalTime;

            private int _intervalTime;

            private int _currentTotalTime;

            private int _currentIntervalTime;

            private bool _running;

            private UnityAction _totalTimeOverEvent;

            private UnityAction _intervalTimeOverEvent;

            public int ID => _id;

            public int TotalTime => _totalTime;

            public int IntervalTime => _intervalTime;

            public int CurrentTotalTime => _currentTotalTime;

            public int CurrentIntervalTime => _currentIntervalTime;

            public bool Running => _running;

            public UnityAction TotalTimeOverEvent => _totalTimeOverEvent;

            public UnityAction IntervalTimeOverEvent => _intervalTimeOverEvent;

            public void Init(
                int id,
                int totalTime,
                UnityAction totalTimeOverAction,
                int intervalTime = 0,
                UnityAction intervalTimeOverAction = null
            )
            {
                _id = id;
                _totalTime = totalTime;
                _totalTimeOverEvent = totalTimeOverAction;
                _intervalTime = intervalTime;
                _intervalTimeOverEvent = intervalTimeOverAction;
                _running = true;
            }

            public void Reset()
            {
                _currentTotalTime = 0;
                _currentIntervalTime = 0;
                _running = true;
            }

            public void Start()
            {
                _running = true;
            }

            public void Stop()
            {
                _running = false;
            }

            public void Clear()
            {
                _id = 0;
                _totalTime = 0;
                _intervalTime = 0;
                _currentTotalTime = 0;
                _currentIntervalTime = 0;
                _running = false;
                _totalTimeOverEvent = null;
                _intervalTimeOverEvent = null;
            }

            public bool Count(int time)
            {
                // 判断计时器是否有间隔时间执行的需求，是则计时
                if (_intervalTimeOverEvent != null)
                {
                    _currentIntervalTime += time;
                    if (_currentIntervalTime >= _intervalTime)
                    {
                        _intervalTimeOverEvent?.Invoke();
                        _currentIntervalTime = 0;
                    }
                }

                // 总时间计时
                _currentTotalTime += time;
                if (_currentTotalTime >= _totalTime)
                {
                    _totalTimeOverEvent?.Invoke();
                    return true;
                }

                return false;
            }
        }

        // 定时器ID置空值
        public const int None = -1;

        private const float DeltaTime = 0.1f;

        // 定时器ID键
        private static int _timerIdKey = None;

        // 存储id<-->定时器子项的字典，其中包含了运行中以及暂停中的定时器，完成或被删除的定时器不在其中
        private readonly Dictionary<int, TimerItem> _timers = new();

        // 存储id<-->定时器子项（不受Timescale影响）的字典，其中包含了运行中以及暂停中的定时器，完成或被删除的定时器不在其中
        private readonly Dictionary<int, TimerItem> _realTimers = new();

        // 协程对象
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime = new(DeltaTime);
        private readonly WaitForSeconds _waitForSeconds = new(DeltaTime);

        private Coroutine _timerCoroutine;
        private Coroutine _realTimerCoroutine;

        // 定时器对象池
        private readonly ObjectPool<TimerItem> _timerItemPool = new(
            () => new TimerItem()
        );

        private void Start()
        {
            StartCount();
        }

        /// <summary>
        /// 开启计数，默认调用该方法，开始记录时间，影响全部定时器
        /// </summary>
        public void StartCount()
        {
            _timerCoroutine = StartCoroutine(StartTiming(false));
            _realTimerCoroutine = StartCoroutine(StartTiming(true));
        }

        /// <summary>
        /// 停止计数，停止记录时间，影响全部定时器
        /// </summary>
        public void StopCount()
        {
            StopCoroutine(_timerCoroutine);
            StopCoroutine(_realTimerCoroutine);
        }

        private IEnumerator StartTiming(bool isRealtime)
        {
            while (true)
            {
                if (isRealtime)
                    yield return _waitForSecondsRealtime;
                else
                    yield return _waitForSeconds;

                // 遍历所有的计时器，如果在运行中则计时并执行回调，否则就跳过
                var timers = isRealtime ? _realTimers : _timers;
                var timerIds = new List<int>(timers.Keys);
                var deleteTimers = new List<TimerItem>();

                // 注意这里遍历字典要修改其子项内部成员时不能用foreach，否则会报错
                foreach (var timerId in timerIds)
                {
                    var result = timers.TryGetValue(timerId, out var timer);
                    if (!result || !timer.Running)
                    {
                        continue;
                    }

                    if (timer.Count((int)(DeltaTime * 1000)))
                    {
                        deleteTimers.Add(timer);
                    }
                }

                // 移除待移除列表中的数据
                foreach (var deleteTimer in deleteTimers)
                {
                    timers.Remove(deleteTimer.ID);
                    _timerItemPool.Release(deleteTimer, timerItem => timerItem.Clear());
                }
            }
        }

        /// <summary>
        /// 创建定时器
        /// </summary>
        /// <param name="isRealTime">是否为实际时间，是则不受Timescale影响</param>
        /// <param name="totalTime">定时总毫秒数</param>
        /// <param name="totalTimeOverCallBack">定时总回调</param>
        /// <param name="intervalTime">定时间隔毫秒数</param>
        /// <param name="intervalTimeOverCallBack">定时间隔回调</param>
        /// <returns>定时器id</returns>
        public int CreateTimer(
            bool isRealTime,
            int totalTime,
            UnityAction totalTimeOverCallBack,
            int intervalTime = 0,
            UnityAction intervalTimeOverCallBack = null
        )
        {
            var id = ++_timerIdKey;
            var timerItem = _timerItemPool.Get((timerItem) =>
                timerItem.Init(id, totalTime, totalTimeOverCallBack, intervalTime, intervalTimeOverCallBack));
            if (isRealTime)
                _realTimers.Add(id, timerItem);
            else
                _timers.Add(id, timerItem);
            return id;
        }

        /// <summary>
        /// 删除定时器
        /// </summary>
        /// <param name="id">定时器id</param>
        public void RemoveTimer(int id)
        {
            if (_timers.ContainsKey(id))
            {
                _timerItemPool.Release(_timers[id], timerItem => timerItem.Clear());
                _timers.Remove(id);
            }
            else if (_realTimers.ContainsKey(id))
            {
                _timerItemPool.Release(_realTimers[id], timerItem => timerItem.Clear());
                _realTimers.Remove(id);
            }
        }

        /// <summary>
        /// 重置定时器，从头开始计时
        /// </summary>
        /// <param name="id">定时器id</param>
        public void ResetTimer(int id)
        {
            if (_timers.ContainsKey(id))
            {
                _timers[id].Reset();
            }
            else if (_realTimers.ContainsKey(id))
            {
                _realTimers[id].Reset();
            }
        }

        /// <summary>
        /// 开启定时器，默认在创建时即为开始
        /// </summary>
        /// <param name="id">定时器id</param>
        public void StartTimer(int id)
        {
            if (_timers.ContainsKey(id))
            {
                _timers[id].Start();
            }
            else if (_realTimers.ContainsKey(id))
            {
                _realTimers[id].Start();
            }
        }

        /// <summary>
        /// 停止定时器，该定时器不参与计时
        /// </summary>
        /// <param name="id">定时器id</param>
        public void StopTimer(int id)
        {
            if (_timers.ContainsKey(id))
            {
                _timers[id].Stop();
            }
            else if (_realTimers.ContainsKey(id))
            {
                _realTimers[id].Stop();
            }
        }
    }
}