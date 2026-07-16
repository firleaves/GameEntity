using System;
using System.Linq;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityStartAndUpdateRequirementTests : GameEntityTestBase
    {
        [Fact]
        public void AwakeArguments_ShouldBeStoredBeforeDependenciesExist()
        {
            TestScene scene = CreateScene("start-arguments");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            ParameterizedDependentUpdateComponent dependent =
                owner.AddComponent<ParameterizedDependentUpdateComponent, string>("movement-config");

            Assert.Equal("movement-config", dependent.Configuration);
            Assert.Equal(1, dependent.AwakeCount);
            Assert.Equal(0, dependent.StartCount);

            World.Instance.Update(0.1f);

            Assert.Equal(0, dependent.StartCount);
            Assert.Equal(0, dependent.UpdateCount);
            Assert.Contains(
                World.Instance.ValidateEntities().Issues,
                issue => issue.Code == "UpdateRequirementMissing" && issue.NodeId == dependent.Handle.NodeId);
        }

        [Fact]
        public void Start_ShouldRunOnceAndUpdateInSameUpdatePassWhenRequirementBecomesReady()
        {
            TestScene scene = CreateScene("start-ready");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            ParameterizedDependentUpdateComponent dependent =
                owner.AddComponent<ParameterizedDependentUpdateComponent, string>("movement-config");
            RequiredComponent required = owner.AddComponent<RequiredComponent>();
            required.IsReady = false;

            World.Instance.Update(0.1f);
            Assert.Equal(0, dependent.StartCount);
            Assert.Equal(0, dependent.UpdateCount);

            required.IsReady = true;
            World.Instance.Update(0.1f);

            Assert.Equal(1, dependent.StartCount);
            Assert.Equal(1, dependent.UpdateCount);
            Assert.Same(required, dependent.DependencyAtStart);
            Assert.True(World.Instance.CaptureEntitySnapshot().Nodes
                .Single(node => node.NodeId == dependent.Handle.NodeId)
                .IsStarted);

            World.Instance.Update(0.1f);
            Assert.Equal(1, dependent.StartCount);
            Assert.Equal(2, dependent.UpdateCount);
        }

        [Fact]
        public void UpdateState_ShouldBlockStartAndReadyChangesShouldOnlyBlockUpdateAfterStart()
        {
            TestScene scene = CreateScene("start-gates");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            RequiredComponent required = owner.AddComponent<RequiredComponent>();
            ParameterizedDependentUpdateComponent dependent =
                owner.AddComponent<ParameterizedDependentUpdateComponent, string>("movement-config");
            dependent.IsUpdateEnabled = false;

            World.Instance.Update(0.1f);
            Assert.Equal(0, dependent.StartCount);

            dependent.IsUpdateEnabled = true;
            World.Instance.Update(0.1f);
            Assert.Equal(1, dependent.StartCount);
            Assert.Equal(1, dependent.UpdateCount);

            required.IsReady = false;
            World.Instance.Update(0.1f);
            Assert.Equal(1, dependent.StartCount);
            Assert.Equal(1, dependent.UpdateCount);

            required.IsReady = true;
            World.Instance.Update(0.1f);
            Assert.Equal(1, dependent.StartCount);
            Assert.Equal(2, dependent.UpdateCount);
        }

        [Fact]
        public void StartOnlyEntity_ShouldRunOnceAndLeaveScheduler()
        {
            TestScene scene = CreateScene("start-only");
            StartOnlyProbeEntity entity = scene.AddChild<StartOnlyProbeEntity>();

            World.Instance.Update(0.1f);
            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.StartCount);
            Assert.True(World.Instance.CaptureEntitySnapshot().Nodes
                .Single(node => node.NodeId == entity.Handle.NodeId)
                .IsStarted);
        }

        [Fact]
        public void EntityDestroyedWhileWaiting_ShouldDestroyWithoutStarting()
        {
            TestScene scene = CreateScene("destroy-before-start");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            ParameterizedDependentUpdateComponent dependent =
                owner.AddComponent<ParameterizedDependentUpdateComponent, string>("movement-config");

            dependent.Destroy();

            Assert.Equal(1, dependent.AwakeCount);
            Assert.Equal(0, dependent.StartCount);
            Assert.Equal(0, dependent.UpdateCount);
            Assert.Equal(1, dependent.DestroyCount);
            Assert.True(dependent.IsDestroyed);
            Assert.Null(owner.GetComponent<ParameterizedDependentUpdateComponent>());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void PooledEntity_ShouldReceiveFreshStartStateForEachLifetime()
        {
            TestScene scene = CreateScene("start-pooled");
            PooledStartProbeEntity first = scene.AddPooledChild<PooledStartProbeEntity>();
            World.Instance.Update(0.1f);
            EntityHandle oldHandle = first.Handle;
            Assert.Equal(1, first.StartCount);

            first.Destroy();
            PooledStartProbeEntity second = scene.AddPooledChild<PooledStartProbeEntity>();
            Assert.Same(first, second);
            Assert.NotEqual(oldHandle, second.Handle);
            Assert.Equal(0, second.StartCount);

            World.Instance.Update(0.1f);
            Assert.Equal(1, second.StartCount);
        }

        [Fact]
        public void StartFailure_ShouldFaultOnceBlockUpdateAndRemainDestroyable()
        {
            TestScene scene = CreateScene("start-fault");
            FaultingStartEntity entity = scene.AddChild<FaultingStartEntity>();

            World.Instance.Update(0.1f);
            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.StartCount);
            Assert.Equal(0, entity.UpdateCount);
            EntityNodeInfo node = World.Instance.CaptureEntitySnapshot().Nodes
                .Single(item => item.NodeId == entity.Handle.NodeId);
            Assert.True(node.IsStartFaulted);
            Assert.False(World.Instance.ValidateEntities().IsValid);
            Assert.Contains(World.Instance.ValidateEntities().Issues, issue => issue.Code == "StartFaulted");

            entity.Destroy();
            Assert.Equal(1, entity.DestroyCount);
            Assert.True(entity.IsDestroyed);
        }

        [Fact]
        public void AwakeFailure_ShouldRollbackAttachedComponentAndRethrow()
        {
            TestScene scene = CreateScene("awake-fault");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            var signals = new LifecycleSignals();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => owner.AddComponent<FaultingAwakeComponent, LifecycleSignals>(signals));

            Assert.Equal("awake failed", exception.Message);
            Assert.Equal(1, signals.AwakeCount);
            Assert.Equal(1, signals.DestroyCount);
            Assert.Null(owner.GetComponent<FaultingAwakeComponent>());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RequireForUpdateOnChild_ShouldRollbackAndRethrow()
        {
            TestScene scene = CreateScene("invalid-dependent-child");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => scene.AddChild<InvalidDependentChild>());

            Assert.Contains("not attached as a Component", exception.Message);
            Assert.Empty(scene.Children);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RequireForUpdateOnScene_ShouldRejectBeforeRegistration()
        {
            var scene = new InvalidDependentScene("invalid-dependent-scene");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => World.Instance.AddScene(scene.Name, scene));

            Assert.Contains("Scene roots cannot declare Update requirements", exception.Message);
            Assert.Null(World.Instance.GetScene(scene.Name));
            Assert.False(scene.Handle.IsValid);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void UpdateDependentComponent_ShouldRejectReparentAsChildAndKeepOwner()
        {
            TestScene scene = CreateScene("dependent-reparent");
            ProbeEntity firstOwner = scene.AddChild<ProbeEntity>();
            ProbeEntity secondOwner = scene.AddChild<ProbeEntity>();
            DependentUpdateComponent dependent = firstOwner.AddComponent<DependentUpdateComponent>();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => dependent.ReparentTo(secondOwner));

            Assert.Contains("must remain attached as a Component", exception.Message);
            Assert.Same(firstOwner, dependent.Owner);
            Assert.Same(dependent, firstOwner.GetComponent<DependentUpdateComponent>());
            Assert.DoesNotContain(dependent, secondOwner.Children);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RequireForUpdateCycle_ShouldRollbackCreationAndDescribeCycle()
        {
            TestScene scene = CreateScene("dependency-cycle");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => owner.AddComponent<CyclicUpdateComponentA>());

            Assert.Contains("RequireForUpdate cycle", exception.Message);
            Assert.Contains(typeof(CyclicUpdateComponentA).FullName, exception.Message);
            Assert.Contains(typeof(CyclicUpdateComponentB).FullName, exception.Message);
            Assert.Empty(owner.Components);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }
}
