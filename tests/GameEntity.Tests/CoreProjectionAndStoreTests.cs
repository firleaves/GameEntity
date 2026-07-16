using System;
using System.Collections.Generic;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class CoreProjectionAndStoreTests : GameEntityTestBase
    {
        [Fact]
        public void EntityNodeInfo_LegacyConstructorShouldExposeEveryDiagnosticField()
        {
            var info = new EntityNodeInfo(
                nodeId: 1,
                entityId: 2,
                instanceId: 3,
                sceneNodeId: 4,
                ownerNodeId: 5,
                componentTypeId: 6,
                kind: EntityNodeKind.ComponentEntity,
                isAlive: true,
                isDestroying: false,
                entityType: "ProbeType",
                viewName: "ProbeView");

            Assert.Equal(1, info.NodeId);
            Assert.Equal(2, info.EntityId);
            Assert.Equal(3, info.InstanceId);
            Assert.Equal(4, info.SceneNodeId);
            Assert.Equal(5, info.OwnerNodeId);
            Assert.Equal(6, info.ComponentTypeId);
            Assert.Equal(EntityNodeKind.ComponentEntity, info.Kind);
            Assert.True(info.IsAlive);
            Assert.False(info.IsDestroying);
            Assert.False(info.IsStarted);
            Assert.False(info.IsStartFaulted);
            Assert.Equal("ProbeType", info.EntityType);
            Assert.Equal("ProbeView", info.ViewName);
        }

        [Fact]
        public void DiagnosticContainers_ShouldNormalizeNullCollectionsAndExposeMessages()
        {
            var snapshot = new EntitySnapshot(null);
            var validation = new EntityValidationResult(null);
            EntityValidationIssue warning = EntityValidationIssue.Warning(7, "WarningCode", "warning message");
            EntityValidationIssue error = EntityValidationIssue.Error(8, "ErrorCode", "error message");

            Assert.Empty(snapshot.Nodes);
            Assert.Empty(validation.Issues);
            Assert.True(validation.IsValid);
            Assert.Equal("warning message", warning.Message);
            Assert.Equal(EntityValidationSeverity.Warning, warning.Severity);
            Assert.Equal("error message", error.Message);
            Assert.Equal(EntityValidationSeverity.Error, error.Severity);
            Assert.Empty(new RequireForUpdateAttribute((Type[])null).RequiredComponentTypes);
        }

        [Fact]
        public void HierarchyProjectionApis_ShouldReturnIndexedChildrenAndComponents()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("hierarchy-projection-apis");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            ProbeEntity child = owner.AddChild<ProbeEntity>();
            ProbeComponent component = owner.AddComponent<ProbeComponent>();

            SortedDictionary<long, Entity> children = world.Hierarchy.BuildChildrenSnapshot(owner);
            SortedDictionary<long, Entity> components = world.Hierarchy.BuildComponentsSnapshot(owner);

            Assert.Same(child, children[child.Id]);
            Assert.Same(component, components[owner.GetLongHashCode(typeof(ProbeComponent))]);
            Assert.Same(component, world.Hierarchy.GetComponent<ProbeComponent>(owner));

            world.Hierarchy.RemoveComponent(owner, component);
            Assert.True(component.IsDestroyed);
            Assert.Null(owner.GetComponent<ProbeComponent>());
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void StoreAndBucketUtilityApis_ShouldExposeCurrentInternalState()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("store-bucket-utility-apis");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityNode originalNode = world.Hierarchy.Nodes.GetNode(entity.Handle.NodeId);

            Assert.Same(entity, world.Hierarchy.Objects.Get(entity.Handle.NodeId));

            world.Hierarchy.Nodes.SetSceneNodeId(entity.Handle.NodeId, scene.Handle.NodeId);
            Assert.Equal(scene.Handle.NodeId, world.Hierarchy.Nodes.GetNode(entity.Handle.NodeId).SceneNodeId);
            world.Hierarchy.Nodes.SetNode(originalNode);

            var bucket = new SceneScheduleBucket(scene.Handle.NodeId);
            Assert.True(bucket.IsEmpty);
            Assert.Equal(0, bucket.Update.Count);
            bucket.Update.Register(entity.Handle);
            Assert.False(bucket.IsEmpty);
            Assert.Equal(1, bucket.Update.Count);
            bucket.Clear();
            Assert.True(bucket.IsEmpty);
        }
    }
}
