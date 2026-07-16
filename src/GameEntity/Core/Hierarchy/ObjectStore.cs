using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class ObjectStore
    {
        private readonly Dictionary<long, Entity> _objects = new Dictionary<long, Entity>();

        public void Add(EntityHandle handle, Entity entity)
        {
            _objects[handle.NodeId] = entity;
        }

        public bool TryGet(long nodeId, out Entity entity)
        {
            return _objects.TryGetValue(nodeId, out entity);
        }

        public Entity Get(long nodeId)
        {
            return _objects[nodeId];
        }

        public void Remove(long nodeId)
        {
            _objects.Remove(nodeId);
        }

        public IReadOnlyList<KeyValuePair<long, Entity>> GetAllEntries()
        {
            return _objects.ToList();
        }

        public void Clear()
        {
            _objects.Clear();
        }
    }
}
