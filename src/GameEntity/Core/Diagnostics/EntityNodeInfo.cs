namespace GameEntity
{
    /// <summary>
    /// Entity 层级节点的只读诊断信息，用于调试、测试和后续 Unity 映射。
    /// </summary>
    public sealed class EntityNodeInfo
    {
        public EntityNodeInfo(
            long nodeId,
            long entityId,
            long instanceId,
            long sceneNodeId,
            long ownerNodeId,
            long componentTypeId,
            EntityNodeKind kind,
            bool isAlive,
            bool isDestroying,
            string entityType,
            string viewName)
            : this(
                nodeId,
                entityId,
                instanceId,
                sceneNodeId,
                ownerNodeId,
                componentTypeId,
                kind,
                isAlive,
                isDestroying,
                false,
                false,
                entityType,
                viewName)
        {
        }

        public EntityNodeInfo(
            long nodeId,
            long entityId,
            long instanceId,
            long sceneNodeId,
            long ownerNodeId,
            long componentTypeId,
            EntityNodeKind kind,
            bool isAlive,
            bool isDestroying,
            bool isStarted,
            bool isStartFaulted,
            string entityType,
            string viewName)
        {
            NodeId = nodeId;
            EntityId = entityId;
            InstanceId = instanceId;
            SceneNodeId = sceneNodeId;
            OwnerNodeId = ownerNodeId;
            ComponentTypeId = componentTypeId;
            Kind = kind;
            IsAlive = isAlive;
            IsDestroying = isDestroying;
            IsStarted = isStarted;
            IsStartFaulted = isStartFaulted;
            EntityType = entityType;
            ViewName = viewName;
        }

        public long NodeId { get; }

        public long EntityId { get; }

        public long InstanceId { get; }

        public long SceneNodeId { get; }

        public long OwnerNodeId { get; }

        public long ComponentTypeId { get; }

        public EntityNodeKind Kind { get; }

        public bool IsAlive { get; }

        public bool IsDestroying { get; }

        public bool IsStarted { get; }

        public bool IsStartFaulted { get; }

        public string EntityType { get; }

        public string ViewName { get; }
    }
}
