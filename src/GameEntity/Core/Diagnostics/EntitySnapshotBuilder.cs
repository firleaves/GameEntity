using System.Linq;

namespace GameEntity
{
    internal sealed class EntitySnapshotBuilder
    {
        private readonly EntityHierarchy _hierarchy;

        public EntitySnapshotBuilder(EntityHierarchy hierarchy)
        {
            _hierarchy = hierarchy;
        }

        public EntitySnapshot Capture()
        {
            var nodes = _hierarchy.Nodes.GetAllRecords()
                .OrderBy(record => record.NodeId)
                .Select(CreateNodeInfo)
                .ToList();

            return new EntitySnapshot(nodes);
        }

        private EntityNodeInfo CreateNodeInfo(NodeRecord record)
        {
            _hierarchy.Objects.TryGet(record.NodeId, out var entity);
            return new EntityNodeInfo(
                record.NodeId,
                record.Generation,
                record.BusinessId,
                record.InstanceId,
                record.SceneNodeId,
                record.OwnerNodeId,
                record.TypeId,
                record.Kind,
                record.IsAlive,
                record.IsDisposing,
                entity?.GetType().FullName,
                entity?.GetViewName());
        }
    }
}
