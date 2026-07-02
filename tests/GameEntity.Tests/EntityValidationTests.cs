using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityValidationTests : GameEntityTestBase
    {
        [Fact]
        public void ValidateEntities_ShouldStayValidAcrossCommonMutations()
        {
            TestScene scene = CreateScene("validation-common");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            ProbeComponent component = entity.AddComponent<ProbeComponent>();

            Assert.True(World.Instance.ValidateEntities().IsValid);

            component.Destroy();
            Assert.True(World.Instance.ValidateEntities().IsValid);

            entity.Destroy();
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ReadOnlyProjections_ShouldExposeEntityHierarchyStateWithoutMutableDictionaries()
        {
            TestScene scene = CreateScene("validation-snapshot");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            ProbeComponent component = entity.AddComponent<ProbeComponent>();

            var children = scene.Children;
            var components = entity.Components;

            Assert.Contains(entity, children);
            Assert.Contains(component, components);
            Assert.True(scene.ContainsChild(entity.Id));
            Assert.True(entity.ContainsComponent(component.GetType()));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }
}
