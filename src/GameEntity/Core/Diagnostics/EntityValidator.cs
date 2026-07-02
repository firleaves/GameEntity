using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class EntityValidator
    {
        private readonly EntityHierarchy _hierarchy;

        public EntityValidator(EntityHierarchy hierarchy)
        {
            _hierarchy = hierarchy;
        }

        public EntityValidationResult Validate()
        {
            var issues = new List<EntityValidationIssue>();
            foreach (var record in _hierarchy.Nodes.GetAllNodes().OrderBy(r => r.NodeId))
            {
                ValidateNode(record, issues);
            }

            return new EntityValidationResult(issues);
        }

        private void ValidateNode(EntityNode record, List<EntityValidationIssue> issues)
        {
            if (!_hierarchy.Objects.TryGet(record.NodeId, out var entity))
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "ObjectMissing", "节点存在，但 ObjectStore 中没有对应 Entity。"));
            }

            if (!_hierarchy.Nodes.IsAttachedToOwnerIndex(record))
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerIndexMissing", "节点没有出现在 owner 对应的 child/component 索引中。"));
            }

            if (record.Kind == EntityNodeKind.SceneRoot)
            {
                if (record.OwnerNodeId != 0)
                {
                    issues.Add(EntityValidationIssue.Error(record.NodeId, "SceneRootHasOwner", "SceneRoot 不应该拥有 owner。"));
                }

                if (record.SceneNodeId != record.NodeId)
                {
                    issues.Add(EntityValidationIssue.Error(record.NodeId, "InvalidSceneRootPartition", "SceneRoot 的 SceneNodeId 必须指向自身。"));
                }

                return;
            }

            if (record.OwnerNodeId == 0)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerMissing", "非 SceneRoot 节点必须拥有 owner。"));
                return;
            }

            if (!_hierarchy.Nodes.TryGetNode(record.OwnerNodeId, out var ownerRecord))
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerNodeMissing", "节点指向的 owner 节点不存在。"));
                return;
            }

            if (ownerRecord.SceneNodeId != record.SceneNodeId)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "CrossSceneOwning", "节点与 owner 不在同一个 scene 分区。"));
            }

            if (OwnerChainContains(record.OwnerNodeId, record.NodeId))
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerCycle", "owner 链中存在循环关系。"));
            }

            if (entity != null && entity.IsDestroyed)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "DestroyedObjectStillIndexed", "已销毁 Entity 仍然保留在 EntityHierarchy 索引中。"));
            }
        }

        private bool OwnerChainContains(long startOwnerNodeId, long targetNodeId)
        {
            var visited = new HashSet<long>();
            long currentNodeId = startOwnerNodeId;
            while (currentNodeId != 0)
            {
                if (currentNodeId == targetNodeId)
                {
                    return true;
                }

                if (!visited.Add(currentNodeId) || !_hierarchy.Nodes.TryGetNode(currentNodeId, out var currentRecord))
                {
                    return false;
                }

                currentNodeId = currentRecord.OwnerNodeId;
            }

            return false;
        }
    }
}
