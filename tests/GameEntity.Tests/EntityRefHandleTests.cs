using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityRefHandleTests : GameEntityTestBase
    {
        [Fact]
        public void EntityRef_ShouldResolveThroughHandleWhenCachedEntityIsStillAlive()
        {
            TestScene scene = CreateScene("ref-resolve");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityRef<ProbeEntity> entityRef = entity;

            Assert.True(entityRef.TryGet(out var resolved));
            Assert.Same(entity, resolved);
            Assert.Equal(entity.Handle, entityRef.Handle);
        }

        [Fact]
        public void EntityRef_ShouldNotResolveDestroyedHandleWhenNewNodeIsCreated()
        {
            TestScene scene = CreateScene("ref-destroyed-handle");
            ProbeEntity first = scene.AddChild<ProbeEntity>();
            EntityRef<ProbeEntity> oldRef = first;
            EntityHandle oldHandle = oldRef.Handle;

            first.Destroy();
            ProbeEntity second = scene.AddChild<ProbeEntity>();

            Assert.NotEqual(oldHandle.NodeId, second.Handle.NodeId);
            Assert.False(oldRef.IsAlive);
            Assert.False(oldRef.TryGet(out _));
            Assert.False(World.Instance.TryResolve(oldHandle, out ProbeEntity _));
            Assert.True(World.Instance.TryResolve(second.Handle, out ProbeEntity resolved));
            Assert.Same(second, resolved);
        }
    }
}
