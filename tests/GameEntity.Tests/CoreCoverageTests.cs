using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class CoreCoverageTests : GameEntityTestBase
    {
        [Fact]
        public void ObjectPoolReuse_ShouldInvalidateOldEntityRefAndHandle()
        {
            TestScene scene = CreateScene("p0-pool");
            PooledProbeEntity first = scene.AddPooledChild<PooledProbeEntity>();
            EntityRef<PooledProbeEntity> oldRef = first;
            EntityHandle oldHandle = first.Handle;

            first.Destroy();
            PooledProbeEntity second = scene.AddPooledChild<PooledProbeEntity>();

            Assert.Same(first, second);
            Assert.NotEqual(oldHandle.NodeId, second.Handle.NodeId);
            Assert.False(oldRef.IsAlive);
            Assert.False(oldRef.TryGet(out _));
            Assert.False(World.Instance.TryResolve(oldHandle, out PooledProbeEntity _));
            Assert.True(World.Instance.TryResolve(second.Handle, out PooledProbeEntity resolved));
            Assert.Same(second, resolved);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void WorldInstance_ShouldCreateCoreServicesWithoutManualSingletonRegistration()
        {
            var scene = (TestScene)World.Instance.AddScene("p0-world-services", new TestScene("p0-world-services"));
            TickProbeEntity entity = scene.AddChild<TickProbeEntity>();

            World.Instance.Tick(0.1f, 0.1f);

            Assert.True(scene.Handle.IsValid);
            Assert.NotEqual(0, scene.Id);
            Assert.NotEqual(0, scene.InstanceId);
            Assert.Equal(1, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void WorldDispose_ShouldClearObjectPoolBetweenWorldInstances()
        {
            TestScene firstScene = CreateScene("p0-pool-world-a");
            PooledProbeEntity first = firstScene.AddPooledChild<PooledProbeEntity>();

            first.Destroy();
            World.Instance.Dispose();

            var secondScene = (TestScene)World.Instance.AddScene("p0-pool-world-b", new TestScene("p0-pool-world-b"));
            PooledProbeEntity second = secondScene.AddPooledChild<PooledProbeEntity>();

            Assert.NotSame(first, second);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void IHasUpdateStrategy_ShouldControlEntitySchedulerUpdateCount()
        {
            TestScene scene = CreateScene("p0-strategy");
            StrategyProbeEntity entity = scene.AddChild<StrategyProbeEntity>();

            World.Instance.Tick(0.3f, 9f);

            Assert.Equal(3, entity.UpdateCount);
            Assert.Equal(0.1f, entity.LastDeltaTime, precision: 5);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void IEntityLifecycleGate_ShouldBlockAndAllowUpdate()
        {
            TestScene scene = CreateScene("p0-gate");
            GateProbeEntity entity = scene.AddChild<GateProbeEntity>();

            World.Instance.Tick(0.1f, 0.1f);
            Assert.Equal(0, entity.UpdateCount);

            entity.CanRun = true;
            World.Instance.Tick(0.1f, 0.1f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void IDependentComponent_ShouldGateSchedulerUntilDependencyIsReady()
        {
            TestScene scene = CreateScene("p0-dependent");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            DependentTickComponent dependent = owner.AddComponent<DependentTickComponent>();

            World.Instance.Tick(0.1f, 0.1f);
            Assert.Equal(0, dependent.UpdateCount);
            Assert.False(dependent.AreAllDependenciesMet);

            RequiredComponent required = owner.AddComponent<RequiredComponent>();
            World.Instance.Tick(0.1f, 0.1f);
            Assert.Equal(1, dependent.UpdateCount);
            Assert.True(dependent.AreAllDependenciesMet);

            required.IsReady = false;
            World.Instance.Tick(0.1f, 0.1f);
            Assert.Equal(1, dependent.UpdateCount);
            Assert.False(dependent.AreAllDependenciesMet);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RemoveChild_ShouldDestroyOnlyMatchingChildSubtree()
        {
            TestScene scene = CreateScene("p0-remove-child");
            ProbeEntity keep = scene.AddChild<ProbeEntity>();
            ProbeEntity remove = scene.AddChild<ProbeEntity>();
            ProbeComponent component = remove.AddComponent<ProbeComponent>();

            scene.RemoveChild(remove.Id);

            Assert.False(keep.IsDestroyed);
            Assert.True(remove.IsDestroyed);
            Assert.True(component.IsDestroyed);
            Assert.True(scene.ContainsChild(keep.Id));
            Assert.False(scene.ContainsChild(remove.Id));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ClearChildren_ShouldDestroyAllChildrenAndKeepSceneValid()
        {
            TestScene scene = CreateScene("p0-clear-children");
            ProbeEntity first = scene.AddChild<ProbeEntity>();
            ProbeEntity second = scene.AddChild<ProbeEntity>();

            scene.ClearChildren();

            Assert.True(first.IsDestroyed);
            Assert.True(second.IsDestroyed);
            Assert.Equal(0, scene.ChildrenCount());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void WorldDispose_ShouldDestroyScenesAndInvalidateHandles()
        {
            TestScene scene = CreateScene("p0-world-destroy");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityHandle handle = entity.Handle;

            World.Instance.Dispose();

            Assert.True(scene.IsDestroyed);
            Assert.True(entity.IsDestroyed);
            Assert.False(World.Instance.TryResolve(handle, out ProbeEntity _));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void SceneDestroy_ShouldDestroySceneSubtreeAndRemoveHierarchyNodes()
        {
            TestScene scene = CreateScene("p0-scene-destroy");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityHandle sceneHandle = scene.Handle;
            EntityHandle entityHandle = entity.Handle;

            scene.Destroy();

            Assert.True(scene.IsDestroyed);
            Assert.True(entity.IsDestroyed);
            Assert.Null(World.Instance.GetScene("p0-scene-destroy"));
            Assert.False(World.Instance.TryResolve(sceneHandle, out TestScene _));
            Assert.False(World.Instance.TryResolve(entityHandle, out ProbeEntity _));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ValidateEntities_ShouldReportObjectStoreMissingNode()
        {
            TestScene scene = CreateScene("p0-validation-negative");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();

            RemoveObjectStoreEntry(entity.Handle.NodeId);

            EntityValidationResult result = World.Instance.ValidateEntities();

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Code == "ObjectMissing" && issue.NodeId == entity.Handle.NodeId);
        }

        private static void RemoveObjectStoreEntry(long nodeId)
        {
            object hierarchy = typeof(World)
                .GetProperty("Hierarchy", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(World.Instance);
            object objectStore = hierarchy.GetType()
                .GetProperty("Objects", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(hierarchy);
            objectStore.GetType()
                .GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(objectStore, new object[] { nodeId });
        }
    }
}
