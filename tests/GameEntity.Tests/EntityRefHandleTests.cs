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
        public void EntityRef_ShouldNotResolveReusedNodeWithOldGeneration()
        {
            TestScene scene = CreateScene("ref-reuse");
            ProbeEntity first = scene.AddChild<ProbeEntity>();
            EntityRef<ProbeEntity> oldRef = first;
            EntityHandle oldHandle = oldRef.Handle;

            first.Dispose();
            ProbeEntity second = scene.AddChild<ProbeEntity>();

            Assert.Equal(oldHandle.NodeId, second.Handle.NodeId);
            Assert.NotEqual(oldHandle.Generation, second.Handle.Generation);
            Assert.False(oldRef.IsAlive);
            Assert.False(oldRef.TryGet(out _));
            Assert.True(World.Instance.TryResolve(second.Handle, out ProbeEntity resolved));
            Assert.Same(second, resolved);
        }
    }
}
