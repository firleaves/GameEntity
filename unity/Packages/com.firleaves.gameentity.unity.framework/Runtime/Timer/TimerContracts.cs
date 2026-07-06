using System;

namespace GameEntity.Unity.Framework
{
    public interface ITimerSystem
    {
        TimerHandle Delay(float seconds, Action callback, bool unscaled = false);
        TimerHandle Every(float interval, Action<int> callback, int repeatCount = -1, bool unscaled = false);
        bool Cancel(TimerHandle handle);
        void CancelAll();
        bool Pause(TimerHandle handle);
        bool Resume(TimerHandle handle);
    }

    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        internal readonly int Id;

        internal TimerHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id > 0;

        public bool Equals(TimerHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is TimerHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
