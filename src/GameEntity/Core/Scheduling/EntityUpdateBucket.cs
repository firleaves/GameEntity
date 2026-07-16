using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class EntityUpdateBucket
    {
        private readonly List<EntityHandle> _handles = new List<EntityHandle>();
        private readonly HashSet<EntityHandle> _listedHandles = new HashSet<EntityHandle>();
        private readonly Dictionary<EntityHandle, float> _elapsedTimes = new Dictionary<EntityHandle, float>();

        public int Count => _elapsedTimes.Count;

        public bool Register(EntityHandle handle, float elapsedTime = 0f)
        {
            if (!handle.IsValid || _elapsedTimes.ContainsKey(handle))
            {
                return false;
            }

            _elapsedTimes.Add(handle, elapsedTime);
            if (_listedHandles.Add(handle))
            {
                _handles.Add(handle);
            }

            return true;
        }

        public bool Unregister(EntityHandle handle)
        {
            return TryUnregister(handle, out _);
        }

        public bool TryUnregister(EntityHandle handle, out float elapsedTime)
        {
            if (!handle.IsValid || !_elapsedTimes.TryGetValue(handle, out elapsedTime))
            {
                elapsedTime = 0f;
                return false;
            }

            _elapsedTimes.Remove(handle);
            return true;
        }

        public bool Contains(EntityHandle handle)
        {
            return handle.IsValid && _elapsedTimes.ContainsKey(handle);
        }

        public IReadOnlyList<EntityHandle> Snapshot()
        {
            return _handles.ToArray();
        }

        public IReadOnlyList<EntityHandle> GetHandleListSnapshot()
        {
            return _handles.ToArray();
        }

        public IReadOnlyList<EntityHandle> GetMembershipSnapshot()
        {
            return _listedHandles.ToArray();
        }

        public IReadOnlyList<EntityHandle> GetRegisteredHandlesSnapshot()
        {
            return _elapsedTimes.Keys.ToArray();
        }

        public bool IsRegistered(EntityHandle handle)
        {
            return Contains(handle);
        }

        public void Compact()
        {
            if (_handles.Count == _listedHandles.Count)
            {
                _handles.RemoveAll(handle => !_elapsedTimes.ContainsKey(handle));
                _listedHandles.RemoveWhere(handle => !_elapsedTimes.ContainsKey(handle));
                return;
            }

            var seen = new HashSet<EntityHandle>();
            _handles.RemoveAll(handle => !_elapsedTimes.ContainsKey(handle) || !seen.Add(handle));
            _listedHandles.Clear();
            foreach (EntityHandle handle in _handles)
            {
                _listedHandles.Add(handle);
            }
        }

        public bool TryAccumulate(EntityHandle handle, float deltaTime, out float elapsedTime)
        {
            if (!_elapsedTimes.TryGetValue(handle, out elapsedTime))
            {
                return false;
            }

            elapsedTime += deltaTime;
            _elapsedTimes[handle] = elapsedTime;
            return true;
        }

        public void ResetElapsed(EntityHandle handle)
        {
            if (_elapsedTimes.ContainsKey(handle))
            {
                _elapsedTimes[handle] = 0f;
            }
        }

        public void Clear()
        {
            _handles.Clear();
            _listedHandles.Clear();
            _elapsedTimes.Clear();
        }
    }
}
