namespace GameEntity
{
    internal struct EntityNode
    {
        public int NodeId;
        public int Generation;
        public long EntityId;
        public long InstanceId;
        public int SceneNodeId;
        public int OwnerNodeId;
        public long ComponentTypeId;
        public EntityNodeKind Kind;
        public EntityNodeFlags Flags;

        public EntityHandle Handle => new EntityHandle(NodeId, Generation);

        public bool IsAlive => (Flags & EntityNodeFlags.Alive) == EntityNodeFlags.Alive;

        public bool IsDisposing => (Flags & EntityNodeFlags.Disposing) == EntityNodeFlags.Disposing;
    }
}
