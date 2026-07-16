using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityValidatorMatrixTests : GameEntityTestBase
    {
        [Fact]
        public void Validator_ShouldDetectIdentityAndAliveStateCorruption()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-identity-matrix");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityNode originalNode = world.Hierarchy.Nodes.GetNode(entity.Handle.NodeId);
            EntityHandle originalHandle = entity.Handle;
            long originalInstanceId = entity.InstanceId;
            EntityValidationResult validation;
            try
            {
                EntityNode corrupted = originalNode;
                corrupted.EntityId++;
                corrupted.Flags = EntityNodeFlags.None;
                world.Hierarchy.Nodes.SetNode(corrupted);
                entity.AssignHierarchyHandle(world.Hierarchy, new EntityHandle(long.MaxValue - 1));
                SetEntityInstanceId(entity, 0);

                validation = world.ValidateEntities();
            }
            finally
            {
                entity.AssignHierarchyHandle(world.Hierarchy, originalHandle);
                SetEntityInstanceId(entity, originalInstanceId);
                world.Hierarchy.Nodes.SetNode(originalNode);
            }

            AssertCodes(
                validation,
                "EntityHandleMismatch",
                "EntityIdMismatch",
                "EntityInstanceIdMismatch",
                "DestroyedObjectStillIndexed",
                "ObjectStoreHandleMismatch",
                "NodeNotAlive");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectComponentStateAndTypeIndexCorruption()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-component-matrix");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            ProbeComponent component = owner.AddComponent<ProbeComponent>();
            EntityNode originalNode = world.Hierarchy.Nodes.GetNode(component.Handle.NodeId);
            EntityValidationResult validation;
            try
            {
                EntityNode corrupted = originalNode;
                corrupted.ComponentTypeId++;
                world.Hierarchy.Nodes.SetNode(corrupted);
                component.IsComponent = false;

                validation = world.ValidateEntities();
            }
            finally
            {
                component.IsComponent = true;
                world.Hierarchy.Nodes.SetNode(originalNode);
            }

            AssertCodes(validation, "ComponentStateMismatch", "ComponentTypeIdMismatch", "OwnerIndexMissing");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectOwnedNodePartitionAndOwnerCorruption()
        {
            World world = World.Instance;
            TestScene sceneA = CreateScene("validator-owned-a");
            TestScene sceneB = CreateScene("validator-owned-b");
            ProbeEntity missingOwner = sceneA.AddChild<ProbeEntity>();
            ProbeEntity crossScene = sceneA.AddChild<ProbeEntity>();
            ProbeEntity ownerCycle = sceneA.AddChild<ProbeEntity>();
            EntityNode missingOwnerOriginal = world.Hierarchy.Nodes.GetNode(missingOwner.Handle.NodeId);
            EntityNode crossSceneOriginal = world.Hierarchy.Nodes.GetNode(crossScene.Handle.NodeId);
            EntityNode ownerCycleOriginal = world.Hierarchy.Nodes.GetNode(ownerCycle.Handle.NodeId);
            EntityValidationResult validation;
            try
            {
                EntityNode missingOwnerNode = missingOwnerOriginal;
                missingOwnerNode.OwnerNodeId = 0;
                missingOwnerNode.SceneNodeId = long.MaxValue;
                missingOwnerNode.ComponentTypeId = 1;
                world.Hierarchy.Nodes.SetNode(missingOwnerNode);

                EntityNode crossSceneNode = crossSceneOriginal;
                crossSceneNode.SceneNodeId = sceneB.Handle.NodeId;
                world.Hierarchy.Nodes.SetNode(crossSceneNode);

                EntityNode ownerCycleNode = ownerCycleOriginal;
                ownerCycleNode.OwnerNodeId = ownerCycleNode.NodeId;
                world.Hierarchy.Nodes.SetNode(ownerCycleNode);

                validation = world.ValidateEntities();
            }
            finally
            {
                world.Hierarchy.Nodes.SetNode(missingOwnerOriginal);
                world.Hierarchy.Nodes.SetNode(crossSceneOriginal);
                world.Hierarchy.Nodes.SetNode(ownerCycleOriginal);
            }

            AssertCodes(
                validation,
                "OwnerMissing",
                "ScenePartitionMissing",
                "ChildComponentTypeId",
                "CrossSceneOwning",
                "SceneRootReferenceMismatch",
                "OwnerCycle",
                "OwnerIndexMissing");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectSceneRootStructuralCorruption()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-scene-root-matrix");
            ProbeEntity child = scene.AddChild<ProbeEntity>();
            EntityNode originalNode = world.Hierarchy.Nodes.GetNode(scene.Handle.NodeId);
            EntityValidationResult validation;
            try
            {
                EntityNode corrupted = originalNode;
                corrupted.OwnerNodeId = child.Handle.NodeId;
                corrupted.SceneNodeId = child.Handle.NodeId;
                corrupted.ComponentTypeId = 1;
                world.Hierarchy.Nodes.SetNode(corrupted);

                validation = world.ValidateEntities();
            }
            finally
            {
                world.Hierarchy.Nodes.SetNode(originalNode);
            }

            AssertCodes(
                validation,
                "SceneRootHasOwner",
                "InvalidSceneRootPartition",
                "SceneRootComponentType",
                "OwnerIndexMissing");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectObjectStoreCorruptionCategories()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-object-store-matrix");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            var nullHandle = new EntityHandle(long.MaxValue - 10);
            var duplicateHandle = new EntityHandle(long.MaxValue - 11);
            EntityValidationResult validation;
            try
            {
                world.Hierarchy.Objects.Add(nullHandle, null);
                world.Hierarchy.Objects.Add(duplicateHandle, entity);
                validation = world.ValidateEntities();
            }
            finally
            {
                world.Hierarchy.Objects.Remove(nullHandle.NodeId);
                world.Hierarchy.Objects.Remove(duplicateHandle.NodeId);
            }

            AssertCodes(
                validation,
                "ObjectWithoutNode",
                "NullObjectEntry",
                "ObjectStoredMultipleTimes",
                "ObjectStoreHandleMismatch");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectSceneRegistryCorruptionCategories()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-scene-registry-matrix");
            Dictionary<string, long> names = GetPrivateField<Dictionary<string, long>>(
                world.Hierarchy.Scenes,
                "_sceneNameToNodeId");
            Dictionary<long, string> nodes = GetPrivateField<Dictionary<long, string>>(
                world.Hierarchy.Scenes,
                "_sceneNameByNodeId");
            EntityValidationResult validation;
            try
            {
                nodes[scene.Handle.NodeId] = "wrong-reverse-name";
                names["missing-target"] = long.MaxValue - 20;
                names["wrong-object-name"] = scene.Handle.NodeId;
                validation = world.ValidateEntities();
            }
            finally
            {
                names.Clear();
                nodes.Clear();
                world.Hierarchy.Scenes.Register(scene.Name, scene.Handle.NodeId);
            }

            AssertCodes(
                validation,
                "SceneRegistryReverseMismatch",
                "SceneRegistryForwardMismatch",
                "SceneRegistryTargetMissing",
                "SceneRegistryObjectMismatch",
                "SceneRegistryNodeMissing");
            Assert.True(world.ValidateEntities().IsValid);
        }

        [Fact]
        public void Validator_ShouldDetectSchedulerCorruptionCategories()
        {
            World world = World.Instance;
            TestScene sceneA = CreateScene("validator-scheduler-matrix-a");
            TestScene sceneB = CreateScene("validator-scheduler-matrix-b");
            UpdateProbeEntity valid = sceneA.AddChild<UpdateProbeEntity>();
            ProbeEntity wrongPhase = sceneA.AddChild<ProbeEntity>();
            UpdateProbeEntity wrongScene = sceneB.AddChild<UpdateProbeEntity>();
            EntityScheduler scheduler = world.Hierarchy.Scheduler;
            SceneScheduleBucket sceneBucket = FindSceneBucket(scheduler, sceneA.Handle.NodeId);
            EntityUpdateBucket updateBucket = sceneBucket.Update;
            List<EntityHandle> handles = GetPrivateField<List<EntityHandle>>(updateBucket, "_handles");
            HashSet<EntityHandle> membership = GetPrivateField<HashSet<EntityHandle>>(updateBucket, "_listedHandles");
            Dictionary<long, SceneScheduleBucket> sceneBuckets =
                GetPrivateField<Dictionary<long, SceneScheduleBucket>>(scheduler, "_sceneBuckets");
            EntityValidationResult validation;
            try
            {
                handles.Remove(valid.Handle);
                membership.Remove(valid.Handle);
                handles.Add(EntityHandle.None);
                membership.Add(new EntityHandle(long.MaxValue - 30));
                updateBucket.Register(new EntityHandle(long.MaxValue - 31));
                updateBucket.Register(wrongPhase.Handle);
                updateBucket.Register(wrongScene.Handle);
                sceneBuckets.Add(long.MaxValue - 32, new SceneScheduleBucket(long.MaxValue - 32));

                validation = world.ValidateEntities();
            }
            finally
            {
                scheduler.Clear();
            }

            AssertCodes(
                validation,
                "SchedulerSceneMissing",
                "SchedulerInvalidHandle",
                "SchedulerMembershipMissing",
                "SchedulerHandleListMissing",
                "SchedulerRegistrationUnlisted",
                "SchedulerDuplicateRegistration",
                "SchedulerEntityMissing",
                "SchedulerSceneMismatch",
                "SchedulerPhaseMismatch");
            Assert.True(world.ValidateEntities().IsValid);
        }

        private static SceneScheduleBucket FindSceneBucket(EntityScheduler scheduler, long sceneNodeId)
        {
            foreach (SceneScheduleBucket bucket in scheduler.GetSceneBucketsSnapshot())
            {
                if (bucket.SceneNodeId == sceneNodeId)
                {
                    return bucket;
                }
            }

            throw new InvalidOperationException($"Missing scheduler bucket for scene {sceneNodeId}.");
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void SetEntityInstanceId(Entity entity, long instanceId)
        {
            typeof(Entity)
                .GetProperty(nameof(Entity.InstanceId), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(entity, instanceId);
        }

        private static void AssertCodes(EntityValidationResult validation, params string[] expectedCodes)
        {
            Assert.False(validation.IsValid);
            foreach (string code in expectedCodes)
            {
                Assert.Contains(validation.Issues, issue => issue.Code == code);
            }
        }
    }
}
