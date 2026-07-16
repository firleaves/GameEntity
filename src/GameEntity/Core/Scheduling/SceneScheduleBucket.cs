namespace GameEntity
{
    internal sealed class SceneScheduleBucket
    {
        public SceneScheduleBucket(long sceneNodeId)
        {
            SceneNodeId = sceneNodeId;
            FixedUpdate = new EntityUpdateBucket();
            Update = new EntityUpdateBucket();
        }

        public long SceneNodeId { get; }

        public EntityUpdateBucket FixedUpdate { get; }

        public EntityUpdateBucket Update { get; }

        public bool IsEmpty => FixedUpdate.Count == 0 && Update.Count == 0;

        public void Clear()
        {
            FixedUpdate.Clear();
            Update.Clear();
        }
    }
}
