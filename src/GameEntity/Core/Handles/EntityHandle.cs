using System;

namespace GameEntity
{
    /// <summary>
    /// Entity 的运行时安全句柄。
    /// NodeId 是 World 内单调递增的 runtime 节点 id，销毁后不复用。
    /// </summary>
    public readonly struct EntityHandle : IEquatable<EntityHandle>
    {
        public static readonly EntityHandle None = new EntityHandle(0);

        public EntityHandle(long nodeId)
        {
            NodeId = nodeId;
        }

        public long NodeId { get; }

        public bool IsValid => NodeId > 0;

        public bool Equals(EntityHandle other)
        {
            return NodeId == other.NodeId;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return NodeId.GetHashCode();
        }

        public override string ToString()
        {
            return IsValid ? NodeId.ToString() : "None";
        }

        public static bool operator ==(EntityHandle left, EntityHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityHandle left, EntityHandle right)
        {
            return !left.Equals(right);
        }
    }
}
