using System;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class WorldAndObserverBoundaryTests : GameEntityTestBase
    {
        [Fact]
        public void ObserveEntities_NullObserver_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => World.Instance.ObserveEntities(null));
        }

        [Fact]
        public void Destroy_ShouldNotifyObserverExactlyOnce()
        {
            TestScene scene = CreateScene("observer-destroy-notification");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            var observer = new RecordingEntityTreeObserver();
            using IDisposable registration = World.Instance.ObserveEntities(observer, replayExisting: false);

            entity.Destroy();
            entity.Destroy();

            Assert.Empty(observer.Registered);
            Entity destroyed = Assert.Single(observer.Destroyed);
            Assert.Same(entity, destroyed);
            Assert.True(entity.IsDestroyed);
        }

        [Fact]
        public void DisposedObserverRegistration_ShouldReceiveNoMoreEvents()
        {
            TestScene scene = CreateScene("observer-disposed-registration");
            var observer = new RecordingEntityTreeObserver();
            IDisposable registration = World.Instance.ObserveEntities(observer, replayExisting: false);

            registration.Dispose();
            registration.Dispose();
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            entity.Destroy();

            Assert.Empty(observer.Registered);
            Assert.Empty(observer.ParentChanges);
            Assert.Empty(observer.Destroyed);
        }

        [Fact]
        public void ThrowingObserver_ShouldNotBlockOtherObservers()
        {
            TestScene scene = CreateScene("observer-exception-isolation");
            ProbeEntity firstOwner = scene.AddChild<ProbeEntity>();
            ProbeEntity secondOwner = scene.AddChild<ProbeEntity>();
            var faulting = new ThrowingEntityTreeObserver();
            var recording = new RecordingEntityTreeObserver();
            using IDisposable faultingRegistration = World.Instance.ObserveEntities(faulting, replayExisting: false);
            using IDisposable recordingRegistration = World.Instance.ObserveEntities(recording, replayExisting: false);

            ProbeEntity child = firstOwner.AddChild<ProbeEntity>();
            child.ReparentTo(secondOwner);
            child.Destroy();

            Assert.Same(child, Assert.Single(recording.Registered));
            ParentChange parentChange = Assert.Single(recording.ParentChanges);
            Assert.Same(child, parentChange.Entity);
            Assert.Same(firstOwner, parentChange.OldParent);
            Assert.Same(secondOwner, parentChange.NewParent);
            Assert.Same(child, Assert.Single(recording.Destroyed));
        }

        [Fact]
        public void Observer_ShouldBeAbleToUnregisterDuringCallback()
        {
            TestScene scene = CreateScene("observer-self-unregister");
            var observer = new SelfUnregisteringEntityTreeObserver();
            observer.Registration = World.Instance.ObserveEntities(observer, replayExisting: false);

            scene.AddChild<ProbeEntity>();
            scene.AddChild<ProbeEntity>();

            Assert.Equal(1, observer.RegisteredCount);
            observer.Registration.Dispose();
        }

        [Fact]
        public void AddScene_NullScene_ShouldThrowWithoutMutatingWorld()
        {
            Assert.Throws<ArgumentNullException>(() => World.Instance.AddScene("null-scene", null));

            Assert.Null(World.Instance.GetScene("null-scene"));
            Assert.Empty(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddScene_DuplicateName_ShouldKeepExistingSceneAndRejectCandidate()
        {
            World world = World.Instance;
            var existing = new TestScene("duplicate-scene");
            var candidate = new TestScene("duplicate-scene");
            world.AddScene(existing.Name, existing);

            Exception exception = Assert.Throws<Exception>(() => world.AddScene(candidate.Name, candidate));

            Assert.Contains("already exists", exception.Message);
            Assert.Same(existing, world.GetScene(existing.Name));
            Assert.True(existing.Handle.IsValid);
            Assert.False(candidate.Handle.IsValid);
            Assert.Single(world.CaptureEntitySnapshot().Nodes);
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddScene_AlreadyRegisteredScene_ShouldRejectDefensiveReentry()
        {
            World world = World.Instance;
            var scene = new TestScene("registered-scene");
            world.AddScene(scene.Name, scene);
            world.UnregisterScene(scene);

            Exception exception = Assert.Throws<Exception>(() => world.AddScene(scene.Name, scene));

            Assert.Contains("already registered", exception.Message);
            Assert.True(scene.Handle.IsValid);
            Assert.True(world.TryResolve(scene.Handle, out TestScene resolved));
            Assert.Same(scene, resolved);

            scene.Destroy();
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddScene_UpdateLifecycleScene_ShouldRejectBeforeRegistration()
        {
            var scene = new UpdatingBoundaryScene("updating-scene");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => World.Instance.AddScene(scene.Name, scene));

            Assert.Contains("Scene roots are not scheduled", exception.Message);
            Assert.False(scene.Handle.IsValid);
            Assert.Null(World.Instance.GetScene(scene.Name));
            Assert.Empty(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RootName_ShouldRoundTripWhileWorldIsActive()
        {
            World.Instance.RootName = "game-root";

            Assert.Equal("game-root", World.Instance.RootName);
        }
    }

    internal sealed class ThrowingEntityTreeObserver : IEntityTreeObserver
    {
        public void OnEntityRegistered(Entity entity)
        {
            throw new InvalidOperationException("observer register failed");
        }

        public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
            throw new InvalidOperationException("observer parent failed");
        }

        public void OnEntityDestroyed(Entity entity)
        {
            throw new InvalidOperationException("observer destroy failed");
        }
    }

    internal sealed class SelfUnregisteringEntityTreeObserver : IEntityTreeObserver
    {
        public IDisposable Registration { get; set; }

        public int RegisteredCount { get; private set; }

        public void OnEntityRegistered(Entity entity)
        {
            RegisteredCount++;
            Registration.Dispose();
        }

        public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
        }

        public void OnEntityDestroyed(Entity entity)
        {
        }
    }

    internal sealed class UpdatingBoundaryScene : Scene, IUpdate
    {
        public UpdatingBoundaryScene(string name) : base(name)
        {
        }

        public void Update(float deltaTime)
        {
        }
    }
}
