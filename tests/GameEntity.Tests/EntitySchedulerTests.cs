using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntitySchedulerTests : GameEntityTestBase
    {
        [Fact]
        public void Tick_ShouldRunThroughEntityScheduler()
        {
            TestScene scene = CreateScene("scheduler-basic");
            TickProbeEntity entity = scene.AddChild<TickProbeEntity>();

            World.Instance.Tick(0.25f, 0.5f);

            Assert.Equal(1, entity.UpdateCount);
            Assert.Equal(0.5f, entity.LastDeltaTime);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RepeatedRegister_ShouldNotRunEntityMoreThanOncePerFrame()
        {
            TestScene scene = CreateScene("scheduler-dedup");
            TickProbeEntity entity = scene.AddChild<TickProbeEntity>();

            World.Instance.Hierarchy.Scheduler.Register(entity);
            World.Instance.Hierarchy.Scheduler.Register(entity);
            World.Instance.Tick(0.1f, 0.1f);

            Assert.Equal(1, entity.UpdateCount);
        }

        [Fact]
        public void DestroyedEntity_ShouldNotRunFromSchedulerOldHandle()
        {
            TestScene scene = CreateScene("scheduler-destroy");
            TickProbeEntity entity = scene.AddChild<TickProbeEntity>();

            entity.Destroy();
            World.Instance.Tick(0.1f, 0.1f);

            Assert.Equal(0, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ReparentAcrossScenes_ShouldMoveScheduledEntityToNewSceneBucket()
        {
            TestScene sceneA = CreateScene("scheduler-a");
            TestScene sceneB = CreateScene("scheduler-b");
            TickProbeEntity entity = sceneA.AddChild<TickProbeEntity>();

            World.Instance.Tick(0.1f, 0.1f);
            entity.ReparentTo(sceneB);
            World.Instance.Tick(0.1f, 0.1f);

            Assert.Equal(2, entity.UpdateCount);
            Assert.Same(sceneB, entity.GetSceneRoot());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RemovedScene_ShouldStopRunningItsScheduledEntities()
        {
            TestScene scene = CreateScene("scheduler-remove-scene");
            TickProbeEntity entity = scene.AddChild<TickProbeEntity>();

            World.Instance.RemoveScene("scheduler-remove-scene");
            World.Instance.Tick(0.1f, 0.1f);

            Assert.True(entity.IsDestroyed);
            Assert.Equal(0, entity.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }
}
