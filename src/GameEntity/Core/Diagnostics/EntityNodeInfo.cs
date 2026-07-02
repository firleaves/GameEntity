namespace GameEntity
{
    /// <summary>
    /// Entity 层级节点的只读诊断信息，用于调试、测试和后续 Unity 映射。
    /// </summary>
    public sealed class EntityNodeInfo
    {
        public EntityNodeInfo(
            int nodeId,
            int generation,
            long entityId,
            long instanceId,
            int sceneNodeId,
            int ownerNodeId,
            long componentTypeId,
            EntityNodeKind kind,
            bool isAlive,
            bool isDisposing,
            string entityType,
            string viewName)
        {
            NodeId = nodeId;
            Generation = generation;
            EntityId = entityId;
            InstanceId = instanceId;
            SceneNodeId = sceneNodeId;
            OwnerNodeId = ownerNodeId;
            ComponentTypeId = componentTypeId;
            Kind = kind;
            IsAlive = isAlive;
            IsDisposing = isDisposing;
            EntityType = entityType;
            ViewName = viewName;
        }

        public int NodeId { get; }

        public int Generation { get; }

        public long EntityId { get; }

        public long InstanceId { get; }

        public int SceneNodeId { get; }

        public int OwnerNodeId { get; }

        public long ComponentTypeId { get; }

        public EntityNodeKind Kind { get; }

        public bool IsAlive { get; }

        public bool IsDisposing { get; }

        public string EntityType { get; }

        public string ViewName { get; }
    }
}
