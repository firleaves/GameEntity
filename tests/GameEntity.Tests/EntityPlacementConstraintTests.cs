using System;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityPlacementConstraintTests : GameEntityTestBase
    {
        [Fact]
        public void ChildOf_ShouldAllowDeclaredOwnerType()
        {
            TestScene scene = CreateScene("placement-valid");
            PlacementOwner owner = scene.AddChild<PlacementOwner>();

            PlacementChild child = owner.AddChild<PlacementChild>();

            Assert.Same(owner, child.Owner);
            Assert.Contains(child, owner.Children);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ChildOf_ShouldRejectDifferentOwnerBeforeConstruction()
        {
            TestScene scene = CreateScene("placement-invalid-owner");
            ProbeEntity invalidOwner = scene.AddChild<ProbeEntity>();
            PlacementChild.ConstructionCount = 0;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => invalidOwner.AddPooledChild<PlacementChild>());

            Assert.Contains("declares ChildOf", exception.Message);
            Assert.Equal(0, PlacementChild.ConstructionCount);
            Assert.Empty(invalidOwner.Children);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ChildOf_ShouldRejectComponentAttachment()
        {
            TestScene scene = CreateScene("placement-invalid-component");
            PlacementOwner owner = scene.AddChild<PlacementOwner>();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => owner.AddComponent<PlacementChild>());

            Assert.Contains("cannot be attached as a Component", exception.Message);
            Assert.Empty(owner.Components);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ChildOf_ShouldRejectInvalidReparentAndKeepPreviousOwner()
        {
            TestScene scene = CreateScene("placement-reparent");
            PlacementOwner validOwner = scene.AddChild<PlacementOwner>();
            ProbeEntity invalidOwner = scene.AddChild<ProbeEntity>();
            PlacementChild child = validOwner.AddChild<PlacementChild>();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => child.ReparentTo(invalidOwner));

            Assert.Contains("cannot be attached", exception.Message);
            Assert.Same(validOwner, child.Owner);
            Assert.Contains(child, validOwner.Children);
            Assert.DoesNotContain(child, invalidOwner.Children);
        }

        [Fact]
        public void ChildOf_ShouldRejectNonEntityParentMetadata()
        {
            TestScene scene = CreateScene("placement-invalid-metadata");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => owner.AddChild<InvalidPlacementChild>());

            Assert.Contains("must derive from Entity", exception.Message);
            Assert.Empty(owner.Children);
        }
    }

    public sealed class PlacementOwner : Entity, IAwake
    {
        public void Awake()
        {
        }
    }

    [ChildOf(typeof(PlacementOwner))]
    public sealed class PlacementChild : Entity, IAwake
    {
        public static int ConstructionCount { get; set; }

        public PlacementChild()
        {
            ConstructionCount++;
        }

        public void Awake()
        {
        }
    }

    [ChildOf(typeof(string))]
    public sealed class InvalidPlacementChild : Entity, IAwake
    {
        public void Awake()
        {
        }
    }
}
