using System;
using System.Linq;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityHierarchyV2Tests : GameEntityTestBase
    {
        [Fact]
        public void SceneConstructor_ShouldNotRegisterHierarchyRootUntilWorldAddScene()
        {
            var scene = new TestScene("unregistered-scene");

            Assert.Equal(0, scene.Id);
            Assert.Equal(0, scene.InstanceId);
            Assert.False(scene.Handle.IsValid);
            Assert.Null(scene.GetSceneRoot());
            Assert.Null(World.Instance.GetScene("unregistered-scene"));
            Assert.Empty(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.Throws<Exception>(() => scene.AddChild<ProbeEntity>());

            World.Instance.AddScene("unregistered-scene", scene);

            Assert.True(scene.Handle.IsValid);
            Assert.NotEqual(0, scene.Id);
            Assert.NotEqual(0, scene.InstanceId);
            Assert.Same(scene, scene.GetSceneRoot());
            Assert.Same(scene, World.Instance.GetScene("unregistered-scene"));
            Assert.Single(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void WorldAddScene_ShouldRejectMismatchedSceneName()
        {
            var scene = new TestScene("actual-scene");

            Exception exception = Assert.Throws<Exception>(() => World.Instance.AddScene("wrong-scene", scene));

            Assert.Contains("scene name mismatch", exception.Message);
            Assert.False(scene.Handle.IsValid);
            Assert.Null(World.Instance.GetScene("wrong-scene"));
            Assert.Null(World.Instance.GetScene("actual-scene"));
            Assert.Empty(World.Instance.CaptureEntitySnapshot().Nodes);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddChildAndComponent_ShouldUseEntityHierarchyOwnershipAndProjectionSnapshots()
        {
            TestScene scene = CreateScene("ownership");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            ProbeComponent component = entity.AddComponent<ProbeComponent>();

            Assert.Same(scene, entity.Parent);
            Assert.Same(scene, entity.Owner);
            Assert.Same(entity, component.Parent);
            Assert.Same(component, entity.GetComponent<ProbeComponent>());
            Assert.True(entity.TryGetComponent<ProbeComponent>(out var queriedComponent));
            Assert.Same(component, queriedComponent);
            Assert.True(entity.TryGetSceneRoot(out var sceneRoot));
            Assert.Same(scene, sceneRoot);

            var childrenSnapshot = scene.Children;
            Assert.Contains(entity, childrenSnapshot);
            Assert.True(scene.ContainsChild(entity.Id));

            var componentsSnapshot = entity.Components;
            Assert.Contains(component, componentsSnapshot);
            Assert.True(entity.ContainsComponent(component.GetType()));

            EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();
            Assert.Equal(3, snapshot.Nodes.Count);
            Assert.Contains(snapshot.Nodes, node => node.Kind == EntityNodeKind.SceneRoot && node.NodeId == node.SceneNodeId);
            Assert.Contains(snapshot.Nodes, node => node.Kind == EntityNodeKind.ChildEntity && node.EntityId == entity.Id);
            Assert.Contains(snapshot.Nodes, node => node.Kind == EntityNodeKind.ComponentEntity && node.EntityId == component.Id);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void DestroyOwner_ShouldCascadeAndInvalidateReferences()
        {
            TestScene scene = CreateScene("destroy");
            ProbeEntity parent = scene.AddChild<ProbeEntity>();
            ProbeEntity child = parent.AddChild<ProbeEntity>();
            ProbeComponent component = child.AddComponent<ProbeComponent>();
            EntityRef<ProbeEntity> childRef = child;
            EntityRef<ProbeComponent> componentRef = component;
            EntityHandle childHandle = child.Handle;

            parent.Destroy();

            Assert.True(parent.IsDestroyed);
            Assert.True(child.IsDestroyed);
            Assert.True(component.IsDestroyed);
            Assert.Equal(1, parent.DestroyCount);
            Assert.Equal(1, child.DestroyCount);
            Assert.Equal(1, component.DestroyCount);
            Assert.False(childRef.IsAlive);
            Assert.False(componentRef.TryGet(out _));
            Assert.False(World.Instance.TryResolve(childHandle, out ProbeEntity _));
            Assert.False(scene.ContainsChild(parent.Id));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void RemoveComponent_ShouldDestroyComponentSubtreeOnly()
        {
            TestScene scene = CreateScene("remove-component");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            ProbeComponent component = entity.AddComponent<ProbeComponent>();
            ProbeEntity componentChild = component.AddChild<ProbeEntity>();

            entity.RemoveComponent<ProbeComponent>();

            Assert.False(entity.IsDestroyed);
            Assert.True(component.IsDestroyed);
            Assert.True(componentChild.IsDestroyed);
            Assert.Equal(0, entity.ComponentsCount());
            Assert.True(scene.ContainsChild(entity.Id));
            Assert.Null(entity.GetComponent<ProbeComponent>());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void SemanticQueries_ShouldFindOwnersComponentsAndSceneRoot()
        {
            TestScene scene = CreateScene("queries");
            ProbeEntity actor = scene.AddChild<ProbeEntity>();
            ProbeComponent component = actor.AddComponent<ProbeComponent>();
            ProbeEntity task = component.AddChild<ProbeEntity>();

            Assert.True(task.TryGetOwner<ProbeComponent>(out var directOwner));
            Assert.Same(component, directOwner);
            Assert.True(task.TryFindOwner<ProbeEntity>(out var actorOwner));
            Assert.Same(actor, actorOwner);
            Assert.True(task.TryGetComponentInAncestors<ProbeComponent>(out var ancestorComponent));
            Assert.Same(component, ancestorComponent);
            Assert.True(component.TryGetSiblingComponent<ProbeComponent>(out var siblingComponent));
            Assert.Same(component, siblingComponent);
            Assert.Same(scene, task.GetSceneRoot());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void NewNode_ShouldUseNewNodeIdAndRejectDestroyedHandle()
        {
            TestScene scene = CreateScene("node-id");
            ProbeEntity first = scene.AddChild<ProbeEntity>();
            EntityHandle oldHandle = first.Handle;

            first.Destroy();
            ProbeEntity second = scene.AddChild<ProbeEntity>();
            EntityHandle newHandle = second.Handle;

            Assert.NotEqual(oldHandle.NodeId, newHandle.NodeId);
            Assert.False(World.Instance.TryResolve(oldHandle, out ProbeEntity _));
            Assert.True(World.Instance.TryResolve(newHandle, out ProbeEntity resolved));
            Assert.Same(second, resolved);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AttachAncestorUnderDescendant_ShouldThrowAndKeepHierarchyValid()
        {
            TestScene scene = CreateScene("cycle");
            ProbeEntity parent = scene.AddChild<ProbeEntity>();
            ProbeEntity child = parent.AddChild<ProbeEntity>();

            Exception exception = Assert.Throws<Exception>(() => parent.ReparentTo(child));

            Assert.Contains("cant attach owner descendant", exception.Message);
            Assert.Same(scene, parent.Parent);
            Assert.Same(parent, child.Parent);
            Assert.True(scene.ContainsChild(parent.Id));
            Assert.True(parent.ContainsChild(child.Id));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void ReparentAcrossScenes_ShouldMoveWholeSubtreeToNewScenePartition()
        {
            TestScene sceneA = CreateScene("scene-a");
            TestScene sceneB = CreateScene("scene-b");
            ProbeEntity entity = sceneA.AddChild<ProbeEntity>();
            ProbeEntity child = entity.AddChild<ProbeEntity>();
            ProbeComponent component = child.AddComponent<ProbeComponent>();

            entity.ReparentTo(sceneB);

            Assert.False(sceneA.ContainsChild(entity.Id));
            Assert.True(sceneB.ContainsChild(entity.Id));
            Assert.Same(sceneB, entity.GetSceneRoot());
            Assert.Same(sceneB, child.GetSceneRoot());
            Assert.Same(sceneB, component.GetSceneRoot());

            EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();
            EntityNodeInfo entityNode = snapshot.Nodes.Single(node => node.EntityId == entity.Id);
            EntityNodeInfo childNode = snapshot.Nodes.Single(node => node.Kind == EntityNodeKind.ChildEntity && node.EntityId == child.Id);
            EntityNodeInfo componentNode = snapshot.Nodes.Single(node => node.NodeId == component.Handle.NodeId);

            Assert.Equal(entityNode.SceneNodeId, childNode.SceneNodeId);
            Assert.Equal(entityNode.SceneNodeId, componentNode.SceneNodeId);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }
}
