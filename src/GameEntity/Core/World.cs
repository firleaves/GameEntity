using System;
using System.Collections.Generic;

namespace GameEntity
{
    public class World : IDisposable
    {
        private static World _instance;

        private readonly Dictionary<string, Scene> _scenes = new Dictionary<string, Scene>();

        public World()
        {
            Time = new TimeInfo();
            Time.Awake();

            IdGenerator = new IdGenerator(Time);
            IdGenerator.Awake();

            ObjectPool = new ObjectPool();
            ObjectPool.Awake();

            Hierarchy = new EntityHierarchy(this);
            Lifecycle = new EntityLifecycle(this);
            Dependencies = new DependencyRegistry();
        }

        public static World Instance
        {
            get
            {
                return _instance ??= new World();
            }
        }

        public string RootName { get; set; }

        internal TimeInfo Time { get; }

        internal IdGenerator IdGenerator { get; }

        internal ObjectPool ObjectPool { get; }

        internal EntityLifecycle Lifecycle { get; }

        internal DependencyRegistry Dependencies { get; }

        internal EntityHierarchy Hierarchy { get; }

        public Scene GetScene(string sceneName)
        {
            return _scenes.TryGetValue(sceneName, out var scene) ? scene : null;
        }

        public bool TryResolve<T>(EntityHandle handle, out T entity) where T : Entity
        {
            return Hierarchy.TryResolve(handle, out entity);
        }

        public EntitySnapshot CaptureEntitySnapshot()
        {
            return Hierarchy.CaptureSnapshot();
        }

        public EntityValidationResult ValidateEntities()
        {
            return Hierarchy.Validate();
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            Time.Update();
            Hierarchy.Scheduler.Update(deltaTime, unscaledDeltaTime);
        }

        public void Dispose()
        {
            Hierarchy.Dispose();
            Dependencies.Clear();
            ObjectPool.Clear();
            Time.Reset();
            _scenes.Clear();

            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        public Scene AddScene(string sceneName, Scene scene)
        {
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

            scene.EnsureIdentity(IdGenerator);

            // scene awake 可能调用 world 获得 scene，所以先加入再 awake。
            _scenes.Add(sceneName, scene);
            Hierarchy.RegisterSceneRoot(scene);

            scene.Awake();
            return scene;
        }

        public void RemoveScene(string sceneName)
        {
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
    }
}
