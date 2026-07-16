using System;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntitySchedulerExceptionTests : GameEntityTestBase
    {
        [Fact]
        public void ThrowingUpdate_ShouldNotBlockFollowingEntitiesOrFuturePasses()
        {
            TestScene scene = CreateScene("scheduler-update-callback-error");
            ThrowingUpdateCallbackEntity faulting = scene.AddChild<ThrowingUpdateCallbackEntity>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);
            World.Instance.Update(0.2f);

            Assert.Equal(2, faulting.UpdateCount);
            Assert.Equal(2, valid.UpdateCount);
            Assert.Equal(0.2f, valid.LastDeltaTime);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ThrowingFixedUpdate_ShouldNotBlockFollowingEntitiesOrFuturePasses()
        {
            TestScene scene = CreateScene("scheduler-fixed-callback-error");
            ThrowingFixedUpdateCallbackEntity faulting = scene.AddChild<ThrowingFixedUpdateCallbackEntity>();
            FixedUpdateProbeEntity valid = scene.AddChild<FixedUpdateProbeEntity>();

            World.Instance.FixedUpdate(0.02f);
            World.Instance.FixedUpdate(0.03f);

            Assert.Equal(2, faulting.FixedUpdateCount);
            Assert.Equal(2, valid.FixedUpdateCount);
            Assert.Equal(0.03f, valid.LastFixedDeltaTime);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void EntityDestroyingItselfDuringUpdate_ShouldLeaveSchedulerClean()
        {
            TestScene scene = CreateScene("scheduler-self-destroy-update");
            SelfDestroyingUpdateEntity selfDestroying = scene.AddChild<SelfDestroyingUpdateEntity>();
            UpdateProbeEntity valid = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);
            World.Instance.Update(0.1f);

            Assert.True(selfDestroying.IsDestroyed);
            Assert.Equal(1, selfDestroying.UpdateCount);
            Assert.Equal(2, valid.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void EntityDestroyedByEarlierCallback_ShouldNotRunFromPassSnapshot()
        {
            TestScene scene = CreateScene("scheduler-destroy-later-entity");
            DestroyTargetOnUpdateEntity destroyer = scene.AddChild<DestroyTargetOnUpdateEntity>();
            UpdateProbeEntity target = scene.AddChild<UpdateProbeEntity>();
            destroyer.Target = target;

            World.Instance.Update(0.1f);

            Assert.Equal(1, destroyer.UpdateCount);
            Assert.True(target.IsDestroyed);
            Assert.Equal(0, target.UpdateCount);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }

    public sealed class ThrowingUpdateCallbackEntity : Entity, IAwake, IUpdate
    {
        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            throw new InvalidOperationException("update callback failed");
        }
    }

    public sealed class ThrowingFixedUpdateCallbackEntity : Entity, IAwake, IFixedUpdate
    {
        public int FixedUpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
            throw new InvalidOperationException("fixed update callback failed");
        }
    }

    public sealed class SelfDestroyingUpdateEntity : Entity, IAwake, IUpdate
    {
        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            Destroy();
        }
    }

    public sealed class DestroyTargetOnUpdateEntity : Entity, IAwake, IUpdate
    {
        public int UpdateCount { get; private set; }

        public UpdateProbeEntity Target { private get; set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            Target.Destroy();
        }
    }
}
