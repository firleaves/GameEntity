using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityReferenceValueTests : GameEntityTestBase
    {
        [Fact]
        public void EmptyEntityRefs_ShouldRemainEmpty()
        {
            EntityRef<ProbeEntity> defaultRef = default;
            EntityRef<ProbeEntity> nullRef = (ProbeEntity)null;
            ProbeEntity implicitValue = defaultRef;

            Assert.False(defaultRef.IsAlive);
            Assert.False(defaultRef.Handle.IsValid);
            Assert.Null(defaultRef.ValueOrNull);
            Assert.Null(implicitValue);
            Assert.False(nullRef.TryGet(out ProbeEntity resolved));
            Assert.Null(resolved);
        }

        [Fact]
        public void EntityHandle_ShouldUseNodeIdValueSemantics()
        {
            var first = new EntityHandle(42);
            var same = new EntityHandle(42);
            var different = new EntityHandle(43);

            Assert.True(first.Equals((object)same));
            Assert.False(first.Equals((object)different));
            Assert.False(first.Equals("42"));
            Assert.True(first == same);
            Assert.True(first != different);
            Assert.Equal(first.GetHashCode(), same.GetHashCode());
            Assert.Equal("42", first.ToString());
            Assert.Equal("None", EntityHandle.None.ToString());
        }
    }
}
