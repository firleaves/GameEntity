using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameEntity.Unity
{
    /// <summary>
    /// Unity 场景中的 GameEntity runtime 入口。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("GameEntity/GameEntity Runner")]
    public sealed class GameEntityRunner : MonoBehaviour
    {
        private static GameEntityRunner _active;

        private IDisposable _observerRegistration;
        private UnityEntityViewRegistry _registry;
        private bool _initialized;
        private bool _shutdown;
        private float _fixedAccumulator;

        public GameObject ViewRoot;
        public bool AutoCreateViews = true;
        public bool DestroyViewsOnEntityDestroy = true;
        public bool UseUnityLogger = true;
        public bool OwnsWorldLifetime = true;

        [Min(1f)]
        [FormerlySerializedAs("FixedTicksPerSecond")]
        public float FixedUpdatesPerSecond = 30f;

        [Min(1)]
        public int MaxFixedStepsPerFrame = 4;

        public UnityEntityViewRegistry Registry => _registry;

        public float FixedInterpolationAlpha { get; private set; }

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Debug.LogError("Only one GameEntityRunner can be active in a scene.");
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
            _registry = new UnityEntityViewRegistry(ViewRoot.transform, AutoCreateViews, DestroyViewsOnEntityDestroy);
            UnityEntityViewRegistry.Active = _registry;
            _observerRegistration = World.Instance.ObserveEntities(_registry);
            _fixedAccumulator = 0f;
            FixedInterpolationAlpha = 0f;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _shutdown)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            RunFixedUpdates(deltaTime);
            World.Instance.Update(deltaTime);
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

            if (_initialized && OwnsWorldLifetime)
            {
                World.Instance.Dispose();
            }

            _initialized = false;
            _fixedAccumulator = 0f;
            FixedInterpolationAlpha = 0f;
        }

        private void RunFixedUpdates(float deltaTime)
        {
            float updatesPerSecond = FixedUpdatesPerSecond;
            if (float.IsNaN(updatesPerSecond) || float.IsInfinity(updatesPerSecond) || updatesPerSecond <= 0f)
            {
                updatesPerSecond = 30f;
            }

            int maxSteps = Mathf.Max(1, MaxFixedStepsPerFrame);
            float fixedDeltaTime = 1f / updatesPerSecond;
            float maxAccumulatedTime = fixedDeltaTime * maxSteps;
            _fixedAccumulator = Mathf.Min(_fixedAccumulator + deltaTime, maxAccumulatedTime);

            int stepCount = 0;
            while (_fixedAccumulator >= fixedDeltaTime && stepCount < maxSteps)
            {
                World.Instance.FixedUpdate(fixedDeltaTime);
                _fixedAccumulator -= fixedDeltaTime;
                stepCount++;
            }

            FixedInterpolationAlpha = Mathf.Clamp01(_fixedAccumulator / fixedDeltaTime);
        }
    }
}
