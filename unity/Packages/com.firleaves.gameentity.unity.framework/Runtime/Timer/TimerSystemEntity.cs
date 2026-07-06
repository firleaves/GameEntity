using System;
using System.Collections.Generic;
using GameEntity;

namespace GameEntity.Unity.Framework
{
    public sealed class TimerSystemEntity : Entity, IAwake, IUpdate, IDestroy, ITimerSystem
    {
        private readonly Dictionary<int, TimerEntry> _timers = new Dictionary<int, TimerEntry>();
        private readonly List<int> _removeBuffer = new List<int>();
        private int _nextId;

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            if (_timers.Count == 0)
            {
                return;
            }

            _removeBuffer.Clear();
            foreach (var pair in _timers)
            {
                var timer = pair.Value;
                if (timer.Paused)
                {
                    continue;
                }

                var tickDelta = timer.Unscaled ? UnityEngine.Time.unscaledDeltaTime : deltaTime;
                timer.Elapsed += Math.Max(0f, tickDelta);
                while (timer.Elapsed >= timer.Interval && !timer.Paused)
                {
                    timer.Elapsed -= timer.Interval;
                    timer.TickCount++;
                    timer.Callback?.Invoke(timer.TickCount);

                    if (timer.RepeatCount >= 0 && timer.TickCount >= timer.RepeatCount)
                    {
                        _removeBuffer.Add(pair.Key);
                        break;
                    }
                }
            }

            for (var i = 0; i < _removeBuffer.Count; i++)
            {
                _timers.Remove(_removeBuffer[i]);
            }

            _removeBuffer.Clear();
        }

        public void OnDestroy()
        {
            CancelAll();
        }

        public TimerHandle Delay(float seconds, Action callback, bool unscaled = false)
        {
            return Every(seconds, _ => callback?.Invoke(), 1, unscaled);
        }

        public TimerHandle Every(float interval, Action<int> callback, int repeatCount = -1, bool unscaled = false)
        {
            if (interval <= 0f)
            {
                throw new FrameworkException("创建 Timer 失败：interval 必须大于 0。");
            }

            var id = ++_nextId;
            _timers.Add(id, new TimerEntry(interval, callback, repeatCount, unscaled));
            return new TimerHandle(id);
        }

        public bool Cancel(TimerHandle handle)
        {
            return handle.IsValid && _timers.Remove(handle.Id);
        }

        public void CancelAll()
        {
            _timers.Clear();
            _removeBuffer.Clear();
        }

        public bool Pause(TimerHandle handle)
        {
            if (!handle.IsValid || !_timers.TryGetValue(handle.Id, out var timer))
            {
                return false;
            }

            timer.Paused = true;
            return true;
        }

        public bool Resume(TimerHandle handle)
        {
            if (!handle.IsValid || !_timers.TryGetValue(handle.Id, out var timer))
            {
                return false;
            }

            timer.Paused = false;
            return true;
        }

        private sealed class TimerEntry
        {
            public TimerEntry(float interval, Action<int> callback, int repeatCount, bool unscaled)
            {
                Interval = interval;
                Callback = callback;
                RepeatCount = repeatCount;
                Unscaled = unscaled;
            }

            public float Interval { get; }
            public Action<int> Callback { get; }
            public int RepeatCount { get; }
            public bool Unscaled { get; }
            public float Elapsed;
            public int TickCount;
            public bool Paused;
        }
    }
}
