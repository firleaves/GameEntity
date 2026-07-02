using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class EntityHierarchy : IDisposable
    {
        private readonly World _world;

        public NodeStore Nodes { get; } = new NodeStore();

        public ObjectStore Objects { get; } = new ObjectStore();

        public ComponentIndexStore Components { get; } = new ComponentIndexStore();

        public SceneRegistry Scenes { get; } = new SceneRegistry();

        public EntityScheduler Scheduler { get; }

        public EntityHierarchy(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Scheduler = new EntityScheduler(this);
        }

        internal World World => _world;

        public void RegisterSceneRoot(Scene scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (TryGetRecord(scene, out var existing))
            {
                scene.SetSceneFromGraph(scene);
                Scenes.Register(scene.Name, existing.NodeId);
                return;
            }

            scene.SetSceneFromGraph(scene);
            var handle = Nodes.CreateNode(scene, NodeKind.SceneRoot, 0, 0, 0);
            var record = Nodes.GetRecord(handle.NodeId);
            record.SceneNodeId = handle.NodeId;
            Nodes.SetRecord(record);

            scene.AssignGraphHandle(this, handle);
            Objects.Add(handle, scene);
            Scenes.Register(scene.Name, handle.NodeId);
        }

        public void AttachChild(Entity owner, Entity child)
        {
            ValidateOwner(owner, child);

            var ownerRecord = RequireRecord(owner);
            var oldParent = GetOwner(child);
            if (oldParent == owner && TryGetRecord(child, out var sameRecord) && sameRecord.Kind == NodeKind.ChildEntity)
            {
                Log.Error($"重复设置了Parent: {child.GetType().FullName} parent: {owner.GetType().FullName}");
                return;
            }

            if (Nodes.HasChild(ownerRecord.NodeId, child.Id, TryGetNodeId(child)))
            {
                throw new Exception($"entity already has child id: {child.Id}");
            }

            DetachExistingOwnerIndex(child);

            var childRecord = EnsureNode(child, NodeKind.ChildEntity, ownerRecord.NodeId, ownerRecord.SceneNodeId, 0);
            int previousSceneNodeId = childRecord.SceneNodeId;
            childRecord.Kind = NodeKind.ChildEntity;
            childRecord.OwnerNodeId = ownerRecord.NodeId;
            childRecord.SceneNodeId = ownerRecord.SceneNodeId;
            childRecord.BusinessId = child.Id;
            childRecord.TypeId = 0;
            Nodes.SetRecord(childRecord);
            Nodes.AttachChild(ownerRecord.NodeId, childRecord.NodeId, child.Id);

            child.IsComponent = false;
            PropagateScene(child, ownerRecord.SceneNodeId, owner.SceneRoot, previousSceneNodeId);
            EntityTreeEventHub.NotifyEntityParentChanged(child, oldParent, owner);
        }

        public void AttachComponent(Entity owner, Entity component)
        {
            ValidateOwner(owner, component);

            var ownerRecord = RequireRecord(owner);
            var type = component.GetType();
            var typeId = owner.GetLongHashCode(type);
            var oldParent = GetOwner(component);

            if (oldParent == owner && TryGetRecord(component, out var sameRecord) && sameRecord.Kind == NodeKind.ComponentEntity)
            {
                Log.Error($"重复设置了Parent: {component.GetType().FullName} parent: {owner.GetType().FullName}");
                return;
            }

            if (Components.TryGet(ownerRecord.NodeId, typeId, out var existingNodeId) && existingNodeId != TryGetNodeId(component))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            DetachExistingOwnerIndex(component);

            var componentRecord = EnsureNode(component, NodeKind.ComponentEntity, ownerRecord.NodeId, ownerRecord.SceneNodeId, typeId);
            int previousSceneNodeId = componentRecord.SceneNodeId;
            componentRecord.Kind = NodeKind.ComponentEntity;
            componentRecord.OwnerNodeId = ownerRecord.NodeId;
            componentRecord.SceneNodeId = ownerRecord.SceneNodeId;
            componentRecord.BusinessId = component.Id;
            componentRecord.TypeId = typeId;
            Nodes.SetRecord(componentRecord);
            Nodes.AttachComponent(ownerRecord.NodeId, componentRecord.NodeId, typeId);
            Components.Add(ownerRecord.NodeId, typeId, componentRecord.NodeId);

            component.IsComponent = true;
            PropagateScene(component, ownerRecord.SceneNodeId, owner.SceneRoot, previousSceneNodeId);
            EntityTreeEventHub.NotifyEntityParentChanged(component, oldParent, owner);
        }

        public Entity GetOwner(Entity entity)
        {
            if (!TryGetRecord(entity, out var record) || record.OwnerNodeId == 0)
            {
                return null;
            }

            return Objects.TryGet(record.OwnerNodeId, out var owner) ? owner : null;
        }

        public K GetChild<K>(Entity owner, long id) where K : Entity
        {
            if (!TryGetRecord(owner, out var ownerRecord))
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
            return TryGetRecord(owner, out var ownerRecord) ? Nodes.GetChildrenCount(ownerRecord.NodeId) : 0;
        }

        public int GetComponentsCount(Entity owner)
        {
            return TryGetRecord(owner, out var ownerRecord) ? Nodes.GetComponentsCount(ownerRecord.NodeId) : 0;
        }

        public IReadOnlyCollection<Entity> GetAllChildren(Entity owner)
        {
            if (!TryGetRecord(owner, out var ownerRecord))
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
            if (!TryGetRecord(owner, out var ownerRecord))
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
            if (!TryGetRecord(owner, out var ownerRecord))
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
            var exact = GetComponent(owner, typeof(K));
            if (exact != null)
            {
                return (K)exact;
            }

            foreach (var component in GetAllComponents(owner))
            {
                if (component is K derivedMatch)
                {
                    return derivedMatch;
                }
            }

            return null;
        }

        public Entity GetComponent(Entity owner, Type type)
        {
            if (type == null || !TryGetRecord(owner, out var ownerRecord))
            {
                return null;
            }

            var typeId = owner.GetLongHashCode(type);
            if (!Components.TryGet(ownerRecord.NodeId, typeId, out var componentNodeId))
            {
                return null;
            }

            return Objects.TryGet(componentNodeId, out var component) ? component : null;
        }

        public bool HasComponent(Entity owner, Type type)
        {
            if (type == null || !TryGetRecord(owner, out var ownerRecord))
            {
                return false;
            }

            return Components.TryGet(ownerRecord.NodeId, owner.GetLongHashCode(type), out _);
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
            if (!TryGetRecord(owner, out var ownerRecord))
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
            if (type == null || !TryGetRecord(owner, out var ownerRecord))
            {
                return;
            }

            var typeId = owner.GetLongHashCode(type);
            if (!Components.TryGet(ownerRecord.NodeId, typeId, out var componentNodeId))
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
            if (component == null || !TryGetRecord(component, out var componentRecord))
            {
                return;
            }

            if (!TryGetRecord(owner, out var ownerRecord) || componentRecord.OwnerNodeId != ownerRecord.NodeId)
            {
                return;
            }

            DestroySubtree(component);
        }

        public void DestroySubtree(Entity entity)
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            if (!TryGetRecord(entity, out var record))
            {
                entity.DisposeSelfFromGraph();
                return;
            }

            if (record.IsDisposing)
            {
                return;
            }

            DestroySubtreeCore(entity, record);
        }

        public bool TryResolve<T>(EntityHandle handle, out T entity) where T : Entity
        {
            entity = null;
            if (!Nodes.TryGetRecord(handle, out var record))
            {
                return false;
            }

            if (!Objects.TryGet(record.NodeId, out var resolved) || resolved.IsDisposed)
            {
                return false;
            }

            entity = resolved as T;
            return entity != null;
        }

        public bool TryGetRecord(Entity entity, out NodeRecord record)
        {
            if (entity == null)
            {
                record = default;
                return false;
            }

            return Nodes.TryGetRecord(entity.GraphHandle, out record);
        }

        public int GetSceneNodeId(Entity entity)
        {
            return TryGetRecord(entity, out var record) ? record.SceneNodeId : 0;
        }

        public Scene GetSceneRoot(Entity entity)
        {
            if (!TryGetRecord(entity, out var record) || record.SceneNodeId == 0)
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

        public void Dispose()
        {
            var sceneRoots = Nodes.GetSceneRoots()
                .Select(nodeId => Objects.TryGet(nodeId, out var scene) ? scene : null)
                .Where(scene => scene != null)
                .ToList();

            foreach (var scene in sceneRoots)
            {
                DestroySubtree(scene);
            }

            Scheduler.Clear();
            Scenes.Clear();
            Components.Clear();
            Objects.Clear();
            Nodes.Clear();
        }

        private void DestroySubtreeCore(Entity entity, NodeRecord record)
        {
            Nodes.SetDisposing(record.NodeId);
            record = Nodes.GetRecord(record.NodeId);

            var componentRemoveContext = TryBeginComponentRemoval(entity, record);
            if (record.Kind == NodeKind.ChildEntity)
            {
                Nodes.DetachFromOwner(record);
            }

            entity.BeginDisposeFromGraph();
            Nodes.SetInstanceId(record.NodeId, entity.InstanceId);

            foreach (var child in GetAllChildren(entity).ToList())
            {
                DestroySubtree(child);
            }

            foreach (var component in GetAllComponents(entity).ToList())
            {
                DestroySubtree(component);
            }

            entity.DisposeSelfFromGraph();

            if (record.Kind == NodeKind.SceneRoot)
            {
                Scheduler.RemoveScene(record.NodeId);
                Scenes.Unregister(record.NodeId);
            }
            else
            {
                Scheduler.Unregister(record.Handle);
            }

            Objects.Remove(record.NodeId);
            Nodes.RemoveNode(record.NodeId);
            entity.ClearGraphHandle();

            if (componentRemoveContext.ShouldNotify)
            {
                _world.Dependencies.NotifyRemoveComponent(componentRemoveContext.Owner, componentRemoveContext.ComponentType);
            }
        }

        private ComponentRemoveContext TryBeginComponentRemoval(Entity entity, NodeRecord record)
        {
            if (record.Kind != NodeKind.ComponentEntity || record.OwnerNodeId == 0)
            {
                return ComponentRemoveContext.None;
            }

            if (!Objects.TryGet(record.OwnerNodeId, out var owner) || owner.IsDisposed)
            {
                Nodes.DetachFromOwner(record);
                Components.Remove(record.OwnerNodeId, record.TypeId, record.NodeId);
                return ComponentRemoveContext.None;
            }

            if (Nodes.TryGetRecord(record.OwnerNodeId, out var ownerRecord) && ownerRecord.IsDisposing)
            {
                Nodes.DetachFromOwner(record);
                Components.Remove(record.OwnerNodeId, record.TypeId, record.NodeId);
                return ComponentRemoveContext.None;
            }

            bool isIndexed = Components.TryGet(record.OwnerNodeId, record.TypeId, out var indexedNodeId) &&
                             indexedNodeId == record.NodeId;
            if (!isIndexed)
            {
                return ComponentRemoveContext.None;
            }

            UnregisterDependentComponent(entity);
            Nodes.DetachFromOwner(record);
            Components.Remove(record.OwnerNodeId, record.TypeId, record.NodeId);
            return new ComponentRemoveContext(owner, entity.GetType(), true);
        }

        private NodeRecord EnsureNode(Entity entity, NodeKind kind, int ownerNodeId, int sceneNodeId, long typeId)
        {
            if (TryGetRecord(entity, out var record))
            {
                return record;
            }

            var handle = Nodes.CreateNode(entity, kind, ownerNodeId, sceneNodeId, typeId);
            entity.AssignGraphHandle(this, handle);
            Objects.Add(handle, entity);
            return Nodes.GetRecord(handle.NodeId);
        }

        private NodeRecord RequireRecord(Entity entity)
        {
            if (!TryGetRecord(entity, out var record))
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

            if (owner.IsDisposed)
            {
                throw new Exception($"cant attach to disposed owner: {owner.GetType().FullName}");
            }

            if (owner.SceneRoot == null)
            {
                throw new Exception($"cant set parent because parent domain is null: {child.GetType().FullName} {owner.GetType().FullName}");
            }

            if (TryGetRecord(child, out var childRecord))
            {
                if (childRecord.Kind == NodeKind.SceneRoot)
                {
                    throw new Exception($"cant attach scene root to owner: {child.GetType().FullName}");
                }

                if (childRecord.IsDisposing)
                {
                    throw new Exception($"cant attach disposing entity: {child.GetType().FullName}");
                }

                if (WouldCreateOwnerCycle(owner, childRecord.NodeId))
                {
                    throw new Exception($"cant attach owner descendant as parent: {child.GetType().FullName} -> {owner.GetType().FullName}");
                }
            }
        }

        private void DetachExistingOwnerIndex(Entity entity)
        {
            if (!TryGetRecord(entity, out var record) || record.OwnerNodeId == 0)
            {
                return;
            }

            Nodes.DetachFromOwner(record);
            if (record.Kind == NodeKind.ComponentEntity)
            {
                Components.Remove(record.OwnerNodeId, record.TypeId, record.NodeId);
            }
        }

        private void PropagateScene(Entity root, int sceneNodeId, Scene scene, int previousSceneNodeIdOverride = -1)
        {
            int previousSceneNodeId = previousSceneNodeIdOverride >= 0
                ? previousSceneNodeIdOverride
                : GetSceneNodeId(root);
            root.SetSceneFromGraph(scene);
            if (TryGetRecord(root, out var rootRecord))
            {
                rootRecord.SceneNodeId = sceneNodeId;
                rootRecord.InstanceId = root.InstanceId;
                Nodes.SetRecord(rootRecord);
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

        private void UnregisterDependentComponent(Entity component)
        {
            var registry = _world.Dependencies;
            if (component is IDependentComponent ||
                component.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true).Length > 0)
            {
                registry.UnregisterDependentComponent(component);
            }
        }

        private int TryGetNodeId(Entity entity)
        {
            return TryGetRecord(entity, out var record) ? record.NodeId : 0;
        }

        private bool WouldCreateOwnerCycle(Entity owner, int childNodeId)
        {
            if (!TryGetRecord(owner, out var ownerRecord))
            {
                return false;
            }

            return ownerRecord.NodeId == childNodeId || OwnerChainContains(ownerRecord.OwnerNodeId, childNodeId);
        }

        private bool OwnerChainContains(int startOwnerNodeId, int targetNodeId)
        {
            var visited = new HashSet<int>();
            int currentNodeId = startOwnerNodeId;
            while (currentNodeId != 0)
            {
                if (currentNodeId == targetNodeId)
                {
                    return true;
                }

                if (!visited.Add(currentNodeId) || !Nodes.TryGetRecord(currentNodeId, out var currentRecord))
                {
                    return false;
                }

                currentNodeId = currentRecord.OwnerNodeId;
            }

            return false;
        }

        private readonly struct ComponentRemoveContext
        {
            public static readonly ComponentRemoveContext None = new ComponentRemoveContext(null, null, false);

            public ComponentRemoveContext(Entity owner, Type componentType, bool shouldNotify)
            {
                Owner = owner;
                ComponentType = componentType;
                ShouldNotify = shouldNotify;
            }

            public Entity Owner { get; }

            public Type ComponentType { get; }

            public bool ShouldNotify { get; }
        }
    }
}
