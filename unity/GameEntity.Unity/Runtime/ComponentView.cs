using UnityEngine;

namespace GameEntity.Unity
{
    /// <summary>
    /// Unity Hierarchy 中的 Entity 调试桥接组件。
    /// </summary>
    public sealed class ComponentView : MonoBehaviour
    {
        public Entity Entity { get; private set; }

        public Entity Component => Entity;

        public long InstanceId { get; private set; }

        public bool IsReleased { get; private set; }

        internal void Bind(Entity entity)
        {
            Entity = entity;
            InstanceId = entity?.InstanceId ?? 0;
            IsReleased = false;
        }

        internal void MarkReleased()
        {
            Entity = null;
            InstanceId = 0;
            IsReleased = true;
        }
    }
}
