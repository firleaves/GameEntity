using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GE
{
    public static class AsyncEntityExtensions
    {

        private static async UniTask AsyncEntityInitialize<T>(Entity entity, AsyncEntity component, CancellationToken cancelToken)
        {
            Type type = typeof(T);
            try
            {
                await component.InitializeAsync(cancelToken);
            }
            catch (OperationCanceledException)
            {
                Log.Warning($"AsyncEntity 加载被取消,移除组件，{type.FullName}");
                entity.RemoveComponent(type);
                throw;

            }
            catch (Exception ex)
            {
                Log.Error($"AsyncEntity 加载异常: {ex}  移除组件，{type.FullName}");
                entity.RemoveComponent(type);
                throw;
            }

        }

        public static async UniTask<T> AddComponentWithIdAsync<T>(this Entity entity, long id, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            var component = entity.AddComponent(type, isFromPool) as AsyncEntity;
            component.Id = id;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component);
                component.ProcessComponentDependencies();
                return component as T;
            }
            return null;
        }


        public static async UniTask<T> AddComponentWithIdAsync<T, P1>(this Entity entity, long id, P1 p1, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            var component = entity.AddComponent(type, isFromPool) as AsyncEntity;
            component.Id = id;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;

                entitySystemSingleton.Awake(component, id, p1);
                DependencyRegistry.Instance.NotifyAddComponent(entity, typeof(T));
                component.ProcessComponentDependencies();


                return component as T;
            }
            return null;
        }


        public static async UniTask<T> AddComponentWithIdAsync<T, P1, P2>(this Entity entity, long id, P1 p1, P2 p2, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            var component = entity.AddComponent(type, isFromPool) as AsyncEntity;
            component.Id = id;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component, id, p1, p2);

                DependencyRegistry.Instance.NotifyAddComponent(entity, typeof(T));
                component.ProcessComponentDependencies();
                return component as T;
            }
            return null;
        }
        public static async UniTask<T> AddComponentWithIdAsync<T, P1, P2, P3>(this Entity entity, long id, P1 p1, P2 p2, P3 p3, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2, P3>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            var component = entity.AddComponent(type, isFromPool) as AsyncEntity;
            component.Id = id;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component, id, p1, p2, p3);

                DependencyRegistry.Instance.NotifyAddComponent(entity, typeof(T));
                component.ProcessComponentDependencies();
                return component as T;
            }
            return null;
        }


        public static async UniTask<T> AddComponentAsync<T>(this Entity entity, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake, new()
        {
            return await AddComponentWithIdAsync<T>(entity, entity.Id, cancelToken, isFromPool);
        }

        public static async UniTask<T> AddComponentAsync<T, P1>(this Entity entity, P1 p1, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1>, new()
        {
            return await AddComponentWithIdAsync<T, P1>(entity, entity.Id, p1, cancelToken, isFromPool);
        }



        public static async UniTask<T> AddComponentAsync<T, P1, P2>(this Entity entity, P1 p1, P2 p2, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2>, new()
        {
            return await AddComponentWithIdAsync<T, P1, P2>(entity, entity.Id, p1, p2, cancelToken, isFromPool);
        }




        public static async UniTask<T> AddComponentAsync<T, P1, P2, P3>(this Entity entity, P1 p1, P2 p2, P3 p3, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2, P3>, new()
        {
            return await AddComponentWithIdAsync<T, P1, P2, P3>(entity, entity.Id, p1, p2, p3, cancelToken, isFromPool);
        }

        public static async UniTask<T> AddChildAsync<T>(this Entity entity, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }


            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = entity;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component);
                return component as T;
            }
            return null;
        }

        public static async UniTask<T> AddChildAsync<T, P1>(this Entity entity, P1 p1, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = entity;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component, p1);
                return component as T;
            }
            return null;
        }

        public static async UniTask<T> AddChildAsync<T, P1, P2>(this Entity entity, P1 p1, P2 p2, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }


            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = entity;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component, p1, p2);
                return component as T;
            }
            return null;
        }

        public static async UniTask<T> AddChildAsync<T, P1, P2, P3>(this Entity entity, P1 p1, P2 p2, P3 p3, CancellationToken cancelToken = default, bool isFromPool = false) where T : AsyncEntity, IAwake<P1, P2, P3>, new()
        {
            if (cancelToken.IsCancellationRequested)
            {
                return null;
            }

            Type type = typeof(T);
            if (entity._components != null && entity._components.ContainsKey(entity.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }


            T component = (T)Entity.Create(type, isFromPool);
            component.Id = IdGenerator.Instance.GenerateId();
            component.Parent = entity;

            await AsyncEntityInitialize<T>(entity, component, cancelToken);

            if (component.IsLoaded)
            {
                var entitySystemSingleton = EntitySystem.Instance;
                entitySystemSingleton.Awake(component, p1, p2, p3);
                return component as T;
            }
            return null;
        }

    }
}
