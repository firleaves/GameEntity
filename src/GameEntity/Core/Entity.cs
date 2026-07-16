using System;
using System.Collections.Generic;

namespace GameEntity
{
    [Flags]
    internal enum EntityStatus : byte
    {
        None = 0,
        IsFromPool = 1,
        IsRegister = 1 << 1,
        IsComponent = 1 << 2,
        IsCreated = 1 << 3,
        IsNew = 1 << 4,
        IsTreePublished = 1 << 5,
    }

    public class Entity : IPool
    {
        /// <summary>
        /// 对比两个Entity是否是同一个实体
        /// </summary>
        public long InstanceId { get; private set; }

        /// <summary>
        /// 实体的唯一ID
        /// </summary>
        public long Id { get; private set; }

        private EntityStatus _status = EntityStatus.None;
        private string _viewName;
        private Scene _scene;
        private EntityHierarchy _hierarchy;
        private bool _hierarchyDestroyCompleted;

        internal EntityHandle HierarchyHandle { get; private set; } = EntityHandle.None;

        public EntityHandle Handle => HierarchyHandle;

        internal bool IsFromPool
        {
            get => (_status & EntityStatus.IsFromPool) == EntityStatus.IsFromPool;
            set
            {
                if (value)
                {
                    _status |= EntityStatus.IsFromPool;
                }
                else
                {
                    _status &= ~EntityStatus.IsFromPool;
                }
            }
        }

        bool IPool.IsFromPool
        {
            get => IsFromPool;
            set => IsFromPool = value;
        }

        protected bool IsRegister
        {
            get => (_status & EntityStatus.IsRegister) == EntityStatus.IsRegister;
            set
            {
                if (IsRegister == value)
                {
                    return;
                }

                if (value)
                {
                    _status |= EntityStatus.IsRegister;
                }
                else
                {
                    _status &= ~EntityStatus.IsRegister;
                }

            }
        }

        internal bool IsTreePublished => (_status & EntityStatus.IsTreePublished) == EntityStatus.IsTreePublished;

        internal bool IsComponent
        {
            get => (_status & EntityStatus.IsComponent) == EntityStatus.IsComponent;
            set
            {
                if (value)
                {
                    _status |= EntityStatus.IsComponent;
                }
                else
                {
                    _status &= ~EntityStatus.IsComponent;
                }
            }
        }

        protected bool IsCreated
        {
            get => (_status & EntityStatus.IsCreated) == EntityStatus.IsCreated;
            set
            {
                if (value)
                {
                    _status |= EntityStatus.IsCreated;
                }
                else
                {
                    _status &= ~EntityStatus.IsCreated;
                }
            }
        }

        protected bool IsNew
        {
            get => (_status & EntityStatus.IsNew) == EntityStatus.IsNew;
            set
            {
                if (value)
                {
                    _status |= EntityStatus.IsNew;
                }
                else
                {
                    _status &= ~EntityStatus.IsNew;
                }
            }
        }

        protected virtual string ViewName
        {
            get
            {
                if (string.IsNullOrEmpty(_viewName))
                {
                    _viewName = GetType().FullName;
                }
                return _viewName;
            }
            set
            {
                _viewName = value;
            }
        }

        public string GetViewName()
        {
            return ViewName;
        }

        public bool IsDestroyed => InstanceId == 0;

        // 父实体。V2 中父关系由 EntityHierarchy 维护，这里只做 façade 查询和挂接入口。
        public Entity Parent
        {
            get => _hierarchy?.GetOwner(this);
            internal set => AttachToParent(value);
        }

        public Entity Owner => Parent;

        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {GetType().FullName}");
                }

                ResolveHierarchy(value).AttachComponent(value, this);
            }
        }

        internal Scene SceneRoot => _scene;

        /// <summary>
        /// 当前实体的子实体只读快照。
        /// </summary>
        public IReadOnlyCollection<Entity> Children => GetAllChildren();

        /// <summary>
        /// 当前实体的组件只读快照。
        /// </summary>
        public IReadOnlyCollection<Entity> Components => GetAllComponents();

        protected Entity()
        {
        }

        protected virtual void RegisterSystem()
        {
        }

        internal void CompleteRegistration()
        {
            RegisterSystem();
        }

        internal void MarkTreePublished()
        {
            _status |= EntityStatus.IsTreePublished;
        }

        public int ComponentsCount()
        {
            return _hierarchy?.GetComponentsCount(this) ?? 0;
        }

        public int ChildrenCount()
        {
            return _hierarchy?.GetChildrenCount(this) ?? 0;
        }

        /// <summary>
        /// 获取当前实体挂载的所有组件。
        /// </summary>
        public IReadOnlyCollection<Entity> GetAllComponents()
        {
            return _hierarchy?.GetAllComponents(this) ?? Array.Empty<Entity>();
        }

        /// <summary>
        /// 获取当前实体的所有子实体。
        /// </summary>
        public IReadOnlyCollection<Entity> GetAllChildren()
        {
            return _hierarchy?.GetAllChildren(this) ?? Array.Empty<Entity>();
        }

        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (_hierarchy != null)
            {
                _hierarchy.DestroySubtree(this);
                return;
            }

            BeginDestroyFromHierarchy();
            DestroySelfFromHierarchy();
        }

        /// <summary>
        /// 派生类可重写的内部销毁钩子。外部业务释放统一调用 Destroy()。
        /// </summary>
        protected virtual void OnDestroyInternal()
        {
        }

        public K GetChild<K>(long id) where K : Entity
        {
            return _hierarchy?.GetChild<K>(this, id);
        }

        public bool TryGetChild<K>(long id, out K child) where K : Entity
        {
            child = GetChild<K>(id);
            return child != null;
        }

        public bool ContainsChild(long id)
        {
            return GetChild<Entity>(id) != null;
        }

        public void ClearChildren()
        {
            _hierarchy?.ClearChildren(this);
        }

        public void RemoveChild(long id)
        {
            if (IsDestroyed)
            {
                return;
            }

            _hierarchy?.RemoveChild(this, id);
        }

        public void RemoveComponent<K>() where K : Entity
        {
            RemoveComponent(typeof(K));
        }

        public void RemoveComponent(Type type)
        {
            if (IsDestroyed)
            {
                return;
            }

            _hierarchy?.RemoveComponent(this, type);
        }

        private void RemoveComponent(Entity component)
        {
            if (IsDestroyed)
            {
                return;
            }

            _hierarchy?.RemoveComponent(this, component);
        }

        public K GetComponent<K>() where K : Entity
        {
            return _hierarchy?.GetComponent(this, typeof(K)) as K;
        }

        public Entity GetComponent(Type type)
        {
            return _hierarchy?.GetComponent(this, type);
        }

        public bool TryGetComponent<T>(out T component) where T : Entity
        {
            component = GetComponent<T>();
            return component != null;
        }

        public bool TryGetComponent(Type type, out Entity component)
        {
            component = GetComponent(type);
            return component != null;
        }

        public bool ContainsComponent<T>() where T : Entity
        {
            return ContainsComponent(typeof(T));
        }

        public bool ContainsComponent(Type type)
        {
            return _hierarchy?.HasComponent(this, type) ?? false;
        }

        public Scene GetSceneRoot()
        {
            return _hierarchy?.GetSceneRoot(this);
        }

        public bool TryGetSceneRoot(out Scene scene)
        {
            scene = GetSceneRoot();
            return scene != null;
        }

        public T FindOwner<T>() where T : Entity
        {
            TryFindOwner<T>(out var owner);
            return owner;
        }

        public bool TryGetOwner<T>(out T owner) where T : Entity
        {
            owner = Parent as T;
            return owner != null;
        }

        public bool TryFindOwner<T>(out T owner) where T : Entity
        {
            Entity current = Parent;
            while (current != null)
            {
                if (current is T typedOwner)
                {
                    owner = typedOwner;
                    return true;
                }

                current = current.Parent;
            }

            owner = null;
            return false;
        }

        public T GetComponentInParent<T>() where T : Entity
        {
            return Parent?.GetComponent<T>();
        }

        public bool TryGetComponentInParent<T>(out T component) where T : Entity
        {
            component = GetComponentInParent<T>();
            return component != null;
        }

        public T GetComponentInAncestors<T>() where T : Entity
        {
            Entity current = Parent;
            while (current != null)
            {
                var component = current.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }

                current = current.Parent;
            }

            return null;
        }

        public bool TryGetComponentInAncestors<T>(out T component) where T : Entity
        {
            component = GetComponentInAncestors<T>();
            return component != null;
        }

        public T GetSiblingComponent<T>() where T : Entity
        {
            return Parent?.GetComponent<T>();
        }

        public bool TryGetSiblingComponent<T>(out T component) where T : Entity
        {
            component = GetSiblingComponent<T>();
            return component != null;
        }

        public void ReparentTo(Entity newOwner)
        {
            if (IsDestroyed || _hierarchy == null || !HierarchyHandle.IsValid || !_hierarchy.TryGetNode(this, out _))
            {
                throw new InvalidOperationException(
                    $"Cannot reparent an Entity that is destroyed or is not attached to the active hierarchy: {GetType().FullName}.");
            }

            if (newOwner == null || newOwner.IsDestroyed ||
                newOwner._hierarchy == null || !ReferenceEquals(_hierarchy, newOwner._hierarchy))
            {
                throw new InvalidOperationException(
                    $"Cannot reparent {GetType().FullName} to an invalid owner or a different hierarchy.");
            }

            _hierarchy.World.ThrowIfNotActive();
            if (IsComponent && UpdateRequirementMetadata.GetRequirementTypes(GetType()).Length > 0)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName} uses RequireForUpdate and must remain attached as a Component.");
            }

            AttachToParent(newOwner);
        }

        private void AttachToParent(Entity owner)
        {
            if (owner == null)
            {
                throw new Exception($"cant set parent null: {GetType().FullName}");
            }

            ResolveHierarchy(owner).AttachChild(owner, this);
        }

        internal static Entity Create(World world, Type type, bool isFromPool)
        {
            world.ThrowIfNotActive();
            Entity entity;
            if (isFromPool)
            {
                entity = (Entity)world.ObjectPool.Fetch(type);
            }
            else
            {
                entity = Activator.CreateInstance(type) as Entity;
            }

            entity.ResetHierarchyStateForCreate(isFromPool);
            return entity;
        }

        public K AddComponentWithId<K>(long id) where K : Entity, IAwake, new()
        {
            return AddComponentWithIdCore<K>(id, false);
        }

        private K AddComponentWithIdCore<K>(long id, bool isFromPool) where K : Entity, IAwake, new()
        {
            Type type = typeof(K);
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            world.Hierarchy.ValidateComponentPlacement(type);
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return CompleteCreation(component, world, () => world.Lifecycle.Awake(component)) as K;
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1) where K : Entity, IAwake<P1>, new()
        {
            return AddComponentWithIdCore<K, P1>(id, p1, false);
        }

        private K AddComponentWithIdCore<K, P1>(long id, P1 p1, bool isFromPool) where K : Entity, IAwake<P1>, new()
        {
            Type type = typeof(K);
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            world.Hierarchy.ValidateComponentPlacement(type);
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return CompleteCreation(component, world, () => world.Lifecycle.Awake(component, p1)) as K;
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2) where K : Entity, IAwake<P1, P2>, new()
        {
            return AddComponentWithIdCore<K, P1, P2>(id, p1, p2, false);
        }

        private K AddComponentWithIdCore<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool) where K : Entity, IAwake<P1, P2>, new()
        {
            Type type = typeof(K);
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            world.Hierarchy.ValidateComponentPlacement(type);
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return CompleteCreation(component, world, () => world.Lifecycle.Awake(component, p1, p2)) as K;
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return AddComponentWithIdCore<K, P1, P2, P3>(id, p1, p2, p3, false);
        }

        private K AddComponentWithIdCore<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Type type = typeof(K);
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            world.Hierarchy.ValidateComponentPlacement(type);
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return CompleteCreation(component, world, () => world.Lifecycle.Awake(component, p1, p2, p3)) as K;
        }

        public K AddComponentWithId<K, P1, P2, P3, P4>(long id, P1 p1, P2 p2, P3 p3, P4 p4) where K : Entity, IAwake<P1, P2, P3, P4>, new()
        {
            return AddComponentWithIdCore<K, P1, P2, P3, P4>(id, p1, p2, p3, p4, false);
        }

        private K AddComponentWithIdCore<K, P1, P2, P3, P4>(long id, P1 p1, P2 p2, P3 p3, P4 p4, bool isFromPool) where K : Entity, IAwake<P1, P2, P3, P4>, new()
        {
            Type type = typeof(K);
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            world.Hierarchy.ValidateComponentPlacement(type);
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return CompleteCreation(component, world, () => world.Lifecycle.Awake(component, p1, p2, p3, p4)) as K;
        }

        public K AddComponent<K>() where K : Entity, IAwake, new()
        {
            return AddComponentCore<K>(false);
        }

        public K AddPooledComponent<K>() where K : Entity, IAwake, new()
        {
            return AddComponentCore<K>(true);
        }

        private K AddComponentCore<K>(bool isFromPool) where K : Entity, IAwake, new()
        {
            return AddComponentWithIdCore<K>(Id, isFromPool);
        }

        public K AddComponent<K, P1>(P1 p1) where K : Entity, IAwake<P1>, new()
        {
            return AddComponentCore<K, P1>(p1, false);
        }

        public K AddPooledComponent<K, P1>(P1 p1) where K : Entity, IAwake<P1>, new()
        {
            return AddComponentCore<K, P1>(p1, true);
        }

        private K AddComponentCore<K, P1>(P1 p1, bool isFromPool) where K : Entity, IAwake<P1>, new()
        {
            return AddComponentWithIdCore<K, P1>(Id, p1, isFromPool);
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2) where K : Entity, IAwake<P1, P2>, new()
        {
            return AddComponentCore<K, P1, P2>(p1, p2, false);
        }

        public K AddPooledComponent<K, P1, P2>(P1 p1, P2 p2) where K : Entity, IAwake<P1, P2>, new()
        {
            return AddComponentCore<K, P1, P2>(p1, p2, true);
        }

        private K AddComponentCore<K, P1, P2>(P1 p1, P2 p2, bool isFromPool) where K : Entity, IAwake<P1, P2>, new()
        {
            return AddComponentWithIdCore<K, P1, P2>(Id, p1, p2, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return AddComponentCore<K, P1, P2, P3>(p1, p2, p3, false);
        }

        public K AddPooledComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return AddComponentCore<K, P1, P2, P3>(p1, p2, p3, true);
        }

        private K AddComponentCore<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return AddComponentWithIdCore<K, P1, P2, P3>(Id, p1, p2, p3, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3, P4>(P1 p1, P2 p2, P3 p3, P4 p4) where K : Entity, IAwake<P1, P2, P3, P4>, new()
        {
            return AddComponentCore<K, P1, P2, P3, P4>(p1, p2, p3, p4, false);
        }

        public K AddPooledComponent<K, P1, P2, P3, P4>(P1 p1, P2 p2, P3 p3, P4 p4) where K : Entity, IAwake<P1, P2, P3, P4>, new()
        {
            return AddComponentCore<K, P1, P2, P3, P4>(p1, p2, p3, p4, true);
        }

        private K AddComponentCore<K, P1, P2, P3, P4>(P1 p1, P2 p2, P3 p3, P4 p4, bool isFromPool) where K : Entity, IAwake<P1, P2, P3, P4>, new()
        {
            return AddComponentWithIdCore<K, P1, P2, P3, P4>(Id, p1, p2, p3, p4, isFromPool);
        }

        public T AddChild<T>() where T : Entity, IAwake
        {
            return AddChildCore<T>(false);
        }

        public T AddPooledChild<T>() where T : Entity, IAwake
        {
            return AddChildCore<T>(true);
        }

        private T AddChildCore<T>(bool isFromPool) where T : Entity, IAwake
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child));
        }

        public T AddChild<T, A>(A a) where T : Entity, IAwake<A>
        {
            return AddChildCore<T, A>(a, false);
        }

        public T AddPooledChild<T, A>(A a) where T : Entity, IAwake<A>
        {
            return AddChildCore<T, A>(a, true);
        }

        private T AddChildCore<T, A>(A a, bool isFromPool) where T : Entity, IAwake<A>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a));
        }

        public T AddChild<T, A, B>(A a, B b) where T : Entity, IAwake<A, B>
        {
            return AddChildCore<T, A, B>(a, b, false);
        }

        public T AddPooledChild<T, A, B>(A a, B b) where T : Entity, IAwake<A, B>
        {
            return AddChildCore<T, A, B>(a, b, true);
        }

        private T AddChildCore<T, A, B>(A a, B b, bool isFromPool) where T : Entity, IAwake<A, B>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b));
        }

        public T AddChild<T, A, B, C>(A a, B b, C c) where T : Entity, IAwake<A, B, C>
        {
            return AddChildCore<T, A, B, C>(a, b, c, false);
        }

        public T AddPooledChild<T, A, B, C>(A a, B b, C c) where T : Entity, IAwake<A, B, C>
        {
            return AddChildCore<T, A, B, C>(a, b, c, true);
        }

        private T AddChildCore<T, A, B, C>(A a, B b, C c, bool isFromPool) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b, c));
        }

        public T AddChild<T, A, B, C, D>(A a, B b, C c, D d) where T : Entity, IAwake<A, B, C, D>
        {
            return AddChildCore<T, A, B, C, D>(a, b, c, d, false);
        }

        public T AddPooledChild<T, A, B, C, D>(A a, B b, C c, D d) where T : Entity, IAwake<A, B, C, D>
        {
            return AddChildCore<T, A, B, C, D>(a, b, c, d, true);
        }

        private T AddChildCore<T, A, B, C, D>(A a, B b, C c, D d, bool isFromPool) where T : Entity, IAwake<A, B, C, D>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b, c, d));
        }

        public T AddChildWithId<T>(long id) where T : Entity, IAwake
        {
            return AddChildWithIdCore<T>(id, false);
        }

        private T AddChildWithIdCore<T>(long id, bool isFromPool) where T : Entity, IAwake
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = Create(world, type, isFromPool) as T;
            child.Id = id;
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child));
        }

        public T AddChildWithId<T, A>(long id, A a) where T : Entity, IAwake<A>
        {
            return AddChildWithIdCore<T, A>(id, a, false);
        }

        private T AddChildWithIdCore<T, A>(long id, A a, bool isFromPool) where T : Entity, IAwake<A>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a));
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b) where T : Entity, IAwake<A, B>
        {
            return AddChildWithIdCore<T, A, B>(id, a, b, false);
        }

        private T AddChildWithIdCore<T, A, B>(long id, A a, B b, bool isFromPool) where T : Entity, IAwake<A, B>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b));
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c) where T : Entity, IAwake<A, B, C>
        {
            return AddChildWithIdCore<T, A, B, C>(id, a, b, c, false);
        }

        private T AddChildWithIdCore<T, A, B, C>(long id, A a, B b, C c, bool isFromPool) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b, c));
        }

        public T AddChildWithId<T, A, B, C, D>(long id, A a, B b, C c, D d) where T : Entity, IAwake<A, B, C, D>
        {
            return AddChildWithIdCore<T, A, B, C, D>(id, a, b, c, d, false);
        }

        private T AddChildWithIdCore<T, A, B, C, D>(long id, A a, B b, C c, D d, bool isFromPool) where T : Entity, IAwake<A, B, C, D>
        {
            Type type = typeof(T);
            World world = GetWorld();
            world.Hierarchy.ValidateChildPlacement(this, type);
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            return CompleteCreation(child, world, () => world.Lifecycle.Awake(child, a, b, c, d));
        }

        private static T CompleteCreation<T>(T entity, World world, Action awake) where T : Entity
        {
            EntityHandle creationHandle = entity.Handle;
            long creationInstanceId = entity.InstanceId;
            using (world.BeginCreationScope())
            {
                try
                {
                    awake();
                    EnsureCreationLifetime(entity, world, creationHandle, creationInstanceId, "Awake");
                    entity.CompleteRegistration();
                    EnsureCreationLifetime(entity, world, creationHandle, creationInstanceId, "RegisterSystem");
                    world.Hierarchy.Scheduler.Register(entity);
                    EnsureCreationLifetime(entity, world, creationHandle, creationInstanceId, "Scheduler registration");
                    world.QueueEntityRegistration(entity);
                    return entity;
                }
                catch
                {
                    if (IsSameCreationLifetime(entity, world, creationHandle, creationInstanceId))
                    {
                        entity.Destroy();
                    }

                    throw;
                }
            }
        }

        private static void EnsureCreationLifetime<T>(
            T entity,
            World world,
            EntityHandle creationHandle,
            long creationInstanceId,
            string stage) where T : Entity
        {
            if (IsSameCreationLifetime(entity, world, creationHandle, creationInstanceId))
            {
                return;
            }

            throw new InvalidOperationException(
                $"{entity.GetType().FullName} ended or replaced its Entity lifetime during {stage}; creation cannot be committed.");
        }

        private static bool IsSameCreationLifetime<T>(
            T entity,
            World world,
            EntityHandle creationHandle,
            long creationInstanceId) where T : Entity
        {
            if (entity == null || entity.IsDestroyed || !creationHandle.IsValid || creationInstanceId == 0 ||
                entity.Handle != creationHandle || entity.InstanceId != creationInstanceId ||
                !world.Hierarchy.TryResolve(creationHandle, out Entity resolved))
            {
                return false;
            }

            return ReferenceEquals(entity, resolved);
        }

        internal protected virtual long GetLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }

        internal void AssignHierarchyHandle(EntityHierarchy hierarchy, EntityHandle handle)
        {
            _hierarchy = hierarchy;
            HierarchyHandle = handle;
            _hierarchyDestroyCompleted = false;
        }

        internal void EnsureIdentity(IdGenerator idGenerator)
        {
            if (Id == 0)
            {
                Id = idGenerator.GenerateId();
            }

            if (InstanceId == 0)
            {
                InstanceId = idGenerator.GenerateInstanceId();
            }
        }

        internal void ClearHierarchyHandle()
        {
            HierarchyHandle = EntityHandle.None;
            _hierarchy = null;
        }

        internal void BeginDestroyFromHierarchy()
        {
            IsRegister = false;
            InstanceId = 0;
        }

        internal void DestroySelfFromHierarchy()
        {
            if (_hierarchyDestroyCompleted)
            {
                return;
            }

            bool wasTreePublished = IsTreePublished;
            _hierarchyDestroyCompleted = true;
            BeginDestroyFromHierarchy();

            World world = _hierarchy?.World ?? World.Instance;
            if (this is IDestroy)
            {
                try
                {
                    world.Lifecycle.Destroy(this);
                }
                catch (Exception e)
                {
                    Log.Error($"Destroy lifecycle error: {GetType().FullName}: {e}");
                }
            }

            _scene = null;

            try
            {
                OnDestroyInternal();
            }
            catch (Exception e)
            {
                Log.Error($"Internal destroy hook error: {GetType().FullName}: {e}");
            }

            if (wasTreePublished)
            {
                world.EntityEvents.NotifyEntityDestroyed(this);
            }

            bool isFromPool = IsFromPool;
            _status = EntityStatus.None;
            IsFromPool = isFromPool;

            world.ObjectPool.Recycle(this);
        }

        internal void SetSceneFromHierarchy(Scene scene)
        {
            if (scene == null)
            {
                throw new Exception($"domain cant set null: {GetType().FullName}");
            }

            if (_scene == scene)
            {
                return;
            }

            Scene preScene = _scene;
            _scene = scene;

            if (preScene == null)
            {
                if (InstanceId == 0)
                {
                    InstanceId = GetWorld().IdGenerator.GenerateInstanceId();
                }

                IsRegister = true;
            }

            if (!IsCreated)
            {
                IsCreated = true;
            }
        }

        private void ResetHierarchyStateForCreate(bool isFromPool)
        {
            _hierarchy = null;
            HierarchyHandle = EntityHandle.None;
            _hierarchyDestroyCompleted = false;
            _scene = null;
            InstanceId = 0;
            Id = 0;

            _status = EntityStatus.None;
            IsFromPool = isFromPool;
            IsCreated = true;
            IsNew = true;
        }

        private static EntityHierarchy ResolveHierarchy(Entity owner)
        {
            return owner._hierarchy ?? World.Instance.Hierarchy;
        }

        internal World GetWorld()
        {
            return _hierarchy?.World ?? World.Instance;
        }
    }
}
