using System;

namespace GameEntity.Unity.Framework
{
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
