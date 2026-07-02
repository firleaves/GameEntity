namespace GameEntity
{
    internal sealed class SceneScheduleBucket
    {
        public SceneScheduleBucket(long sceneNodeId)
        {
            SceneNodeId = sceneNodeId;
            Update = new EntityUpdateBucket();
        }

        public long SceneNodeId { get; }

        public EntityUpdateBucket Update { get; }

        public bool IsEmpty => Update.Snapshot().Count == 0;

        public void Clear()
        {
            Update.Clear();
        }
    }
}
