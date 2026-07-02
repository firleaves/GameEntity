using System.Linq;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class CrossSceneGraphTests : GameEntityTestBase
    {
        [Fact]
        public void CrossSceneReparent_ShouldMigrateEntireSubtreePartition()
        {
            TestScene sceneA = CreateScene("cross-a");
            TestScene sceneB = CreateScene("cross-b");
            ProbeEntity root = sceneA.AddChild<ProbeEntity>();
            ProbeEntity child = root.AddChild<ProbeEntity>();
            ProbeComponent component = child.AddComponent<ProbeComponent>();

            root.ReparentTo(sceneB);

            EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();
            EntityNodeInfo rootNode = snapshot.Nodes.Single(node => node.NodeId == root.Handle.NodeId);
            EntityNodeInfo childNode = snapshot.Nodes.Single(node => node.NodeId == child.Handle.NodeId);
            EntityNodeInfo componentNode = snapshot.Nodes.Single(node => node.NodeId == component.Handle.NodeId);

            Assert.Equal(sceneB.Handle.NodeId, rootNode.SceneNodeId);
            Assert.Equal(sceneB.Handle.NodeId, childNode.SceneNodeId);
            Assert.Equal(sceneB.Handle.NodeId, componentNode.SceneNodeId);
            Assert.Same(sceneB, root.GetSceneRoot());
            Assert.Same(sceneB, child.GetSceneRoot());
            Assert.Same(sceneB, component.GetSceneRoot());
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }
    }
}
