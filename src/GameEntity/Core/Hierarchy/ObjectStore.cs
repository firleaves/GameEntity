using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class ObjectStore
    {
        private readonly Dictionary<int, Entity> _objects = new Dictionary<int, Entity>();

        public void Add(EntityHandle handle, Entity entity)
        {
            _objects[handle.NodeId] = entity;
        }

        public bool TryGet(int nodeId, out Entity entity)
        {
            return _objects.TryGetValue(nodeId, out entity);
        }

        public Entity Get(int nodeId)
        {
            return _objects[nodeId];
        }

        public void Remove(int nodeId)
        {
            _objects.Remove(nodeId);
        }

        public void Clear()
        {
            _objects.Clear();
        }
    }
}
