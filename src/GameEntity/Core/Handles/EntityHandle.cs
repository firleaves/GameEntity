using System;

namespace GameEntity
{
    /// <summary>
    /// Entity 的运行时安全句柄。
    /// NodeId 标识节点槽位，Generation 用于节点复用后的失效校验。
    /// </summary>
    public readonly struct EntityHandle : IEquatable<EntityHandle>
    {
        public static readonly EntityHandle None = new EntityHandle(0, 0);

        public EntityHandle(int nodeId, int generation)
        {
            NodeId = nodeId;
            Generation = generation;
        }

        public int NodeId { get; }

        public int Generation { get; }

        public bool IsValid => NodeId > 0 && Generation > 0;

        public bool Equals(EntityHandle other)
        {
            return NodeId == other.NodeId && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (NodeId * 397) ^ Generation;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{NodeId}:{Generation}" : "None";
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
