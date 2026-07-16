using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace GameEntity.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, GameEntityBenchmarkConfig.Instance);
        }
    }

    public sealed class GameEntityBenchmarkConfig : ManualConfig
    {
        public static readonly GameEntityBenchmarkConfig Instance = new GameEntityBenchmarkConfig();

        private GameEntityBenchmarkConfig()
        {
            AddJob(Job.ShortRun
                .WithRuntime(CoreRuntime.Core80)
                .WithId("Core80-Short"));

            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(StatisticColumn.P95);
            AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Unicode);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(HtmlExporter.Default);

            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
            WithOptions(ConfigOptions.JoinSummary);
        }
    }

    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class EntityHierarchyBenchmarks
    {
        [Params(100, 1000, 5000)]
        public int EntityCount { get; set; }

        private BenchScene _scene;
        private BenchEntity[] _entities;
        private EntityHandle[] _handles;
        private BenchScene _sceneA;
        private BenchScene _sceneB;
        private BenchEntity[] _movableEntities;

        [IterationSetup(Targets = new[] { nameof(CreateChildren), nameof(CreateChildrenWithComponent) })]
        public void SetupCreateBenchmarks()
        {
            ResetWorld();
            _scene = CreateScene("create");
        }

        [IterationSetup(Targets = new[] { nameof(QueryComponents), nameof(ResolveHandles), nameof(CaptureSnapshot), nameof(ValidateHierarchy) })]
        public void SetupReadBenchmarks()
        {
            ResetWorld();
            _scene = CreateScene("read");
            _entities = new BenchEntity[EntityCount];
            _handles = new EntityHandle[EntityCount];

            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _scene.AddChild<BenchEntity>();
                entity.AddComponent<BenchComponent>();
                _entities[i] = entity;
                _handles[i] = entity.Handle;
            }
        }

        [IterationSetup(Target = nameof(ReparentAcrossScenes))]
        public void SetupReparentBenchmark()
        {
            ResetWorld();
            _sceneA = CreateScene("scene-a");
            _sceneB = CreateScene("scene-b");
            _movableEntities = new BenchEntity[EntityCount];

            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _sceneA.AddChild<BenchEntity>();
                entity.AddChild<BenchLeafEntity>();
                entity.AddComponent<BenchComponent>();
                _movableEntities[i] = entity;
            }
        }

        [IterationCleanup]
        public void CleanupWorld()
        {
            World.Instance.Dispose();
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Create")]
        public long CreateChildren()
        {
            long checksum = 0;

            for (int i = 0; i < EntityCount; i++)
            {
                checksum += _scene.AddChild<BenchEntity>().Handle.NodeId;
            }

            return checksum;
        }

        [Benchmark]
        [BenchmarkCategory("Create")]
        public long CreateChildrenWithComponent()
        {
            long checksum = 0;

            for (int i = 0; i < EntityCount; i++)
            {
                var entity = _scene.AddChild<BenchEntity>();
                var component = entity.AddComponent<BenchComponent>();
                checksum += entity.Handle.NodeId + component.Handle.NodeId;
            }

            return checksum;
        }

        [Benchmark]
        [BenchmarkCategory("Read")]
        public long QueryComponents()
        {
            long checksum = 0;

            for (int i = 0; i < _entities.Length; i++)
            {
                if (_entities[i].TryGetComponent<BenchComponent>(out var component))
                {
                    checksum += component.Handle.NodeId;
                }
            }

            return checksum;
        }

        [Benchmark]
        [BenchmarkCategory("Read")]
        public long ResolveHandles()
        {
            long checksum = 0;

            for (int i = 0; i < _handles.Length; i++)
            {
                if (World.Instance.TryResolve(_handles[i], out BenchEntity entity))
                {
                    checksum += entity.Handle.NodeId;
                }
            }

            return checksum;
        }

        [Benchmark]
        [BenchmarkCategory("Diagnostics")]
        public int CaptureSnapshot()
        {
            return World.Instance.CaptureEntitySnapshot().Nodes.Count;
        }

        [Benchmark]
        [BenchmarkCategory("Diagnostics")]
        public bool ValidateHierarchy()
        {
            return World.Instance.ValidateEntities().IsValid;
        }

        [Benchmark]
        [BenchmarkCategory("Reparent")]
        public long ReparentAcrossScenes()
        {
            long checksum = 0;

            for (int i = 0; i < _movableEntities.Length; i++)
            {
                var entity = _movableEntities[i];
                entity.ReparentTo(_sceneB);
                checksum += entity.GetSceneRoot().Handle.NodeId;
            }

            return checksum;
        }

        private static BenchScene CreateScene(string name)
        {
            return (BenchScene)World.Instance.AddScene(name, new BenchScene(name));
        }

        private static void ResetWorld()
        {
            World.Instance.Dispose();
        }
    }

    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class EntitySchedulerBenchmarks
    {
        [Params(100, 1000, 5000)]
        public int EntityCount { get; set; }

        [Params(1, 10)]
        public int FrameCount { get; set; }

        private BenchScene _scene;
        private UpdatingEntity[] _entities;

        [IterationSetup]
        public void Setup()
        {
            World.Instance.Dispose();
            _scene = (BenchScene)World.Instance.AddScene("scheduler", new BenchScene("scheduler"));
            _entities = new UpdatingEntity[EntityCount];

            for (int i = 0; i < EntityCount; i++)
            {
                _entities[i] = _scene.AddChild<UpdatingEntity>();
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
            World.Instance.Dispose();
        }

        [Benchmark]
        [BenchmarkCategory("Scheduler")]
        public int UpdateRegisteredEntities()
        {
            for (int frame = 0; frame < FrameCount; frame++)
            {
                World.Instance.Update(0.016f);
            }

            int checksum = 0;
            for (int i = 0; i < _entities.Length; i++)
            {
                checksum += _entities[i].UpdateCount;
            }

            return checksum;
        }
    }

    public sealed class BenchScene : Scene
    {
        public BenchScene(string name) : base(name)
        {
        }

        public override void Awake()
        {
        }

        public override void OnDestroy()
        {
        }
    }

    public sealed class BenchEntity : Entity, IAwake, IDestroy
    {
        public void Awake()
        {
        }

        public void OnDestroy()
        {
        }
    }

    public sealed class BenchLeafEntity : Entity, IAwake, IDestroy
    {
        public void Awake()
        {
        }

        public void OnDestroy()
        {
        }
    }

    public sealed class BenchComponent : Entity, IAwake, IDestroy
    {
        public void Awake()
        {
        }

        public void OnDestroy()
        {
        }
    }

    public sealed class UpdatingEntity : Entity, IAwake, IUpdate, IDestroy
    {
        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
        }
    }
}
