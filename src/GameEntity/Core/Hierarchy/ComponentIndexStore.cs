using System;
using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class ComponentIndexStore
    {
        private readonly Dictionary<ComponentKey, int> _components = new Dictionary<ComponentKey, int>();

        public void Add(int ownerNodeId, long componentTypeId, int componentNodeId)
        {
            var key = new ComponentKey(ownerNodeId, componentTypeId);
            if (_components.TryGetValue(key, out var existingNodeId) && existingNodeId != componentNodeId)
            {
                throw new Exception($"entity already has component type id: {componentTypeId}");
            }

            _components[key] = componentNodeId;
        }

        public bool TryGet(int ownerNodeId, long componentTypeId, out int componentNodeId)
        {
            return _components.TryGetValue(new ComponentKey(ownerNodeId, componentTypeId), out componentNodeId);
        }

        public void Remove(int ownerNodeId, long componentTypeId, int componentNodeId)
        {
            var key = new ComponentKey(ownerNodeId, componentTypeId);
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
            private readonly long _componentTypeId;

            public ComponentKey(int ownerNodeId, long componentTypeId)
            {
                _ownerNodeId = ownerNodeId;
                _componentTypeId = componentTypeId;
            }

            public bool Equals(ComponentKey other)
            {
                return _ownerNodeId == other._ownerNodeId && _componentTypeId == other._componentTypeId;
            }

            public override bool Equals(object obj)
            {
                return obj is ComponentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_ownerNodeId * 397) ^ _componentTypeId.GetHashCode();
                }
            }
        }
    }
}
