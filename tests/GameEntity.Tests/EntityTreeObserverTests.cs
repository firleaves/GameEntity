using System;
using System.Collections.Generic;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityTreeObserverTests : GameEntityTestBase
    {
        [Fact]
        public void ObserveEntities_ShouldPublishCommittedTreeParentFirst()
        {
            var observer = new RecordingEntityTreeObserver();
            using IDisposable registration = World.Instance.ObserveEntities(observer, replayExisting: false);

            var scene = (ObserverScene)World.Instance.AddScene("observer-commit", new ObserverScene("observer-commit"));

            Assert.Collection(
                observer.Registered,
                entity => Assert.Same(scene, entity),
                entity => Assert.Same(scene.Root, entity),
                entity => Assert.Same(scene.Root.Child, entity),
                entity => Assert.Same(scene.Root.Component, entity));
            Assert.All(observer.Registered, entity => Assert.True(entity.Handle.IsValid));
            Assert.All(observer.Registered, entity => Assert.False(entity.IsDestroyed));
            Assert.True(scene.AwakeCompleted);
            Assert.True(scene.Root.AwakeCompleted);
            Assert.True(scene.Root.Child.AwakeCompleted);
            Assert.True(scene.Root.Component.AwakeCompleted);
            Assert.Empty(observer.ParentChanges);
        }

        [Fact]
        public void ObserveEntities_ShouldReplayExistingTreeParentFirst()
        {
            var scene = (ObserverScene)World.Instance.AddScene("observer-replay", new ObserverScene("observer-replay"));
            var observer = new RecordingEntityTreeObserver();

            using IDisposable registration = World.Instance.ObserveEntities(observer);

            Assert.Collection(
                observer.Registered,
                entity => Assert.Same(scene, entity),
                entity => Assert.Same(scene.Root, entity),
                entity => Assert.Same(scene.Root.Child, entity),
                entity => Assert.Same(scene.Root.Component, entity));
            Assert.Empty(observer.ParentChanges);
        }

        [Fact]
        public void FailedSceneAwake_ShouldRollbackWithoutPublishingTransientEntities()
        {
            var observer = new RecordingEntityTreeObserver();
            using IDisposable registration = World.Instance.ObserveEntities(observer, replayExisting: false);
            var scene = new FaultingObserverScene("observer-failure");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => World.Instance.AddScene(scene.Name, scene));

            Assert.Equal("scene awake failed", exception.Message);
            Assert.Null(World.Instance.GetScene(scene.Name));
            Assert.False(scene.Handle.IsValid);
            Assert.True(scene.IsDestroyed);
            Assert.NotNull(scene.Child);
            Assert.True(scene.Child.IsDestroyed);
            Assert.Empty(observer.Registered);
            Assert.Empty(observer.Destroyed);
            Assert.Empty(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void Reparent_ShouldPublishOnlyParentChangedEvent()
        {
            TestScene scene = CreateScene("observer-reparent");
            ProbeEntity firstOwner = scene.AddChild<ProbeEntity>();
            ProbeEntity secondOwner = scene.AddChild<ProbeEntity>();
            ProbeEntity child = firstOwner.AddChild<ProbeEntity>();
            var observer = new RecordingEntityTreeObserver();
            using IDisposable registration = World.Instance.ObserveEntities(observer, replayExisting: false);

            child.ReparentTo(secondOwner);

            Assert.Empty(observer.Registered);
            var change = Assert.Single(observer.ParentChanges);
            Assert.Same(child, change.Entity);
            Assert.Same(firstOwner, change.OldParent);
            Assert.Same(secondOwner, change.NewParent);
        }
    }

    internal sealed class RecordingEntityTreeObserver : IEntityTreeObserver
    {
        public List<Entity> Registered { get; } = new List<Entity>();

        public List<ParentChange> ParentChanges { get; } = new List<ParentChange>();

        public List<Entity> Destroyed { get; } = new List<Entity>();

        public void OnEntityRegistered(Entity entity)
        {
            Registered.Add(entity);
        }

        public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
            ParentChanges.Add(new ParentChange(entity, oldParent, newParent));
        }

        public void OnEntityDestroyed(Entity entity)
        {
            Destroyed.Add(entity);
        }
    }

    internal readonly struct ParentChange
    {
        public ParentChange(Entity entity, Entity oldParent, Entity newParent)
        {
            Entity = entity;
            OldParent = oldParent;
            NewParent = newParent;
        }

        public Entity Entity { get; }

        public Entity OldParent { get; }

        public Entity NewParent { get; }
    }

    internal sealed class ObserverScene : Scene
    {
        public ObserverScene(string name) : base(name)
        {
        }

        public bool AwakeCompleted { get; private set; }

        public ObserverRootEntity Root { get; private set; }

        public override void Awake()
        {
            Root = AddChild<ObserverRootEntity>();
            AwakeCompleted = true;
        }
    }

    internal sealed class ObserverRootEntity : Entity, IAwake
    {
        public bool AwakeCompleted { get; private set; }

        public ObserverChildEntity Child { get; private set; }

        public ObserverComponent Component { get; private set; }

        public void Awake()
        {
            Child = AddChild<ObserverChildEntity>();
            Component = AddComponent<ObserverComponent>();
            AwakeCompleted = true;
        }
    }

    internal sealed class ObserverChildEntity : Entity, IAwake
    {
        public bool AwakeCompleted { get; private set; }

        public void Awake()
        {
            AwakeCompleted = true;
        }
    }

    internal sealed class ObserverComponent : Entity, IAwake
    {
        public bool AwakeCompleted { get; private set; }

        public void Awake()
        {
            AwakeCompleted = true;
        }
    }

    internal sealed class FaultingObserverScene : Scene
    {
        public FaultingObserverScene(string name) : base(name)
        {
        }

        public ProbeEntity Child { get; private set; }

        public override void Awake()
        {
            Child = AddChild<ProbeEntity>();
            throw new InvalidOperationException("scene awake failed");
        }
    }
}
