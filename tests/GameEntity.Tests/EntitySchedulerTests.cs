using System;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntitySchedulerTests : GameEntityTestBase
    {
        [Fact]
        public void Update_ShouldRunThroughEntityScheduler()
        {
            TestScene scene = CreateScene("scheduler-basic");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.25f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.Equal(0.25f, entity.LastDeltaTime);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RepeatedRegister_ShouldNotRunEntityMoreThanOncePerFrame()
        {
            TestScene scene = CreateScene("scheduler-dedup");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Hierarchy.Scheduler.Register(entity);
            World.Instance.Hierarchy.Scheduler.Register(entity);
            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.UpdateCount);
        }

        [Fact]
        public void DestroyedEntity_ShouldNotRunFromSchedulerOldHandle()
        {
            TestScene scene = CreateScene("scheduler-destroy");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();

            entity.Destroy();
            World.Instance.Update(0.1f);

            Assert.Equal(0, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ReparentAcrossScenes_ShouldMoveScheduledEntityToNewSceneBucket()
        {
            TestScene sceneA = CreateScene("scheduler-a");
            TestScene sceneB = CreateScene("scheduler-b");
            UpdateProbeEntity entity = sceneA.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);
            entity.ReparentTo(sceneB);
            World.Instance.Update(0.1f);

            Assert.Equal(2, entity.UpdateCount);
            Assert.Same(sceneB, entity.GetSceneRoot());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RemovedScene_ShouldStopRunningItsScheduledEntities()
        {
            TestScene scene = CreateScene("scheduler-remove-scene");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();

            World.Instance.RemoveScene("scheduler-remove-scene");
            World.Instance.Update(0.1f);

            Assert.True(entity.IsDestroyed);
            Assert.Equal(0, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void FixedUpdate_ShouldRunOnlyFixedUpdateEntities()
        {
            TestScene scene = CreateScene("scheduler-fixed");
            FixedUpdateProbeEntity fixedEntity = scene.AddChild<FixedUpdateProbeEntity>();
            UpdateProbeEntity updateEntity = scene.AddChild<UpdateProbeEntity>();

            World.Instance.FixedUpdate(1f / 30f);

            Assert.Equal(1, fixedEntity.StartCount);
            Assert.Equal(1, fixedEntity.FixedUpdateCount);
            Assert.Equal(1f / 30f, fixedEntity.LastFixedDeltaTime, precision: 6);
            Assert.Equal(0, updateEntity.UpdateCount);

            World.Instance.Update(1f / 60f);

            Assert.Equal(1, fixedEntity.FixedUpdateCount);
            Assert.Equal(1, updateEntity.UpdateCount);
        }

        [Fact]
        public void EntityImplementingBothUpdatePhases_ShouldStartOnce()
        {
            TestScene scene = CreateScene("scheduler-dual-phase");
            DualUpdateProbeEntity entity = scene.AddChild<DualUpdateProbeEntity>();

            World.Instance.FixedUpdate(1f / 30f);
            World.Instance.Update(1f / 60f);

            Assert.Equal(1, entity.StartCount);
            Assert.Equal(1, entity.FixedUpdateCount);
            Assert.Equal(1, entity.UpdateCount);
        }

        [Fact]
        public void UpdateState_ShouldGateStartAndFixedUpdate()
        {
            TestScene scene = CreateScene("scheduler-fixed-disabled");
            FixedUpdateProbeEntity entity = scene.AddChild<FixedUpdateProbeEntity>();
            entity.IsUpdateEnabled = false;

            World.Instance.FixedUpdate(1f / 30f);

            Assert.Equal(0, entity.StartCount);
            Assert.Equal(0, entity.FixedUpdateCount);

            entity.IsUpdateEnabled = true;
            World.Instance.FixedUpdate(1f / 30f);

            Assert.Equal(1, entity.StartCount);
            Assert.Equal(1, entity.FixedUpdateCount);
        }

        [Fact]
        public void UpdateInterval_ShouldCallAtMostOnceWithEntireElapsedTime()
        {
            TestScene scene = CreateScene("scheduler-update-rate-single-call");
            RateLimitedProbeEntity entity = scene.AddChild<RateLimitedProbeEntity>();
            entity.UpdateInterval = 0.1f;

            World.Instance.Update(0.35f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.Equal(0.35f, entity.LastDeltaTime, precision: 6);
        }

        [Fact]
        public void UpdateInterval_ShouldKeepIndependentElapsedTimePerEntity()
        {
            TestScene scene = CreateScene("scheduler-update-rate-independent");
            RateLimitedProbeEntity first = scene.AddChild<RateLimitedProbeEntity>();

            World.Instance.Update(0.2f);

            RateLimitedProbeEntity second = scene.AddChild<RateLimitedProbeEntity>();
            World.Instance.Update(0.1f);

            Assert.Equal(1, first.UpdateCount);
            Assert.Equal(0, second.UpdateCount);

            World.Instance.Update(0.2f);

            Assert.Equal(1, first.UpdateCount);
            Assert.Equal(1, second.UpdateCount);
        }

        [Fact]
        public void UpdateInterval_ShouldNotAccumulateWhileUpdateIsDisabled()
        {
            TestScene scene = CreateScene("scheduler-update-rate-disabled");
            RateLimitedProbeEntity entity = scene.AddChild<RateLimitedProbeEntity>();
            entity.UpdateInterval = 0.2f;
            entity.IsUpdateEnabled = false;

            World.Instance.Update(0.2f);
            entity.IsUpdateEnabled = true;
            World.Instance.Update(0.1f);

            Assert.Equal(0, entity.UpdateCount);

            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.Equal(0.2f, entity.LastDeltaTime, precision: 6);
        }

        [Fact]
        public void InvalidUpdateInterval_ShouldSkipOnlyThatEntityAndResetElapsedTime()
        {
            TestScene scene = CreateScene("scheduler-update-rate-invalid");
            RateLimitedProbeEntity invalid = scene.AddChild<RateLimitedProbeEntity>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();
            invalid.UpdateInterval = float.NaN;

            World.Instance.Update(0.2f);

            Assert.Equal(0, invalid.UpdateCount);
            Assert.Equal(1, valid.UpdateCount);

            invalid.UpdateInterval = 0.3f;
            World.Instance.Update(0.3f);

            Assert.Equal(1, invalid.UpdateCount);
            Assert.Equal(0.3f, invalid.LastDeltaTime, precision: 6);
        }

        [Fact]
        public void ThrowingUpdateInterval_ShouldSkipOnlyThatEntityAndResetElapsedTime()
        {
            TestScene scene = CreateScene("scheduler-update-interval-throwing");
            ThrowingUpdateIntervalEntity faulting = scene.AddChild<ThrowingUpdateIntervalEntity>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);

            Assert.Equal(0, faulting.UpdateCount);
            Assert.Equal(1, valid.UpdateCount);

            faulting.ThrowOnRead = false;
            World.Instance.Update(0.1f);

            Assert.Equal(0, faulting.UpdateCount);

            World.Instance.Update(0.1f);

            Assert.Equal(1, faulting.UpdateCount);
            Assert.Equal(0.2f, faulting.LastDeltaTime, precision: 6);
        }

        [Fact]
        public void ReparentAcrossScenes_ShouldPreserveUpdateIntervalElapsedTime()
        {
            TestScene sceneA = CreateScene("scheduler-rate-a");
            TestScene sceneB = CreateScene("scheduler-rate-b");
            RateLimitedProbeEntity entity = sceneA.AddChild<RateLimitedProbeEntity>();
            entity.UpdateInterval = 0.2f;

            World.Instance.Update(0.1f);
            entity.ReparentTo(sceneB);
            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.Equal(0.2f, entity.LastDeltaTime, precision: 6);
            Assert.Same(sceneB, entity.GetSceneRoot());
        }

        [Fact]
        public void PooledEntity_ShouldNotKeepPreviousUpdateIntervalElapsedTime()
        {
            TestScene scene = CreateScene("scheduler-rate-pooled");
            RateLimitedProbeEntity first = scene.AddPooledChild<RateLimitedProbeEntity>();

            World.Instance.Update(0.2f);
            first.Destroy();

            RateLimitedProbeEntity second = scene.AddPooledChild<RateLimitedProbeEntity>();
            Assert.Same(first, second);

            World.Instance.Update(0.1f);

            Assert.Equal(0, second.UpdateCount);
        }

        [Fact]
        public void RequireForUpdate_ShouldAlsoGateFixedUpdate()
        {
            TestScene scene = CreateScene("scheduler-fixed-dependency");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            FixedDependentUpdateComponent fixedEntity = owner.AddComponent<FixedDependentUpdateComponent>();

            World.Instance.FixedUpdate(1f / 30f);

            Assert.Equal(0, fixedEntity.StartCount);
            Assert.Equal(0, fixedEntity.FixedUpdateCount);

            owner.AddComponent<RequiredComponent>();
            World.Instance.FixedUpdate(1f / 30f);

            Assert.Equal(1, fixedEntity.StartCount);
            Assert.Equal(1, fixedEntity.FixedUpdateCount);
        }

        [Fact]
        public void IEntityUpdateIntervalWithoutIUpdate_ShouldRollbackCreation()
        {
            TestScene scene = CreateScene("scheduler-invalid-update-rate");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => scene.AddChild<InvalidUpdateIntervalEntity>());

            Assert.Contains("does not implement IUpdate", exception.Message);
            Assert.Empty(scene.Children);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void WorldUpdateEntrypoints_ShouldRejectInvalidDeltaTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => World.Instance.Update(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => World.Instance.Update(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => World.Instance.FixedUpdate(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => World.Instance.FixedUpdate(float.PositiveInfinity));
        }

        [Fact]
        public void ThrowingUpdateState_ShouldSkipOnlyFaultingEntity()
        {
            TestScene scene = CreateScene("scheduler-throwing-update-state");
            ThrowingUpdateStateEntity faulting = scene.AddChild<ThrowingUpdateStateEntity>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);

            Assert.Equal(0, faulting.UpdateCount);
            Assert.Equal(1, valid.UpdateCount);
        }

        [Fact]
        public void ThrowingReadyState_ShouldSkipDependentAndReportValidationIssue()
        {
            TestScene scene = CreateScene("scheduler-throwing-ready-state");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            owner.AddComponent<ThrowingReadyComponent>();
            ThrowingReadyDependentComponent dependent = owner.AddComponent<ThrowingReadyDependentComponent>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);
            EntityValidationResult validation = World.Instance.ValidateEntities();

            Assert.Equal(0, dependent.UpdateCount);
            Assert.Equal(1, valid.UpdateCount);
            Assert.Contains(
                validation.Issues,
                issue => issue.Code == "UpdateRequirementStateError" && issue.NodeId == dependent.Handle.NodeId);
        }
    }
}
