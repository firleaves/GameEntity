using System;
using System.Collections.Generic;

namespace GameEntity
{
    public sealed class World : IDisposable
    {
        private enum WorldState : byte
        {
            Active,
            Disposing,
            Disposed,
        }

        private static World _instance;

        private readonly Dictionary<string, Scene> _scenes = new Dictionary<string, Scene>();
        private readonly HashSet<Entity> _pendingEntityRegistrations = new HashSet<Entity>();
        private int _creationScopeDepth;
        private bool _isPublishingEntityRegistrations;
        private bool _isRunningUpdatePass;
        private WorldState _state;
        private string _rootName;

        private World()
        {
            Time = new TimeInfo();
            Time.Awake();

            IdGenerator = new IdGenerator(Time);
            IdGenerator.Awake();

            ObjectPool = new ObjectPool();
            ObjectPool.Awake();

            EntityEvents = new EntityTreeEventHub();
            Hierarchy = new EntityHierarchy(this);
            Lifecycle = new EntityLifecycle(this);
        }

        public static World Instance
        {
            get
            {
                return _instance ??= new World();
            }
        }

        public string RootName
        {
            get
            {
                ThrowIfNotActive();
                return _rootName;
            }
            set
            {
                ThrowIfNotActive();
                _rootName = value;
            }
        }

        internal TimeInfo Time { get; }

        internal IdGenerator IdGenerator { get; }

        internal ObjectPool ObjectPool { get; }

        internal EntityLifecycle Lifecycle { get; }

        internal EntityHierarchy Hierarchy { get; }

        internal EntityTreeEventHub EntityEvents { get; }

        public Scene GetScene(string sceneName)
        {
            ThrowIfNotActive();
            return _scenes.TryGetValue(sceneName, out var scene) ? scene : null;
        }

        public bool TryResolve<T>(EntityHandle handle, out T entity) where T : Entity
        {
            ThrowIfNotActive();
            return Hierarchy.TryResolve(handle, out entity);
        }

        public EntitySnapshot CaptureEntitySnapshot()
        {
            ThrowIfNotActive();
            return Hierarchy.CaptureSnapshot();
        }

        public EntityValidationResult ValidateEntities()
        {
            ThrowIfNotActive();
            return Hierarchy.Validate();
        }

        public IDisposable ObserveEntities(IEntityTreeObserver observer, bool replayExisting = true)
        {
            ThrowIfNotActive();
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            IReadOnlyList<Entity> existingEntities = replayExisting
                ? Hierarchy.GetPublishedEntitiesParentFirst()
                : Array.Empty<Entity>();
            IDisposable registration = EntityEvents.Register(observer);
            EntityEvents.ReplayRegistered(observer, existingEntities);
            return registration;
        }

        public void Update(float deltaTime)
        {
            ThrowIfNotActive();
            ValidateDeltaTime(deltaTime, nameof(deltaTime), allowZero: true);
            BeginUpdatePass(nameof(Update));
            try
            {
                Time.Update();
                Hierarchy.Scheduler.Update(deltaTime);
            }
            finally
            {
                _isRunningUpdatePass = false;
            }
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            ThrowIfNotActive();
            ValidateDeltaTime(fixedDeltaTime, nameof(fixedDeltaTime), allowZero: false);
            BeginUpdatePass(nameof(FixedUpdate));
            try
            {
                Time.Update();
                Hierarchy.Scheduler.FixedUpdate(fixedDeltaTime);
            }
            finally
            {
                _isRunningUpdatePass = false;
            }
        }

        public void Dispose()
        {
            if (_state != WorldState.Active)
            {
                return;
            }

            if (_isRunningUpdatePass || _creationScopeDepth != 0 || _isPublishingEntityRegistrations)
            {
                throw new InvalidOperationException("World cannot be disposed while an Update or creation transaction is running.");
            }

            _state = WorldState.Disposing;
            try
            {
                Hierarchy.Dispose();
            }
            finally
            {
                try
                {
                    _pendingEntityRegistrations.Clear();
                    _creationScopeDepth = 0;
                    _isPublishingEntityRegistrations = false;
                    EntityEvents.Clear();
                    ObjectPool.Clear();
                    Time.Reset();
                    _scenes.Clear();
                }
                finally
                {
                    _state = WorldState.Disposed;
                    if (ReferenceEquals(_instance, this))
                    {
                        _instance = null;
                    }
                }
            }
        }

        public Scene AddScene(string sceneName, Scene scene)
        {
            ThrowIfNotActive();
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (_scenes.ContainsKey(sceneName))
            {
                throw new Exception($"scene {sceneName} already exists");
            }

            if (scene.Name != sceneName)
            {
                throw new Exception($"scene name mismatch: key {sceneName}, scene {scene.Name}");
            }

            if (scene.Handle.IsValid)
            {
                throw new Exception($"scene {sceneName} already registered");
            }

            EntityPlacementMetadata.ValidateSceneRoot(scene.GetType());

            if (scene is IStart || scene is IFixedUpdate || scene is IUpdate)
            {
                throw new InvalidOperationException(
                    $"{scene.GetType().FullName} implements an Update lifecycle interface, but Scene roots are not scheduled.");
            }

            if (UpdateRequirementMetadata.GetRequirementTypes(scene.GetType()).Length > 0)
            {
                throw new InvalidOperationException(
                    $"{scene.GetType().FullName} uses RequireForUpdate but Scene roots cannot declare Update requirements.");
            }

            using (BeginCreationScope())
            {
                EntityHandle creationHandle = EntityHandle.None;
                long creationInstanceId = 0;
                try
                {
                    scene.EnsureIdentity(IdGenerator);

                    // Scene.Awake 允许通过 World 查询自身，因此先加入临时结构，成功后再发布观察者事件。
                    _scenes.Add(sceneName, scene);
                    Hierarchy.RegisterSceneRoot(scene);
                    creationHandle = scene.Handle;
                    creationInstanceId = scene.InstanceId;

                    scene.Awake();
                    EnsureSceneCreationLifetime(scene, creationHandle, creationInstanceId, "Awake");
                    scene.CompleteRegistration();
                    EnsureSceneCreationLifetime(scene, creationHandle, creationInstanceId, "RegisterSystem");
                    QueueEntityRegistration(scene);
                    return scene;
                }
                catch
                {
                    try
                    {
                        if (IsSameSceneLifetime(scene, creationHandle, creationInstanceId) ||
                            (!creationHandle.IsValid && !scene.IsDestroyed))
                        {
                            scene.Destroy();
                        }
                    }
                    catch (Exception cleanupError)
                    {
                        Log.Error($"Scene creation rollback error: {scene.GetType().FullName}: {cleanupError}");
                    }

                    if (_scenes.TryGetValue(sceneName, out Scene registeredScene) &&
                        ReferenceEquals(registeredScene, scene) &&
                        (!creationHandle.IsValid ||
                         (registeredScene.Handle == creationHandle && registeredScene.InstanceId == creationInstanceId)))
                    {
                        _scenes.Remove(sceneName);
                    }

                    throw;
                }
            }
        }

        public void RemoveScene(string sceneName)
        {
            ThrowIfNotActive();
            if (_scenes.TryGetValue(sceneName, out var scene))
            {
                scene.Destroy();
                _scenes.Remove(sceneName);
            }
        }

        internal void UnregisterScene(Scene scene)
        {
            if (scene == null)
            {
                return;
            }

            if (_scenes.TryGetValue(scene.Name, out var registeredScene) && ReferenceEquals(registeredScene, scene))
            {
                _scenes.Remove(scene.Name);
            }
        }

        internal IDisposable BeginCreationScope()
        {
            ThrowIfNotActive();
            _creationScopeDepth++;
            return new CreationScope(this);
        }

        internal void ThrowIfNotActive()
        {
            if (_state != WorldState.Active)
            {
                throw new ObjectDisposedException(nameof(World), "This World has started disposing and can no longer be used.");
            }
        }

        internal void QueueEntityRegistration(Entity entity)
        {
            if (entity == null || entity.IsDestroyed || entity.IsTreePublished)
            {
                return;
            }

            _pendingEntityRegistrations.Add(entity);
        }

        private void EndCreationScope()
        {
            if (_creationScopeDepth <= 0)
            {
                throw new InvalidOperationException("Entity creation scope is unbalanced.");
            }

            _creationScopeDepth--;
            FlushEntityRegistrations();
        }

        private void FlushEntityRegistrations()
        {
            if (_creationScopeDepth != 0 || _isPublishingEntityRegistrations)
            {
                return;
            }

            _isPublishingEntityRegistrations = true;
            try
            {
                while (_pendingEntityRegistrations.Count > 0)
                {
                    var batch = new List<Entity>(_pendingEntityRegistrations);
                    _pendingEntityRegistrations.Clear();
                    batch.Sort((left, right) =>
                    {
                        int depthComparison = Hierarchy.GetHierarchyDepth(left).CompareTo(Hierarchy.GetHierarchyDepth(right));
                        return depthComparison != 0 ? depthComparison : left.Handle.NodeId.CompareTo(right.Handle.NodeId);
                    });

                    foreach (Entity entity in batch)
                    {
                        if (entity == null || entity.IsDestroyed || entity.IsTreePublished || !entity.Handle.IsValid)
                        {
                            continue;
                        }

                        entity.MarkTreePublished();
                        EntityEvents.NotifyEntityRegistered(entity);
                    }
                }
            }
            finally
            {
                _isPublishingEntityRegistrations = false;
            }
        }

        private void BeginUpdatePass(string operation)
        {
            if (_isRunningUpdatePass)
            {
                throw new InvalidOperationException($"World.{operation} cannot reenter an active update pass.");
            }

            _isRunningUpdatePass = true;
        }

        private void EnsureSceneCreationLifetime(
            Scene scene,
            EntityHandle creationHandle,
            long creationInstanceId,
            string stage)
        {
            if (IsSameSceneLifetime(scene, creationHandle, creationInstanceId))
            {
                return;
            }

            throw new InvalidOperationException(
                $"{scene.GetType().FullName} ended or replaced its Scene lifetime during {stage}; creation cannot be committed.");
        }

        private bool IsSameSceneLifetime(Scene scene, EntityHandle creationHandle, long creationInstanceId)
        {
            if (scene == null || scene.IsDestroyed || !creationHandle.IsValid || creationInstanceId == 0 ||
                scene.Handle != creationHandle || scene.InstanceId != creationInstanceId ||
                !Hierarchy.TryResolve(creationHandle, out Scene resolved))
            {
                return false;
            }

            return ReferenceEquals(scene, resolved);
        }

        private sealed class CreationScope : IDisposable
        {
            private World _world;

            public CreationScope(World world)
            {
                _world = world;
            }

            public void Dispose()
            {
                if (_world == null)
                {
                    return;
                }

                World world = _world;
                _world = null;
                world.EndCreationScope();
            }
        }

        private static void ValidateDeltaTime(float deltaTime, string parameterName, bool allowZero)
        {
            bool belowMinimum = allowZero ? deltaTime < 0f : deltaTime <= 0f;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || belowMinimum)
            {
                string expectation = allowZero ? "finite and greater than or equal to zero" : "finite and greater than zero";
                throw new ArgumentOutOfRangeException(parameterName, deltaTime, $"Delta time must be {expectation}.");
            }
        }
    }
}
