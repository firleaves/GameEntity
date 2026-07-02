namespace GameEntity
{
    internal sealed class SceneScheduleBucket
    {
        public SceneScheduleBucket(int sceneNodeId)
        {
            SceneNodeId = sceneNodeId;
            Update = new EntityUpdateBucket();
        }

        public int SceneNodeId { get; }

        public EntityUpdateBucket Update { get; }

        public bool IsEmpty => Update.Snapshot().Count == 0;

        public void Clear()
        {
            Update.Clear();
        }
    }
}
