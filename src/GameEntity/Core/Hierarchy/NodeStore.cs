using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class NodeStore
    {
        private readonly IdGenerator _idGenerator;
        private readonly Dictionary<long, EntityNode> _records = new Dictionary<long, EntityNode>();
        private readonly Dictionary<long, SortedDictionary<long, long>> _childrenByOwner = new Dictionary<long, SortedDictionary<long, long>>();
        private readonly Dictionary<long, SortedDictionary<long, long>> _componentsByOwner = new Dictionary<long, SortedDictionary<long, long>>();

        public NodeStore(IdGenerator idGenerator)
        {
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        }

        public EntityHandle CreateNode(Entity entity, EntityNodeKind kind, long ownerNodeId, long sceneNodeId, long componentTypeId)
        {
            long nodeId = _idGenerator.GenerateId();
            var record = new EntityNode
            {
                NodeId = nodeId,
                EntityId = entity.Id,
                InstanceId = entity.InstanceId,
                OwnerNodeId = ownerNodeId,
                SceneNodeId = sceneNodeId,
                ComponentTypeId = componentTypeId,
                Kind = kind,
                Flags = EntityNodeFlags.Alive,
            };

            _records.Add(nodeId, record);
            return record.Handle;
        }

        public bool TryGetNode(long nodeId, out EntityNode record)
        {
            return _records.TryGetValue(nodeId, out record);
        }

        public bool TryGetNode(EntityHandle handle, out EntityNode record)
        {
            if (!handle.IsValid || !_records.TryGetValue(handle.NodeId, out record))
            {
                record = default;
                return false;
            }

            return record.IsAlive;
        }

        public EntityNode GetNode(long nodeId)
        {
            return _records[nodeId];
        }

        public void SetNode(EntityNode record)
        {
            _records[record.NodeId] = record;
        }

        public void SetInstanceId(long nodeId, long instanceId)
        {
            var record = _records[nodeId];
            record.InstanceId = instanceId;
            _records[nodeId] = record;
        }

        public void SetSceneNodeId(long nodeId, long sceneNodeId)
        {
            var record = _records[nodeId];
            record.SceneNodeId = sceneNodeId;
            _records[nodeId] = record;
        }

        public void SetDestroying(long nodeId)
        {
            var record = _records[nodeId];
            record.Flags |= EntityNodeFlags.Destroying;
            _records[nodeId] = record;
        }

        public void AttachChild(long ownerNodeId, long childNodeId, long entityId)
        {
            if (!_childrenByOwner.TryGetValue(ownerNodeId, out var children))
            {
                children = new SortedDictionary<long, long>();
                _childrenByOwner.Add(ownerNodeId, children);
            }

            children.Add(entityId, childNodeId);
        }

        public void AttachComponent(long ownerNodeId, long componentNodeId, long componentTypeId)
        {
            if (!_componentsByOwner.TryGetValue(ownerNodeId, out var components))
            {
                components = new SortedDictionary<long, long>();
                _componentsByOwner.Add(ownerNodeId, components);
            }

            components.Add(componentTypeId, componentNodeId);
        }

        public bool HasComponent(long ownerNodeId, long componentTypeId, long exceptNodeId = 0)
        {
            return TryGetComponent(ownerNodeId, componentTypeId, out var componentNodeId) && componentNodeId != exceptNodeId;
        }

        public bool TryGetComponent(long ownerNodeId, long componentTypeId, out long componentNodeId)
        {
            componentNodeId = 0;
            return _componentsByOwner.TryGetValue(ownerNodeId, out var components) &&
                   components.TryGetValue(componentTypeId, out componentNodeId);
        }

        public void DetachFromOwner(EntityNode record)
        {
            if (record.OwnerNodeId == 0)
            {
                return;
            }

            if (record.Kind == EntityNodeKind.ChildEntity)
            {
                RemoveChild(record.OwnerNodeId, record.EntityId, record.NodeId);
                return;
            }

            if (record.Kind == EntityNodeKind.ComponentEntity)
            {
                RemoveComponent(record.OwnerNodeId, record.ComponentTypeId, record.NodeId);
            }
        }

        public bool HasChild(long ownerNodeId, long entityId, long exceptNodeId = 0)
        {
            return TryGetChild(ownerNodeId, entityId, out var childNodeId) && childNodeId != exceptNodeId;
        }

        public bool TryGetChild(long ownerNodeId, long entityId, out long childNodeId)
        {
            childNodeId = 0;
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children) &&
                   children.TryGetValue(entityId, out childNodeId);
        }

        public IReadOnlyList<long> GetChildren(long ownerNodeId)
        {
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children)
                ? children.Values.ToList()
                : Array.Empty<long>();
        }

        public int GetChildrenCount(long ownerNodeId)
        {
            return _childrenByOwner.TryGetValue(ownerNodeId, out var children) ? children.Count : 0;
        }

        public IReadOnlyList<long> GetComponents(long ownerNodeId)
        {
            return _componentsByOwner.TryGetValue(ownerNodeId, out var components)
                ? components.Values.ToList()
                : Array.Empty<long>();
        }

        public int GetComponentsCount(long ownerNodeId)
        {
            return _componentsByOwner.TryGetValue(ownerNodeId, out var components) ? components.Count : 0;
        }

        public void RemoveNode(long nodeId)
        {
            _records.Remove(nodeId);
            _childrenByOwner.Remove(nodeId);
            _componentsByOwner.Remove(nodeId);
        }

        public IReadOnlyList<EntityNode> GetAllNodes()
        {
            return _records.Values.ToList();
        }

        public bool IsAttachedToOwnerIndex(EntityNode record)
        {
            if (record.OwnerNodeId == 0)
            {
                return record.Kind == EntityNodeKind.SceneRoot;
            }

            if (record.Kind == EntityNodeKind.ChildEntity)
            {
                return _childrenByOwner.TryGetValue(record.OwnerNodeId, out var children) &&
                       children.TryGetValue(record.EntityId, out var childNodeId) &&
                       childNodeId == record.NodeId;
            }

            if (record.Kind == EntityNodeKind.ComponentEntity)
            {
                return _componentsByOwner.TryGetValue(record.OwnerNodeId, out var components) &&
                       components.TryGetValue(record.ComponentTypeId, out var componentNodeId) &&
                       componentNodeId == record.NodeId;
            }

            return false;
        }

        public IReadOnlyList<long> GetSceneRoots()
        {
            return _records.Values
                .Where(r => r.Kind == EntityNodeKind.SceneRoot && r.IsAlive)
                .Select(r => r.NodeId)
                .ToList();
        }

        public void Clear()
        {
            _records.Clear();
            _childrenByOwner.Clear();
            _componentsByOwner.Clear();
        }

        private void RemoveChild(long ownerNodeId, long entityId, long childNodeId)
        {
            if (!_childrenByOwner.TryGetValue(ownerNodeId, out var children))
            {
                return;
            }

            if (children.TryGetValue(entityId, out var currentNodeId) && currentNodeId == childNodeId)
            {
                children.Remove(entityId);
            }

            if (children.Count == 0)
            {
                _childrenByOwner.Remove(ownerNodeId);
            }
        }

        private void RemoveComponent(long ownerNodeId, long componentTypeId, long componentNodeId)
        {
            if (!_componentsByOwner.TryGetValue(ownerNodeId, out var components))
            {
                return;
            }

            if (components.TryGetValue(componentTypeId, out var currentNodeId) && currentNodeId == componentNodeId)
            {
                components.Remove(componentTypeId);
            }

            if (components.Count == 0)
            {
                _componentsByOwner.Remove(ownerNodeId);
            }
        }
    }
}
