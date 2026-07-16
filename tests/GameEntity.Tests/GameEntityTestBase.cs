using System;

namespace GameEntity.Tests
{
    public abstract class GameEntityTestBase : IDisposable
    {
        protected GameEntityTestBase()
        {
            ResetWorld();
        }

        public void Dispose()
        {
            World.Instance.Dispose();
        }

        protected static TestScene CreateScene(string name)
        {
            return (TestScene)World.Instance.AddScene(name, new TestScene(name));
        }

        private static void ResetWorld()
        {
            World.Instance.Dispose();
        }
    }

    public sealed class TestScene : Scene
    {
        public TestScene(string name) : base(name)
        {
        }
    }

    [RequireForUpdate(typeof(RequiredComponent))]
    public sealed class InvalidDependentScene : Scene
    {
        public InvalidDependentScene(string name) : base(name)
        {
        }
    }

    public sealed class ProbeEntity : Entity, IAwake, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class ProbeComponent : Entity, IAwake, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class UpdateProbeEntity : Entity, IAwake, IUpdate, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public float LastDeltaTime { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void Update(float time)
        {
            UpdateCount++;
            LastDeltaTime = time;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class PooledProbeEntity : Entity, IAwake, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class FixedUpdateProbeEntity : Entity, IAwake, IStart, IFixedUpdate, IEntityUpdateState
    {
        public bool IsUpdateEnabled { get; set; } = true;

        public int StartCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public float LastFixedDeltaTime { get; private set; }

        public void Awake()
        {
            StartCount = 0;
            FixedUpdateCount = 0;
            LastFixedDeltaTime = 0f;
            IsUpdateEnabled = true;
        }

        public void Start()
        {
            StartCount++;
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
            LastFixedDeltaTime = fixedDeltaTime;
        }
    }

    public sealed class DualUpdateProbeEntity : Entity, IAwake, IStart, IFixedUpdate, IUpdate
    {
        public int StartCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Start()
        {
            StartCount++;
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    public sealed class RateLimitedProbeEntity : Entity, IAwake, IStart, IUpdate, IEntityUpdateInterval, IEntityUpdateState
    {
        public float UpdateInterval { get; set; } = 0.3f;

        public bool IsUpdateEnabled { get; set; } = true;

        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public float LastDeltaTime { get; private set; }

        public void Awake()
        {
            UpdateInterval = 0.3f;
            IsUpdateEnabled = true;
            StartCount = 0;
            UpdateCount = 0;
            LastDeltaTime = 0f;
        }

        public void Start()
        {
            StartCount++;
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            LastDeltaTime = deltaTime;
        }
    }

    public sealed class InvalidUpdateIntervalEntity : Entity, IAwake, IEntityUpdateInterval
    {
        public float UpdateInterval => 0.1f;

        public void Awake()
        {
        }
    }

    public sealed class ThrowingUpdateIntervalEntity : Entity, IAwake, IUpdate, IEntityUpdateInterval
    {
        public bool ThrowOnRead { get; set; } = true;

        public int UpdateCount { get; private set; }

        public float LastDeltaTime { get; private set; }

        public float UpdateInterval
        {
            get
            {
                if (ThrowOnRead)
                {
                    throw new InvalidOperationException("update interval failed");
                }

                return 0.2f;
            }
        }

        public void Awake()
        {
            ThrowOnRead = true;
            UpdateCount = 0;
            LastDeltaTime = 0f;
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            LastDeltaTime = deltaTime;
        }
    }

    public sealed class UpdateStateProbeEntity : Entity, IAwake, IUpdate, IEntityUpdateState
    {
        public bool IsUpdateEnabled { get; set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

    }

    public sealed class ReadyStateUpdateProbeEntity : Entity, IAwake, IUpdate, IEntityReadyState
    {
        public bool IsReady { get; set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
        }
    }

    public sealed class RequiredComponent : Entity, IAwake, IEntityReadyState
    {
        public bool IsReady { get; set; } = true;

        public void Awake()
        {
        }
    }

    [RequireForUpdate(typeof(RequiredComponent))]
    public sealed class DependentUpdateComponent : Entity, IAwake, IStart, IUpdate
    {
        public int AwakeCount { get; private set; }

        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void Start()
        {
            StartCount++;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }
    }

    [RequireForUpdate(typeof(RequiredComponent))]
    public sealed class ParameterizedDependentUpdateComponent : Entity, IAwake<string>, IStart, IUpdate, IDestroy, IEntityUpdateState
    {
        public string Configuration { get; private set; }

        public int AwakeCount { get; private set; }

        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public RequiredComponent DependencyAtStart { get; private set; }

        public bool IsUpdateEnabled { get; set; } = true;

        public void Awake(string configuration)
        {
            Configuration = configuration;
            AwakeCount++;
        }

        public void Start()
        {
            DependencyAtStart = Owner.GetComponent<RequiredComponent>();
            StartCount++;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class StartOnlyProbeEntity : Entity, IAwake, IStart
    {
        public int StartCount { get; private set; }

        public void Awake()
        {
        }

        public void Start()
        {
            StartCount++;
        }
    }

    public sealed class PooledStartProbeEntity : Entity, IAwake, IStart
    {
        public int StartCount { get; private set; }

        public void Awake()
        {
            StartCount = 0;
        }

        public void Start()
        {
            StartCount++;
        }
    }

    public sealed class FaultingStartEntity : Entity, IAwake, IStart, IUpdate, IDestroy
    {
        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
        }

        public void Start()
        {
            StartCount++;
            throw new InvalidOperationException("start failed");
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    public sealed class LifecycleSignals
    {
        public int AwakeCount { get; set; }

        public int DestroyCount { get; set; }
    }

    public sealed class FaultingAwakeComponent : Entity, IAwake<LifecycleSignals>, IDestroy
    {
        private LifecycleSignals _signals;

        public void Awake(LifecycleSignals signals)
        {
            _signals = signals;
            _signals.AwakeCount++;
            throw new InvalidOperationException("awake failed");
        }

        public void OnDestroy()
        {
            _signals.DestroyCount++;
        }
    }

    [RequireForUpdate(typeof(RequiredComponent))]
    public sealed class InvalidDependentChild : Entity, IAwake, IUpdate
    {
        public void Awake()
        {
        }

        public void Update(float time)
        {
        }
    }

    [RequireForUpdate(typeof(RequiredComponent))]
    public sealed class FixedDependentUpdateComponent : Entity, IAwake, IStart, IFixedUpdate
    {
        public int StartCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Start()
        {
            StartCount++;
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
        }
    }

    public sealed class ThrowingUpdateStateEntity : Entity, IAwake, IUpdate, IEntityUpdateState
    {
        public bool IsUpdateEnabled => throw new InvalidOperationException("update state failed");

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    public sealed class ThrowingReadyComponent : Entity, IAwake, IEntityReadyState
    {
        public bool IsReady => throw new InvalidOperationException("ready state failed");

        public void Awake()
        {
        }
    }

    [RequireForUpdate(typeof(ThrowingReadyComponent))]
    public sealed class ThrowingReadyDependentComponent : Entity, IAwake, IUpdate
    {
        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    [RequireForUpdate(typeof(CyclicUpdateComponentB))]
    public sealed class CyclicUpdateComponentA : Entity, IAwake, IUpdate
    {
        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
        }
    }

    [RequireForUpdate(typeof(CyclicUpdateComponentA))]
    public sealed class CyclicUpdateComponentB : Entity, IAwake, IUpdate
    {
        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
        }
    }
}
