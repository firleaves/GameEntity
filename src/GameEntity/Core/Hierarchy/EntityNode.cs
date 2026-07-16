namespace GameEntity
{
    internal struct EntityNode
    {
        public long NodeId;
        public long EntityId;
        public long InstanceId;
        public long SceneNodeId;
        public long OwnerNodeId;
        public long ComponentTypeId;
        public EntityNodeKind Kind;
        public EntityNodeFlags Flags;

        public EntityHandle Handle => new EntityHandle(NodeId);

        public bool IsAlive => (Flags & EntityNodeFlags.Alive) == EntityNodeFlags.Alive;

        public bool IsDestroying => (Flags & EntityNodeFlags.Destroying) == EntityNodeFlags.Destroying;

        public bool IsStarted => (Flags & EntityNodeFlags.Started) == EntityNodeFlags.Started;

        public bool IsStartFaulted => (Flags & EntityNodeFlags.StartFaulted) == EntityNodeFlags.StartFaulted;
    }
}
