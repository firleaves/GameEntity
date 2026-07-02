using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class NodeStore
    {
        private int _nextNodeId;
        private readonly Dictionary<int, NodeRecord> _records = new Dictionary<int, NodeRecord>();
        private readonly Dictionary<int, int> _generations = new Dictionary<int, int>();
        private readonly Stack<int> _freeNodeIds = new Stack<int>();
        private readonly Dictionary<int, SortedDictionary<long, int>> _childrenByOwner = new Dictionary<int, SortedDictionary<long, int>>();
        private readonly Dictionary<int, SortedDictionary<long, int>> _componentsByOwner = new Dictionary<int, SortedDictionary<long, int>>();

        public EntityHandle CreateNode(Entity entity, NodeKind kind, int ownerNodeId, int sceneNodeId, long typeId)
        {
            int nodeId;
            int generation;
            if (_freeNodeIds.Count > 0)
            {
                nodeId = _freeNodeIds.Pop();
                generation = _generations.TryGetValue(nodeId, out var currentGeneration)
                    ? currentGeneration + 1
                    : 1;
            }
            else
            {
                nodeId = ++_nextNodeId;
                generation = 1;
            }

            _generations[nodeId] = generation;
            var record = new NodeRecord
            {
                NodeId = nodeId,
                Generation = generation,
                BusinessId = entity.Id,
                InstanceId = entity.InstanceId,
                OwnerNodeId = ownerNodeId,
                SceneNodeId = sceneNodeId,
                TypeId = typeId,
                Kind = kind,
                Flags = NodeFlags.Alive,
            };

            _records.Add(nodeId, record);
            return record.Handle;
        }

        public bool TryGetRecord(int nodeId, out NodeRecord record)
        {
            return _records.TryGetValue(nodeId, out record);
        }

        public bool TryGetRecord(EntityHandle handle, out NodeRecord record)
        {
            if (!handle.IsValid || !_records.TryGetValue(handle.NodeId, out record))
            {
                record = default;
                return false;
            }

            return record.Generation == handle.Generation && record.IsAlive;
        }

        public NodeRecord GetRecord(int nodeId)
        {
            return _records[nodeId];
        }

        public void SetRecord(NodeRecord record)
        {
            _records[record.NodeId] = record;
        }

        public void SetInstanceId(int nodeId, long instanceId)
        {
            var record = _records[nodeId];
            record.InstanceId = instanceId;
            _records[nodeId] = record;
        }

        public void SetSceneNodeId(int nodeId, int sceneNodeId)
        {
            var record = _records[nodeId];
            record.SceneNodeId = sceneNodeId;
            _records[nodeId] = record;
        }

        public void SetDisposing(int nodeId)
        {
            var record = _records[nodeId];
            record.Flags |= NodeFlags.Disposing;
            _records[nodeId] = record;
        }

        public void AttachChild(int ownerNodeId, int childNodeId, long businessId)
        {
            if (!_childrenByOwner.TryGetValue(ownerNodeId, out var children))
            {
                children = new SortedDictionary<long, int>();
                _childrenByOwner.Add(ownerNodeId, children);
            }

            children.Add(businessId, childNodeId);
        }

        public void AttachComponent(int ownerNodeId, int componentNodeId, long typeId)
        {
            if (!_componentsByOwner.TryGetValue(ownerNodeId, out var components))
            {
                components = new SortedDictionary<long, int>();
                _componentsByOwner.Add(ownerNodeId, components);
            }

            components.Add(typeId, componentNodeId);
        }

        public void DetachFromOwner(NodeRecord record)
        {
            if (record.OwnerNodeId == 0)
            {
                return;
            }

            if (record.Kind == NodeKind.ChildEntity)
            {
                RemoveChild(record.OwnerNodeId, record.BusinessId, record.NodeId);
                return;
            }

            if (record.Kind == NodeKind.ComponentEntity)
            {
                RemoveComponent(record.OwnerNodeId, record.TypeId, record.NodeId);
            }
        }

        public bool HasChild(int ownerNodeId, long businessId, int exceptNodeId = 0)
        {
            return TryGetChild(ownerNodeId, businessId, out var childNodeId) && childNodeId != exceptNodeId;
        }

        public bool TryGetChild(int ownerNodeId, long businessId, out int childNodeId)
        {
            childNodeId = 0;
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children) &&
                   children.TryGetValue(businessId, out childNodeId);
        }

        public IReadOnlyList<int> GetChildren(int ownerNodeId)
        {
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children)
                ? children.Values.ToList()
                : Array.Empty<int>();
        }

        public int GetChildrenCount(int ownerNodeId)
        {
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children) ? children.Count : 0;
        }

        public IReadOnlyList<int> GetComponents(int ownerNodeId)
        {
            return _componentsByOwner.TryGetValue(ownerNodeId, out var components)
                ? components.Values.ToList()
                : Array.Empty<int>();
        }

        public int GetComponentsCount(int ownerNodeId)
        {
            return _componentsByOwner.TryGetValue(ownerNodeId, out var components) ? components.Count : 0;
        }

        public void RemoveNode(int nodeId)
        {
            if (_records.Remove(nodeId))
            {
                _freeNodeIds.Push(nodeId);
            }

            _childrenByOwner.Remove(nodeId);
            _componentsByOwner.Remove(nodeId);
        }

        public IReadOnlyList<NodeRecord> GetAllRecords()
        {
            return _records.Values.ToList();
        }

        public bool IsAttachedToOwnerIndex(NodeRecord record)
        {
            if (record.OwnerNodeId == 0)
            {
                return record.Kind == NodeKind.SceneRoot;
            }

            if (record.Kind == NodeKind.ChildEntity)
            {
                return _childrenByOwner.TryGetValue(record.OwnerNodeId, out var children) &&
                       children.TryGetValue(record.BusinessId, out var childNodeId) &&
                       childNodeId == record.NodeId;
            }

            if (record.Kind == NodeKind.ComponentEntity)
            {
                return _componentsByOwner.TryGetValue(record.OwnerNodeId, out var components) &&
                       components.TryGetValue(record.TypeId, out var componentNodeId) &&
                       componentNodeId == record.NodeId;
            }

            return false;
        }

        public IReadOnlyList<int> GetSceneRoots()
        {
            return _records.Values
                .Where(r => r.Kind == NodeKind.SceneRoot && r.IsAlive)
                .Select(r => r.NodeId)
                .ToList();
        }

        public void Clear()
        {
            _records.Clear();
            _generations.Clear();
            _freeNodeIds.Clear();
            _childrenByOwner.Clear();
            _componentsByOwner.Clear();
            _nextNodeId = 0;
        }

        private void RemoveChild(int ownerNodeId, long businessId, int childNodeId)
        {
            if (!_childrenByOwner.TryGetValue(ownerNodeId, out var children))
            {
                return;
            }

            if (children.TryGetValue(businessId, out var currentNodeId) && currentNodeId == childNodeId)
            {
                children.Remove(businessId);
            }

            if (children.Count == 0)
            {
                _childrenByOwner.Remove(ownerNodeId);
            }
        }

        private void RemoveComponent(int ownerNodeId, long typeId, int componentNodeId)
        {
            if (!_componentsByOwner.TryGetValue(ownerNodeId, out var components))
            {
                return;
            }

            if (components.TryGetValue(typeId, out var currentNodeId) && currentNodeId == componentNodeId)
            {
                components.Remove(typeId);
            }

            if (components.Count == 0)
            {
                _componentsByOwner.Remove(ownerNodeId);
            }
        }
    }
}
