using System;
using UnityEngine;

namespace GameEntity.Unity
{
    /// <summary>
    /// Unity 场景中的 GameEntity runtime 入口。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameEntityUnityBootstrap : MonoBehaviour
    {
        private static GameEntityUnityBootstrap _active;

        private IDisposable _observerRegistration;
        private UnityEntityViewRegistry _registry;
        private bool _initialized;
        private bool _shutdown;

        public GameObject ViewRoot;
        public bool AutoCreateViews = true;
        public bool DestroyViewsOnEntityDispose = true;
        public bool UseUnityLogger = true;

        public UnityEntityViewRegistry Registry => _registry;

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Debug.LogError("Only one GameEntityUnityBootstrap can be active in a scene.");
                enabled = false;
                return;
            }

            _active = this;
            if (ViewRoot == null)
            {
                ViewRoot = gameObject;
            }

            if (UseUnityLogger)
            {
                Log.Logger = new UnityGameEntityLogger();
            }

            InitializeWorld();
            _registry = new UnityEntityViewRegistry(ViewRoot.transform, AutoCreateViews, DestroyViewsOnEntityDispose);
            UnityEntityViewRegistry.Active = _registry;
            _observerRegistration = EntityTreeEventHub.Register(_registry);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _shutdown)
            {
                return;
            }

            TimeInfo.Instance?.Update();
            EntitySystem.Instance?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_initialized || _shutdown)
            {
                return;
            }

            EntitySystem.Instance?.LateUpdate();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void InitializeWorld()
        {
            World.Instance.RootName = ViewRoot.name;

            if (EntitySystem.Instance == null) World.Instance.AddSingleton<EntitySystem>();
            if (ObjectPool.Instance == null) World.Instance.AddSingleton<ObjectPool>();
            if (IdGenerator.Instance == null) World.Instance.AddSingleton<IdGenerator>();
            if (TimeInfo.Instance == null) World.Instance.AddSingleton<TimeInfo>();

            World.Instance.InitializeDependencySystem();
        }

        private void Shutdown()
        {
            if (_shutdown)
            {
                return;
            }

            _shutdown = true;
            _observerRegistration?.Dispose();
            _observerRegistration = null;

            if (UnityEntityViewRegistry.Active == _registry)
            {
                UnityEntityViewRegistry.Active = null;
            }

            _registry = null;
            if (_active == this)
            {
                _active = null;
            }

            if (_initialized)
            {
                World.Instance.Dispose();
            }

            _initialized = false;
        }
    }
}
