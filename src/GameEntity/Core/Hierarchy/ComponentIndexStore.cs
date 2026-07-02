using System;
using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class ComponentIndexStore
    {
        private readonly Dictionary<ComponentKey, int> _components = new Dictionary<ComponentKey, int>();

        public void Add(int ownerNodeId, long typeId, int componentNodeId)
        {
            var key = new ComponentKey(ownerNodeId, typeId);
            if (_components.TryGetValue(key, out var existingNodeId) && existingNodeId != componentNodeId)
            {
                throw new Exception($"entity already has component type id: {typeId}");
            }

            _components[key] = componentNodeId;
        }

        public bool TryGet(int ownerNodeId, long typeId, out int componentNodeId)
        {
            return _components.TryGetValue(new ComponentKey(ownerNodeId, typeId), out componentNodeId);
        }

        public void Remove(int ownerNodeId, long typeId, int componentNodeId)
        {
            var key = new ComponentKey(ownerNodeId, typeId);
            if (_components.TryGetValue(key, out var currentNodeId) && currentNodeId == componentNodeId)
            {
                _components.Remove(key);
            }
        }

        public void Clear()
        {
            _components.Clear();
        }

        private readonly struct ComponentKey : IEquatable<ComponentKey>
        {
            private readonly int _ownerNodeId;
            private readonly long _typeId;

            public ComponentKey(int ownerNodeId, long typeId)
            {
                _ownerNodeId = ownerNodeId;
                _typeId = typeId;
            }

            public bool Equals(ComponentKey other)
            {
                return _ownerNodeId == other._ownerNodeId && _typeId == other._typeId;
            }

            public override bool Equals(object obj)
            {
                return obj is ComponentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_ownerNodeId * 397) ^ _typeId.GetHashCode();
                }
            }
        }
    }
}
