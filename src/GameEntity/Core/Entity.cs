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
    }

    public class Entity : IDisposable, IPool
    {
        /// <summary>
        /// 对比两个Entity是否是同一个实体
        /// </summary>
        public long InstanceId { get; protected set; }

        /// <summary>
        /// 实体的唯一ID
        /// </summary>
        public long Id { get; protected internal set; }

        private EntityStatus _status = EntityStatus.None;
        private string _viewName;
        private Scene _scene;
        private EntityHierarchy _hierarchy;
        private bool _hierarchyDisposeCompleted;

        internal EntityHandle GraphHandle { get; private set; } = EntityHandle.None;

        public EntityHandle Handle => GraphHandle;

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

                if (value)
                {
                    RegisterSystem();
                    EntityTreeEventHub.NotifyEntityRegistered(this);
                }
            }
        }

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

        public bool IsDisposed => InstanceId == 0;

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

                ResolveGraph(value).AttachComponent(value, this);
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

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_hierarchy != null)
            {
                _hierarchy.DestroySubtree(this);
                return;
            }

            BeginDisposeFromGraph();
            DisposeSelfFromGraph();
        }

        /// <summary>
        /// Optional hook for derived classes.
        /// NOTE: the original Unity-first code accidentally called Dispose() recursively here.
        /// In the .NET standalone copy, we keep a hook to preserve intent without recursion.
        /// </summary>
        protected virtual void OnDispose()
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
            if (IsDisposed)
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
            if (IsDisposed)
            {
                return;
            }

            _hierarchy?.RemoveComponent(this, type);
        }

        private void RemoveComponent(Entity component)
        {
            if (IsDisposed)
            {
                return;
            }

            _hierarchy?.RemoveComponent(this, component);
        }

        public K GetComponent<K>() where K : Entity
        {
            return _hierarchy?.GetComponent<K>(this);
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
            AttachToParent(newOwner);
        }

        private void AttachToParent(Entity owner)
        {
            if (owner == null)
            {
                throw new Exception($"cant set parent null: {GetType().FullName}");
            }

            ResolveGraph(owner).AttachChild(owner, this);
        }

        internal static Entity Create(World world, Type type, bool isFromPool)
        {
            Entity entity;
            if (isFromPool)
            {
                entity = (Entity)world.ObjectPool.Fetch(type);
            }
            else
            {
                entity = Activator.CreateInstance(type) as Entity;
            }

            entity.ResetGraphStateForCreate(isFromPool);
            return entity;
        }

        public Entity AddComponent(Entity component)
        {
            Type type = component.GetType();
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;
            return component;
        }

        internal Entity AddComponent(Type type, bool isFromPool = false)
        {
            if (_hierarchy != null && _hierarchy.HasComponent(this, type))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            World world = GetWorld();
            Entity component = Create(world, type, isFromPool);
            component.Id = Id;
            component.ComponentParent = this;
            return component;
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
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            world.Lifecycle.Awake(component);
            world.Hierarchy.Scheduler.Register(component);
            return component as K;
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
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            world.Lifecycle.Awake(component, p1);
            world.Hierarchy.Scheduler.Register(component);
            return component as K;
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
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            world.Lifecycle.Awake(component, p1, p2);
            world.Hierarchy.Scheduler.Register(component);
            return component as K;
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
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            world.Lifecycle.Awake(component, p1, p2, p3);
            world.Hierarchy.Scheduler.Register(component);
            return component as K;
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
            Entity component = Create(world, type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            world.Lifecycle.Awake(component, p1, p2, p3, p4);
            world.Hierarchy.Scheduler.Register(component);
            return component as K;
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
            K component = AddComponentWithIdCore<K>(Id, isFromPool);
            NotifyComponentAdded(component);
            return component;
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
            K component = AddComponentWithIdCore<K, P1>(Id, p1, isFromPool);
            NotifyComponentAdded(component);
            return component;
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
            K component = AddComponentWithIdCore<K, P1, P2>(Id, p1, p2, isFromPool);
            NotifyComponentAdded(component);
            return component;
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
            K component = AddComponentWithIdCore<K, P1, P2, P3>(Id, p1, p2, p3, isFromPool);
            NotifyComponentAdded(component);
            return component;
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
            K component = AddComponentWithIdCore<K, P1, P2, P3, P4>(Id, p1, p2, p3, p4, isFromPool);
            NotifyComponentAdded(component);
            return component;
        }

        private void NotifyComponentAdded<K>(K component) where K : Entity
        {
            World world = GetWorld();
            world.Dependencies.NotifyAddComponent(this, typeof(K));
            component.ProcessComponentDependencies(world);
        }

        internal Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
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
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            world.Lifecycle.Awake(child);
            world.Hierarchy.Scheduler.Register(child);
            return child;
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
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            world.Lifecycle.Awake(child, a);
            world.Hierarchy.Scheduler.Register(child);
            return child;
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
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b);
            world.Hierarchy.Scheduler.Register(child);
            return child;
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
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b, c);
            world.Hierarchy.Scheduler.Register(child);
            return child;
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
            T child = (T)Create(world, type, isFromPool);
            child.Id = world.IdGenerator.GenerateId();
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b, c, d);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        public T AddChildWithId<T>(long id) where T : Entity, IAwake
        {
            return AddChildWithIdCore<T>(id, false);
        }

        private T AddChildWithIdCore<T>(long id, bool isFromPool) where T : Entity, IAwake
        {
            Type type = typeof(T);
            World world = GetWorld();
            T child = Create(world, type, isFromPool) as T;
            child.Id = id;
            child.Parent = this;
            world.Lifecycle.Awake(child);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        public T AddChildWithId<T, A>(long id, A a) where T : Entity, IAwake<A>
        {
            return AddChildWithIdCore<T, A>(id, a, false);
        }

        private T AddChildWithIdCore<T, A>(long id, A a, bool isFromPool) where T : Entity, IAwake<A>
        {
            Type type = typeof(T);
            World world = GetWorld();
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            world.Lifecycle.Awake(child, a);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b) where T : Entity, IAwake<A, B>
        {
            return AddChildWithIdCore<T, A, B>(id, a, b, false);
        }

        private T AddChildWithIdCore<T, A, B>(long id, A a, B b, bool isFromPool) where T : Entity, IAwake<A, B>
        {
            Type type = typeof(T);
            World world = GetWorld();
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c) where T : Entity, IAwake<A, B, C>
        {
            return AddChildWithIdCore<T, A, B, C>(id, a, b, c, false);
        }

        private T AddChildWithIdCore<T, A, B, C>(long id, A a, B b, C c, bool isFromPool) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof(T);
            World world = GetWorld();
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b, c);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        public T AddChildWithId<T, A, B, C, D>(long id, A a, B b, C c, D d) where T : Entity, IAwake<A, B, C, D>
        {
            return AddChildWithIdCore<T, A, B, C, D>(id, a, b, c, d, false);
        }

        private T AddChildWithIdCore<T, A, B, C, D>(long id, A a, B b, C c, D d, bool isFromPool) where T : Entity, IAwake<A, B, C, D>
        {
            Type type = typeof(T);
            World world = GetWorld();
            T child = (T)Create(world, type, isFromPool);
            child.Id = id;
            child.Parent = this;
            world.Lifecycle.Awake(child, a, b, c, d);
            world.Hierarchy.Scheduler.Register(child);
            return child;
        }

        internal protected virtual long GetLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }

        internal void AssignGraphHandle(EntityHierarchy hierarchy, EntityHandle handle)
        {
            _hierarchy = hierarchy;
            GraphHandle = handle;
            _hierarchyDisposeCompleted = false;
        }

        internal void ClearGraphHandle()
        {
            GraphHandle = EntityHandle.None;
            _hierarchy = null;
        }

        internal void BeginDisposeFromGraph()
        {
            IsRegister = false;
            InstanceId = 0;
        }

        internal void DisposeSelfFromGraph()
        {
            if (_hierarchyDisposeCompleted)
            {
                return;
            }

            _hierarchyDisposeCompleted = true;
            BeginDisposeFromGraph();

            World world = _hierarchy?.World ?? World.Instance;
            if (this is IDestroy)
            {
                world.Lifecycle.Destroy(this);
            }

            _scene = null;

            OnDispose();

            EntityTreeEventHub.NotifyEntityDisposed(this);

            bool isFromPool = IsFromPool;
            _status = EntityStatus.None;
            IsFromPool = isFromPool;

            world.ObjectPool.Recycle(this);
        }

        internal void SetSceneFromGraph(Scene scene)
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

        private void ResetGraphStateForCreate(bool isFromPool)
        {
            _hierarchy = null;
            GraphHandle = EntityHandle.None;
            _hierarchyDisposeCompleted = false;
            _scene = null;
            InstanceId = 0;
            Id = 0;

            _status = EntityStatus.None;
            IsFromPool = isFromPool;
            IsCreated = true;
            IsNew = true;
        }

        private static EntityHierarchy ResolveGraph(Entity owner)
        {
            return owner._hierarchy ?? World.Instance.Hierarchy;
        }

        internal World GetWorld()
        {
            return _hierarchy?.World ?? World.Instance;
        }
    }
}
