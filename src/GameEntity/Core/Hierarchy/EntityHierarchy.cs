using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class EntityHierarchy : IDisposable
    {
        private readonly World _world;

        public NodeStore Nodes { get; }

        public ObjectStore Objects { get; } = new ObjectStore();

        public SceneRegistry Scenes { get; } = new SceneRegistry();

        public EntityScheduler Scheduler { get; }

        public EntityHierarchy(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Nodes = new NodeStore(_world.IdGenerator);
            Scheduler = new EntityScheduler(this);
        }

        internal World World => _world;

        public void RegisterSceneRoot(Scene scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (TryGetNode(scene, out var existing))
            {
                scene.SetSceneFromHierarchy(scene);
                Scenes.Register(scene.Name, existing.NodeId);
                return;
            }

            var handle = Nodes.CreateNode(scene, EntityNodeKind.SceneRoot, 0, 0, 0);
            var record = Nodes.GetNode(handle.NodeId);
            record.SceneNodeId = handle.NodeId;
            Nodes.SetNode(record);

            scene.AssignHierarchyHandle(this, handle);
            Objects.Add(handle, scene);
            Scenes.Register(scene.Name, handle.NodeId);
            scene.SetSceneFromHierarchy(scene);
        }

        public void AttachChild(Entity owner, Entity child)
        {
            ValidateOwner(owner, child);
            ValidateChildPlacement(owner, child.GetType());

            var ownerRecord = RequireNode(owner);
            var oldParent = GetOwner(child);
            if (oldParent == owner && TryGetNode(child, out var sameRecord) && sameRecord.Kind == EntityNodeKind.ChildEntity)
            {
                Log.Error($"重复设置了Parent: {child.GetType().FullName} parent: {owner.GetType().FullName}");
                return;
            }

            if (Nodes.HasChild(ownerRecord.NodeId, child.Id, TryGetNodeId(child)))
            {
                throw new Exception($"entity already has child id: {child.Id}");
            }

            DetachExistingOwnerIndex(child);

            var childRecord = EnsureNode(child, EntityNodeKind.ChildEntity, ownerRecord.NodeId, ownerRecord.SceneNodeId, 0);
            long previousSceneNodeId = childRecord.SceneNodeId;
            childRecord.Kind = EntityNodeKind.ChildEntity;
            childRecord.OwnerNodeId = ownerRecord.NodeId;
            childRecord.SceneNodeId = ownerRecord.SceneNodeId;
            childRecord.EntityId = child.Id;
            childRecord.ComponentTypeId = 0;
            Nodes.SetNode(childRecord);
            Nodes.AttachChild(ownerRecord.NodeId, childRecord.NodeId, child.Id);

            child.IsComponent = false;
            PropagateScene(child, ownerRecord.SceneNodeId, owner.SceneRoot, previousSceneNodeId);
            if (child.IsTreePublished)
            {
                _world.EntityEvents.NotifyEntityParentChanged(child, oldParent, owner);
            }
        }

        public void AttachComponent(Entity owner, Entity component)
        {
            ValidateOwner(owner, component);
            ValidateComponentPlacement(component.GetType());

            var ownerRecord = RequireNode(owner);
            var type = component.GetType();
            var componentTypeId = owner.GetLongHashCode(type);
            var oldParent = GetOwner(component);

            if (oldParent == owner && TryGetNode(component, out var sameRecord) && sameRecord.Kind == EntityNodeKind.ComponentEntity)
            {
                Log.Error($"重复设置了Parent: {component.GetType().FullName} parent: {owner.GetType().FullName}");
                return;
            }

            if (Nodes.HasComponent(ownerRecord.NodeId, componentTypeId, TryGetNodeId(component)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            DetachExistingOwnerIndex(component);

            var componentRecord = EnsureNode(component, EntityNodeKind.ComponentEntity, ownerRecord.NodeId, ownerRecord.SceneNodeId, componentTypeId);
            long previousSceneNodeId = componentRecord.SceneNodeId;
            componentRecord.Kind = EntityNodeKind.ComponentEntity;
            componentRecord.OwnerNodeId = ownerRecord.NodeId;
            componentRecord.SceneNodeId = ownerRecord.SceneNodeId;
            componentRecord.EntityId = component.Id;
            componentRecord.ComponentTypeId = componentTypeId;
            Nodes.SetNode(componentRecord);
            Nodes.AttachComponent(ownerRecord.NodeId, componentRecord.NodeId, componentTypeId);

            component.IsComponent = true;
            PropagateScene(component, ownerRecord.SceneNodeId, owner.SceneRoot, previousSceneNodeId);
            if (component.IsTreePublished)
            {
                _world.EntityEvents.NotifyEntityParentChanged(component, oldParent, owner);
            }
        }

        public Entity GetOwner(Entity entity)
        {
            if (!TryGetNode(entity, out var record) || record.OwnerNodeId == 0)
            {
                return null;
            }

            return Objects.TryGet(record.OwnerNodeId, out var owner) ? owner : null;
        }

        public K GetChild<K>(Entity owner, long id) where K : Entity
        {
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return null;
            }

            if (!Nodes.TryGetChild(ownerRecord.NodeId, id, out var childNodeId))
            {
                return null;
            }

            return Objects.TryGet(childNodeId, out var child) ? child as K : null;
        }

        public int GetChildrenCount(Entity owner)
        {
            return TryGetNode(owner, out var ownerRecord) ? Nodes.GetChildrenCount(ownerRecord.NodeId) : 0;
        }

        public int GetComponentsCount(Entity owner)
        {
            return TryGetNode(owner, out var ownerRecord) ? Nodes.GetComponentsCount(ownerRecord.NodeId) : 0;
        }

        public IReadOnlyCollection<Entity> GetAllChildren(Entity owner)
        {
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return Array.Empty<Entity>();
            }

            return Nodes.GetChildren(ownerRecord.NodeId)
                .Select(nodeId => Objects.TryGet(nodeId, out var entity) ? entity : null)
                .Where(entity => entity != null)
                .ToList();
        }

        public IReadOnlyCollection<Entity> GetAllComponents(Entity owner)
        {
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return Array.Empty<Entity>();
            }

            return Nodes.GetComponents(ownerRecord.NodeId)
                .Select(nodeId => Objects.TryGet(nodeId, out var entity) ? entity : null)
                .Where(entity => entity != null)
                .ToList();
        }

        public SortedDictionary<long, Entity> BuildChildrenSnapshot(Entity owner)
        {
            var result = new SortedDictionary<long, Entity>();
            foreach (var child in GetAllChildren(owner))
            {
                result[child.Id] = child;
            }

            return result;
        }

        public SortedDictionary<long, Entity> BuildComponentsSnapshot(Entity owner)
        {
            var result = new SortedDictionary<long, Entity>();
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return result;
            }

            foreach (var componentNodeId in Nodes.GetComponents(ownerRecord.NodeId))
            {
                if (!Objects.TryGet(componentNodeId, out var component))
                {
                    continue;
                }

                result[owner.GetLongHashCode(component.GetType())] = component;
            }

            return result;
        }

        public K GetComponent<K>(Entity owner) where K : Entity
        {
            return GetComponent(owner, typeof(K)) as K;
        }

        public Entity GetComponent(Entity owner, Type type)
        {
            if (type == null || !TryGetNode(owner, out var ownerRecord))
            {
                return null;
            }

            var componentTypeId = owner.GetLongHashCode(type);
            if (!Nodes.TryGetComponent(ownerRecord.NodeId, componentTypeId, out var componentNodeId))
            {
                return null;
            }

            return Objects.TryGet(componentNodeId, out var component) ? component : null;
        }

        public bool HasComponent(Entity owner, Type type)
        {
            if (type == null || !TryGetNode(owner, out var ownerRecord))
            {
                return false;
            }

            return Nodes.TryGetComponent(ownerRecord.NodeId, owner.GetLongHashCode(type), out _);
        }

        public void ClearChildren(Entity owner)
        {
            foreach (var child in GetAllChildren(owner).ToList())
            {
                DestroySubtree(child);
            }
        }

        public void RemoveChild(Entity owner, long id)
        {
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return;
            }

            if (!Nodes.TryGetChild(ownerRecord.NodeId, id, out var childNodeId))
            {
                return;
            }

            if (Objects.TryGet(childNodeId, out var child))
            {
                DestroySubtree(child);
            }
        }

        public void RemoveComponent(Entity owner, Type type)
        {
            if (type == null || !TryGetNode(owner, out var ownerRecord))
            {
                return;
            }

            var componentTypeId = owner.GetLongHashCode(type);
            if (!Nodes.TryGetComponent(ownerRecord.NodeId, componentTypeId, out var componentNodeId))
            {
                return;
            }

            if (Objects.TryGet(componentNodeId, out var component))
            {
                DestroySubtree(component);
            }
        }

        public void RemoveComponent(Entity owner, Entity component)
        {
            if (component == null || !TryGetNode(component, out var componentRecord))
            {
                return;
            }

            if (!TryGetNode(owner, out var ownerRecord) || componentRecord.OwnerNodeId != ownerRecord.NodeId)
            {
                return;
            }

            DestroySubtree(component);
        }

        public void DestroySubtree(Entity entity)
        {
            if (entity == null || entity.IsDestroyed)
            {
                return;
            }

            if (!TryGetNode(entity, out var record))
            {
                entity.DestroySelfFromHierarchy();
                return;
            }

            if (record.IsDestroying)
            {
                return;
            }

            DestroySubtreeCore(entity, record);
        }

        public bool TryResolve<T>(EntityHandle handle, out T entity) where T : Entity
        {
            entity = null;
            if (!Nodes.TryGetNode(handle, out var record))
            {
                return false;
            }

            if (!Objects.TryGet(record.NodeId, out var resolved) || resolved.IsDestroyed)
            {
                return false;
            }

            entity = resolved as T;
            return entity != null;
        }

        public bool TryGetNode(Entity entity, out EntityNode record)
        {
            if (entity == null)
            {
                record = default;
                return false;
            }

            return Nodes.TryGetNode(entity.HierarchyHandle, out record);
        }

        public long GetSceneNodeId(Entity entity)
        {
            return TryGetNode(entity, out var record) ? record.SceneNodeId : 0;
        }

        public Scene GetSceneRoot(Entity entity)
        {
            if (!TryGetNode(entity, out var record) || record.SceneNodeId == 0)
            {
                return null;
            }

            return Objects.TryGet(record.SceneNodeId, out var sceneRoot) ? sceneRoot as Scene : null;
        }

        public EntitySnapshot CaptureSnapshot()
        {
            return new EntitySnapshotBuilder(this).Capture();
        }

        public EntityValidationResult Validate()
        {
            return new EntityValidator(this).Validate();
        }

        public IReadOnlyList<Entity> GetPublishedEntitiesParentFirst()
        {
            return Nodes.GetAllNodes()
                .OrderBy(GetHierarchyDepth)
                .ThenBy(record => record.NodeId)
                .Select(record => Objects.TryGet(record.NodeId, out var entity) ? entity : null)
                .Where(entity => entity != null && entity.IsTreePublished && !entity.IsDestroyed)
                .ToList();
        }

        public int GetHierarchyDepth(Entity entity)
        {
            return TryGetNode(entity, out var record) ? GetHierarchyDepth(record) : int.MaxValue;
        }

        public void ValidateChildPlacement(Entity owner, Type entityType)
        {
            EntityPlacementMetadata.ValidateChild(owner, entityType);
        }

        public void ValidateComponentPlacement(Type entityType)
        {
            EntityPlacementMetadata.ValidateComponent(entityType);
        }

        public void Dispose()
        {
            var sceneRoots = Nodes.GetSceneRoots()
                .Select(nodeId => Objects.TryGet(nodeId, out var scene) ? scene : null)
                .Where(scene => scene != null)
                .ToList();

            try
            {
                foreach (var scene in sceneRoots)
                {
                    DestroySubtree(scene);
                }
            }
            finally
            {
                Scheduler.Clear();
                Scenes.Clear();
                Objects.Clear();
                Nodes.Clear();
            }
        }

        private void DestroySubtreeCore(Entity entity, EntityNode record)
        {
            EntityHandle handle = record.Handle;
            EntityNodeKind kind = record.Kind;
            try
            {
                Nodes.SetDestroying(record.NodeId);
                record = Nodes.GetNode(record.NodeId);

                TryBeginComponentRemoval(entity, record);
                if (record.Kind == EntityNodeKind.ChildEntity)
                {
                    Nodes.DetachFromOwner(record);
                }

                entity.BeginDestroyFromHierarchy();
                Nodes.SetInstanceId(record.NodeId, entity.InstanceId);

                foreach (var child in GetAllChildren(entity).ToList())
                {
                    DestroySubtree(child);
                }

                foreach (var component in GetAllComponents(entity).ToList())
                {
                    DestroySubtree(component);
                }

                entity.DestroySelfFromHierarchy();
            }
            finally
            {
                if (kind == EntityNodeKind.SceneRoot)
                {
                    Scheduler.RemoveScene(record.NodeId);
                    Scenes.Unregister(record.NodeId);
                    if (entity is Scene scene)
                    {
                        _world.UnregisterScene(scene);
                    }
                }
                else
                {
                    Scheduler.Unregister(handle);
                }

                Objects.Remove(record.NodeId);
                Nodes.RemoveNode(record.NodeId);
                entity.ClearHierarchyHandle();
            }
        }

        private void TryBeginComponentRemoval(Entity entity, EntityNode record)
        {
            if (record.Kind != EntityNodeKind.ComponentEntity || record.OwnerNodeId == 0)
            {
                return;
            }

            if (!Objects.TryGet(record.OwnerNodeId, out var owner) || owner.IsDestroyed)
            {
                Nodes.DetachFromOwner(record);
                return;
            }

            if (Nodes.TryGetNode(record.OwnerNodeId, out var ownerRecord) && ownerRecord.IsDestroying)
            {
                Nodes.DetachFromOwner(record);
                return;
            }

            bool isIndexed = Nodes.TryGetComponent(record.OwnerNodeId, record.ComponentTypeId, out var indexedNodeId) &&
                             indexedNodeId == record.NodeId;
            if (!isIndexed)
            {
                return;
            }

            Nodes.DetachFromOwner(record);
        }

        private EntityNode EnsureNode(Entity entity, EntityNodeKind kind, long ownerNodeId, long sceneNodeId, long componentTypeId)
        {
            if (TryGetNode(entity, out var record))
            {
                return record;
            }

            var handle = Nodes.CreateNode(entity, kind, ownerNodeId, sceneNodeId, componentTypeId);
            entity.AssignHierarchyHandle(this, handle);
            Objects.Add(handle, entity);
            return Nodes.GetNode(handle.NodeId);
        }

        private EntityNode RequireNode(Entity entity)
        {
            if (!TryGetNode(entity, out var record))
            {
                throw new Exception($"entity is not attached to entity hierarchy: {entity.GetType().FullName}");
            }

            return record;
        }

        private void ValidateOwner(Entity owner, Entity child)
        {
            if (owner == null)
            {
                throw new Exception($"cant set parent null: {child?.GetType().FullName}");
            }

            if (child == null)
            {
                throw new Exception("cant attach null entity");
            }

            if (owner == child)
            {
                throw new Exception($"cant set parent self: {child.GetType().FullName}");
            }

            if (owner.IsDestroyed)
            {
                throw new Exception($"cant attach to destroyed owner: {owner.GetType().FullName}");
            }

            if (owner.SceneRoot == null)
            {
                throw new Exception($"cant set parent because parent domain is null: {child.GetType().FullName} {owner.GetType().FullName}");
            }

            if (TryGetNode(child, out var childRecord))
            {
                if (childRecord.Kind == EntityNodeKind.SceneRoot)
                {
                    throw new Exception($"cant attach scene root to owner: {child.GetType().FullName}");
                }

                if (childRecord.IsDestroying)
                {
                    throw new Exception($"cant attach destroying entity: {child.GetType().FullName}");
                }

                if (WouldCreateOwnerCycle(owner, childRecord.NodeId))
                {
                    throw new Exception($"cant attach owner descendant as parent: {child.GetType().FullName} -> {owner.GetType().FullName}");
                }
            }
        }

        private void DetachExistingOwnerIndex(Entity entity)
        {
            if (!TryGetNode(entity, out var record) || record.OwnerNodeId == 0)
            {
                return;
            }

            Nodes.DetachFromOwner(record);
        }

        private void PropagateScene(Entity root, long sceneNodeId, Scene scene, long previousSceneNodeIdOverride = -1)
        {
            long previousSceneNodeId = previousSceneNodeIdOverride >= 0
                ? previousSceneNodeIdOverride
                : GetSceneNodeId(root);
            root.SetSceneFromHierarchy(scene);
            if (TryGetNode(root, out var rootRecord))
            {
                rootRecord.SceneNodeId = sceneNodeId;
                rootRecord.InstanceId = root.InstanceId;
                Nodes.SetNode(rootRecord);
                Scheduler.MoveIfRegistered(rootRecord.Handle, previousSceneNodeId, sceneNodeId);
            }

            foreach (var child in GetAllChildren(root))
            {
                PropagateScene(child, sceneNodeId, scene);
            }

            foreach (var component in GetAllComponents(root))
            {
                PropagateScene(component, sceneNodeId, scene);
            }
        }

        private long TryGetNodeId(Entity entity)
        {
            return TryGetNode(entity, out var record) ? record.NodeId : 0;
        }

        private int GetHierarchyDepth(EntityNode record)
        {
            int depth = 0;
            var visited = new HashSet<long>();
            long ownerNodeId = record.OwnerNodeId;
            while (ownerNodeId != 0 && visited.Add(ownerNodeId) && Nodes.TryGetNode(ownerNodeId, out var ownerRecord))
            {
                depth++;
                ownerNodeId = ownerRecord.OwnerNodeId;
            }

            return depth;
        }

        private bool WouldCreateOwnerCycle(Entity owner, long childNodeId)
        {
            if (!TryGetNode(owner, out var ownerRecord))
            {
                return false;
            }

            return ownerRecord.NodeId == childNodeId || OwnerChainContains(ownerRecord.OwnerNodeId, childNodeId);
        }

        private bool OwnerChainContains(long startOwnerNodeId, long targetNodeId)
        {
            var visited = new HashSet<long>();
            long currentNodeId = startOwnerNodeId;
            while (currentNodeId != 0)
            {
                if (currentNodeId == targetNodeId)
                {
                    return true;
                }

                if (!visited.Add(currentNodeId) || !Nodes.TryGetNode(currentNodeId, out var currentRecord))
                {
                    return false;
                }

                currentNodeId = currentRecord.OwnerNodeId;
            }

            return false;
        }

    }
}
