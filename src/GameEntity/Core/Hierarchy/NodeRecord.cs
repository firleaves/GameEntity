namespace GameEntity
{
    internal struct NodeRecord
    {
        public int NodeId;
        public int Generation;
        public long BusinessId;
        public long InstanceId;
        public int SceneNodeId;
        public int OwnerNodeId;
        public long TypeId;
        public NodeKind Kind;
        public NodeFlags Flags;

        public EntityHandle Handle => new EntityHandle(NodeId, Generation);

        public bool IsAlive => (Flags & NodeFlags.Alive) == NodeFlags.Alive;

        public bool IsDisposing => (Flags & NodeFlags.Disposing) == NodeFlags.Disposing;
    }
}
