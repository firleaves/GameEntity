using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class EntityUpdateBucket
    {
        private readonly List<EntityHandle> _handles = new List<EntityHandle>();
        private readonly HashSet<EntityHandle> _handleSet = new HashSet<EntityHandle>();

        public bool Register(EntityHandle handle)
        {
            if (!handle.IsValid || !_handleSet.Add(handle))
            {
                return false;
            }

            _handles.Add(handle);
            return true;
        }

        public bool Unregister(EntityHandle handle)
        {
            return handle.IsValid && _handleSet.Remove(handle);
        }

        public bool Contains(EntityHandle handle)
        {
            return handle.IsValid && _handleSet.Contains(handle);
        }

        public IReadOnlyList<EntityHandle> Snapshot()
        {
            return _handles.ToArray();
        }

        public bool IsRegistered(EntityHandle handle)
        {
            return Contains(handle);
        }

        public void Compact()
        {
            _handles.RemoveAll(handle => !_handleSet.Contains(handle));
        }

        public void Clear()
        {
            _handles.Clear();
            _handleSet.Clear();
        }
    }
}
