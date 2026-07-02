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

    public sealed class TickProbeEntity : Entity, IAwake, IUpdate, IDestroy
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

    public sealed class StrategyProbeEntity : Entity, IAwake, IUpdate, IHasUpdateStrategy
    {
        private readonly FixedCountUpdateStrategy _strategy = new FixedCountUpdateStrategy(3);

        public int UpdateCount { get; private set; }

        public float LastDeltaTime { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
            LastDeltaTime = time;
        }

        public IUpdateStrategy GetUpdateStrategy()
        {
            return _strategy;
        }
    }

    public sealed class GateProbeEntity : Entity, IAwake, IUpdate, IEntityLifecycleGate
    {
        public bool IsReady { get; set; } = true;

        public bool CanRun { get; set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

    }

    public sealed class RequiredComponent : Entity, IAwake, IEntityLifecycleGate
    {
        public bool IsReady { get; set; } = true;

        public bool CanRun { get; set; } = true;

        public void Awake()
        {
        }
    }

    [DependsOn(typeof(RequiredComponent))]
    public sealed class DependentTickComponent : DependentComponentBase, IAwake, IUpdate
    {
        public int UpdateCount { get; private set; }

        public int ActivationChangedCount { get; private set; }

        public bool LastActivationState { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        protected override void OnActivationChanged(bool isActive)
        {
            ActivationChangedCount++;
            LastActivationState = isActive;
        }
    }

    public sealed class FixedCountUpdateStrategy : IUpdateStrategy
    {
        private readonly int _count;

        public FixedCountUpdateStrategy(int count)
        {
            _count = count;
        }

        public int GetUpdateCount(Entity entity, float deltaTime, float unscaledDeltaTime, out float singleDeltaTime)
        {
            singleDeltaTime = deltaTime / _count;
            return _count;
        }
    }
}
