using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GE
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

    public class Entity : IDisposable
    {

        public GameObject ViewGO;

        /// <summary>
        /// 对比两个Entity是否是同一个实体
        /// </summary>
        public long InstanceId { get; protected set; }

        /// <summary>
        /// 实体的唯一ID
        /// </summary>
        public long Id { get; protected internal set; }

        private EntityStatus _status = EntityStatus.None;

        private Entity _parent;
        internal SortedDictionary<long, Entity> _children;

        internal protected SortedDictionary<long, Entity> _components;

        protected IScene _iScene;


        public bool IsFromPool
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
                }

                if (value)
                {
                    ViewGO = new UnityEngine.GameObject(ViewName);
                    ViewGO.AddComponent<ComponentView>().Component = this;
                    ViewGO.transform.SetParent(_parent == null ? World.Instance.Root.transform : _parent.ViewGO.transform);
                }
                else
                {
                    UnityEngine.Object.Destroy(ViewGO);
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
        private string _viewName;
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
                ViewGO.name = _viewName;
            }
        }
        public bool IsDisposed => InstanceId == 0;


        // 父实体

        public Entity Parent
        {
            get => _parent;
            set
            {

                if (value == null)
                {
                    throw new Exception($"cant set parent null: {GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {GetType().FullName}");
                }




                if (_parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (_parent == value)
                    {
                        Log.Error($"重复设置了Parent: {GetType().FullName} parent: {_parent.GetType().FullName}");
                        return;
                    }

                    _parent.RemoveFromChildren(this);
                }

                _parent = value;
                IsComponent = false;
                _parent.AddToChildren(this);

                if (this is IScene scene)
                {

                    _iScene = scene;
                }
                else
                {
                    IScene = _parent._iScene;
                }


                ViewGO.GetComponent<ComponentView>().Component = this;
                ViewGO.transform.SetParent(_parent == null ? World.Instance.Root.transform : _parent.ViewGO.transform);
                if (_children != null)
                {
                    foreach (var child in _children.Values)
                    {
                        child.ViewGO.transform.SetParent(ViewGO.transform);
                    }
                }
                if (_components != null)
                {
                    foreach (var comp in _components.Values)
                    {
                        comp.ViewGO.transform.SetParent(ViewGO.transform);
                    }
                }


            }
        }

        // 子实体集合




        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {GetType().FullName}");
                }

                // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent domain is null: {GetType().FullName} {value.GetType().FullName}");
                }

                if (_parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (_parent == value)
                    {
                        Log.Error($"重复设置了Parent: {GetType().FullName} parent: {_parent.GetType().FullName}");
                        return;
                    }

                    _parent.RemoveFromComponents(this);
                }

                _parent = value;
                IsComponent = true;
                _parent.AddToComponents(this);

                if (this is IScene scene)
                {

                    IScene = scene;
                }
                else
                {
                    IScene = _parent._iScene;
                }

            }
        }

        public IScene IScene
        {
            get
            {
                return _iScene;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"domain cant set null: {GetType().FullName}");
                }

                if (_iScene == value)
                {
                    return;
                }

                IScene preScene = _iScene;
                _iScene = value;

                if (preScene == null)
                {
                    if (InstanceId == 0)
                    {
                        InstanceId = IdGenerator.Instance.GenerateId();
                    }

                    IsRegister = true;
                }

                // 递归设置孩子的Domain
                if (_children != null)
                {
                    foreach (Entity entity in _children.Values)
                    {
                        entity.IScene = _iScene;
                    }
                }

                if (_components != null)
                {
                    foreach (Entity component in _components.Values)
                    {
                        component.IScene = _iScene;
                    }
                }

                if (!IsCreated)
                {
                    IsCreated = true;
                }
            }
        }

        public SortedDictionary<long, Entity> Children
        {
            get
            {
                return _children ??= ObjectPool.Instance.Fetch<SortedDictionary<long, Entity>>();
            }
        }

        public SortedDictionary<long, Entity> Components
        {
            get
            {
                return _components ??= ObjectPool.Instance.Fetch<SortedDictionary<long, Entity>>();
            }
        }



        protected Entity()
        {

        }

        protected virtual void RegisterSystem()
        {

        }
        public int ComponentsCount()
        {
            if (_components == null)
            {
                return 0;
            }
            return _components.Count;
        }

        public int ChildrenCount()
        {
            if (_children == null)
            {
                return 0;
            }
            return _children.Count;
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsRegister = false;
            InstanceId = 0;



            ObjectPool objectPool = ObjectPool.Instance;
            // 清理Children
            if (_children != null)
            {
                foreach (Entity child in _children.Values)
                {
                    child.Dispose();
                }

                _children.Clear();
                objectPool.Recycle(_children);
                _children = null;


            }

            // 清理Component
            if (_components != null)
            {
                foreach (var kv in _components)
                {
                    kv.Value.Dispose();
                }

                _components.Clear();
                objectPool.Recycle(_components);
                _components = null;
            }

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystem.Instance.Destroy(this);
            }

            _iScene = null;

            if (_parent != null && !_parent.IsDisposed)
            {
                if (IsComponent)
                {
                    _parent.RemoveComponent(this);
                }
                else
                {
                    _parent.RemoveFromChildren(this);
                }
            }

            _parent = null;

            Dispose();

            // 把status字段其它的status标记都还原
            bool isFromPool = IsFromPool;
            _status = EntityStatus.None;
            IsFromPool = isFromPool;

            ObjectPool.Instance.Recycle(this);
        }


        private void AddToChildren(Entity entity)
        {
            Children.Add(entity.Id, entity);
        }

        private void RemoveFromChildren(Entity entity)
        {
            if (_children == null)
            {
                return;
            }

            _children.Remove(entity.Id);

            if (_children.Count == 0)
            {
                ObjectPool.Instance.Recycle(_children);
                _children = null;
            }
        }

        private void AddToComponents(Entity component)
        {
            Components.Add(GetLongHashCode(component.GetType()), component);
        }

        private void RemoveFromComponents(Entity component)
        {
            if (_components == null)
            {
                return;
            }

            _components.Remove(GetLongHashCode(component.GetType()));

            if (_components.Count == 0)
            {
                ObjectPool.Instance.Recycle(_components);
                _components = null;
            }
        }

        public K GetChild<K>(long id) where K : Entity
        {
            if (_children == null)
            {
                return null;
            }

            _children.TryGetValue(id, out Entity child);
            return child as K;
        }
        public void ClearChildren()
        {
            if (_children != null)
            {
                var childrenCopy = new List<Entity>(_children.Values);
                foreach (Entity child in childrenCopy)
                {
                    child.Dispose();
                }
            }
        }
        public void RemoveChild(long id)
        {
            if (_children == null)
            {
                return;
            }

            if (!_children.TryGetValue(id, out Entity child))
            {
                return;
            }

            _children.Remove(id);
            child.Dispose();
        }

        public void RemoveComponent<K>() where K : Entity
        {
            if (IsDisposed)
            {
                return;
            }

            if (_components == null)
            {
                return;
            }

            Type type = typeof(K);

            Entity c;
            if (!_components.TryGetValue(GetLongHashCode(type), out c))
            {
                return;
            }

            // 通知组件将被移除
            DependencyRegistry.Instance.NotifyRemoveComponent(this, type);


            var registry = DependencyRegistry.Instance;
            if (registry != null && (c is IDependentComponent ||
                c.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true).Length > 0))
            {
                registry.UnregisterDependentComponent(c);
            }


            RemoveFromComponents(c);
            c.Dispose();
        }

        private void RemoveComponent(Entity component)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_components == null)
            {
                return;
            }

            Entity c;
            if (!_components.TryGetValue(GetLongHashCode(component.GetType()), out c))
            {
                return;
            }

            if (c.InstanceId != component.InstanceId)
            {
                return;
            }

            RemoveFromComponents(c);
            c.Dispose();
        }

        public void RemoveComponent(Type type)
        {
            if (IsDisposed)
            {
                return;
            }

            Entity c;
            if (!_components.TryGetValue(GetLongHashCode(type), out c))
            {
                return;
            }

            RemoveFromComponents(c);
            c.Dispose();
        }

        public K GetComponent<K>() where K : Entity
        {
            if (_components == null)
            {
                return null;
            }

            if (_components.TryGetValue(GetLongHashCode(typeof(K)), out var exactMatch))
            {
                return (K)exactMatch;
            }

            foreach (var component in _components.Values)
            {
                if (component is K derivedMatch)
                {
                    return derivedMatch;
                }
            }

            return null;
        }

        public Entity GetComponent(Type type)
        {
            if (_components == null)
            {
                return null;
            }


            Entity component;
            if (!_components.TryGetValue(GetLongHashCode(type), out component))
            {
                return null;
            }

            return component;
        }




        internal static Entity Create(Type type, bool isFromPool)
        {
            Entity component;
            if (isFromPool)
            {
                component = (Entity)ObjectPool.Instance.Fetch(type);
            }
            else
            {
                component = Activator.CreateInstance(type) as Entity;
            }

            component.IsFromPool = isFromPool;
            component.IsCreated = true;
            component.IsNew = true;
            component.Id = 0;
            return component;
        }

        public Entity AddComponent(Entity component)
        {
            Type type = component.GetType();
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;

            return component;
        }


        internal Entity AddComponent(Type type, bool isFromPool = false)
        {
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = Id;
            component.ComponentParent = this;
            // var entitySystemSingleton = EntitySystem.Instance;
            // entitySystemSingleton.Awake(component);

            return component;
        }

        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            Type type = typeof(K);
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            var entitySystemSingleton = EntitySystem.Instance;
            entitySystemSingleton.Awake(component);
            entitySystemSingleton.TryRegisterUpdate(component);

            return component as K;
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            Type type = typeof(K);
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            var entitySystemSingleton = EntitySystem.Instance;
            entitySystemSingleton.Awake(component, p1);
            entitySystemSingleton.TryRegisterUpdate(component);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            Type type = typeof(K);
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            var entitySystemSingleton = EntitySystem.Instance;
            entitySystemSingleton.Awake(component, p1, p2);
            entitySystemSingleton.TryRegisterUpdate(component);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Type type = typeof(K);
            if (_components != null && _components.ContainsKey(GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            var entitySystemSingleton = EntitySystem.Instance;
            entitySystemSingleton.Awake(component, p1, p2, p3);
            entitySystemSingleton.TryRegisterUpdate(component);

            return component as K;
        }




        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            K component = AddComponentWithId<K>(Id, isFromPool);

            // 添加组件后通知依赖系统
            DependencyRegistry.Instance.NotifyAddComponent(this, typeof(K));
            component.ProcessComponentDependencies();

            return component;
        }

        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            K component = AddComponentWithId<K, P1>(Id, p1, isFromPool);

            // 添加组件后通知依赖系统
            DependencyRegistry.Instance.NotifyAddComponent(this, typeof(K));
            component.ProcessComponentDependencies();

            return component;
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            K component = AddComponentWithId<K, P1, P2>(Id, p1, p2, isFromPool);

            // 添加组件后通知依赖系统
            DependencyRegistry.Instance.NotifyAddComponent(this, typeof(K));
            component.ProcessComponentDependencies();

            return component;
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            K component = AddComponentWithId<K, P1, P2, P3>(Id, p1, p2, p3, isFromPool);

            // 添加组件后通知依赖系统
            DependencyRegistry.Instance.NotifyAddComponent(this, typeof(K));
            component.ProcessComponentDependencies();

            return component;
        }

        internal Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = this;

            EntitySystem.Instance.Awake(component);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a, b);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a, b, c);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof(T);
            T component = Entity.Create(type, isFromPool) as T;
            component.Id = id;
            component.Parent = this;
            EntitySystem.Instance.Awake(component);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a, b);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof(T);
            T component = (T)Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystem.Instance.Awake(component, a, b, c);
            EntitySystem.Instance.TryRegisterUpdate(component);
            return component;
        }

        internal protected virtual long GetLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }




    }
}

