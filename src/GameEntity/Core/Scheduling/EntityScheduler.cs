using System;
using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class EntityScheduler
    {
        private readonly EntityHierarchy _hierarchy;
        private readonly Dictionary<long, SceneScheduleBucket> _sceneBuckets = new Dictionary<long, SceneScheduleBucket>();
        private IUpdateStrategy _updateStrategy;

        public EntityScheduler(EntityHierarchy hierarchy)
        {
            _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        }

        public void SetUpdateStrategy(IUpdateStrategy strategy)
        {
            _updateStrategy = strategy;
        }

        public void Register(Entity entity)
        {
            if (entity == null || entity.IsDestroyed || !_hierarchy.TryGetNode(entity, out var record) || record.SceneNodeId == 0)
            {
                return;
            }

            var bucket = GetOrCreateSceneBucket(record.SceneNodeId);
            if (entity is IUpdate)
            {
                bucket.Update.Register(entity.Handle);
            }
        }

        public void Unregister(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            Unregister(entity.Handle);
        }

        public void Unregister(EntityHandle handle)
        {
            if (!handle.IsValid)
            {
                return;
            }

            foreach (var bucket in _sceneBuckets.Values)
            {
                bucket.Update.Unregister(handle);
            }
        }

        public void MoveIfRegistered(EntityHandle handle, long oldSceneNodeId, long newSceneNodeId)
        {
            if (!handle.IsValid || oldSceneNodeId == 0 || newSceneNodeId == 0 || oldSceneNodeId == newSceneNodeId)
            {
                return;
            }

            if (!_sceneBuckets.TryGetValue(oldSceneNodeId, out var oldBucket))
            {
                return;
            }

            var newBucket = GetOrCreateSceneBucket(newSceneNodeId);
            MoveIfRegistered(oldBucket, newBucket, handle);
        }

        public void RemoveScene(long sceneNodeId)
        {
            if (_sceneBuckets.TryGetValue(sceneNodeId, out var bucket))
            {
                bucket.Clear();
                _sceneBuckets.Remove(sceneNodeId);
            }
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var sceneNodeId in GetSceneNodeIdsSnapshot())
            {
                if (!_sceneBuckets.TryGetValue(sceneNodeId, out var sceneBucket))
                {
                    continue;
                }

                UpdateSceneBucket(sceneBucket, deltaTime, unscaledDeltaTime);
            }
        }

        public void Clear()
        {
            foreach (var bucket in _sceneBuckets.Values)
            {
                bucket.Clear();
            }

            _sceneBuckets.Clear();
            _updateStrategy = null;
        }

        private void UpdateSceneBucket(SceneScheduleBucket sceneBucket, float deltaTime, float unscaledDeltaTime)
        {
            foreach (var handle in sceneBucket.Update.Snapshot())
            {
                if (!sceneBucket.Update.IsRegistered(handle))
                {
                    continue;
                }

                if (!_hierarchy.TryResolve(handle, out Entity entity) || entity is not IUpdate updateableEntity)
                {
                    sceneBucket.Update.Unregister(handle);
                    continue;
                }

                if (!EnsureHandleInCurrentScene(sceneBucket, entity))
                {
                    continue;
                }

                if (!CanRun(entity) || !AreDependenciesReady(entity))
                {
                    continue;
                }

                IUpdateStrategy strategy = ResolveUpdateStrategy(entity);
                if (strategy == null)
                {
                    RunUpdate(updateableEntity, unscaledDeltaTime);
                    continue;
                }

                int updateCount = strategy.GetUpdateCount(entity, deltaTime, unscaledDeltaTime, out float singleDeltaTime);
                for (int i = 0; i < updateCount; i++)
                {
                    RunUpdate(updateableEntity, singleDeltaTime);
                }
            }

            sceneBucket.Update.Compact();
        }

        private bool EnsureHandleInCurrentScene(SceneScheduleBucket currentBucket, Entity entity)
        {
            long currentSceneNodeId = _hierarchy.GetSceneNodeId(entity);
            if (currentSceneNodeId == currentBucket.SceneNodeId)
            {
                return true;
            }

            currentBucket.Update.Unregister(entity.Handle);
            if (currentSceneNodeId != 0)
            {
                GetOrCreateSceneBucket(currentSceneNodeId).Update.Register(entity.Handle);
            }

            return false;
        }

        private IUpdateStrategy ResolveUpdateStrategy(Entity entity)
        {
            if (entity is IHasUpdateStrategy hasStrategy)
            {
                return hasStrategy.GetUpdateStrategy();
            }

            return _updateStrategy;
        }

        private SceneScheduleBucket GetOrCreateSceneBucket(long sceneNodeId)
        {
            if (!_sceneBuckets.TryGetValue(sceneNodeId, out var bucket))
            {
                bucket = new SceneScheduleBucket(sceneNodeId);
                _sceneBuckets.Add(sceneNodeId, bucket);
            }

            return bucket;
        }

        private IReadOnlyList<long> GetSceneNodeIdsSnapshot()
        {
            return new List<long>(_sceneBuckets.Keys);
        }

        private static void MoveIfRegistered(SceneScheduleBucket oldBucket, SceneScheduleBucket newBucket, EntityHandle handle)
        {
            if (!oldBucket.Update.Unregister(handle))
            {
                return;
            }

            newBucket.Update.Register(handle);
        }

        private static void RunUpdate(IUpdate updateableEntity, float deltaTime)
        {
            try
            {
                updateableEntity.Update(deltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"Update error: {e}");
            }
        }

        private static bool CanRun(Entity entity)
        {
            return entity is not IEntityLifecycleGate gate || gate.CanRun;
        }

        private bool AreDependenciesReady(Entity entity)
        {
            if (entity is not IDependentComponent)
            {
                return true;
            }

            return _hierarchy.World.Dependencies.RefreshDependencyStatus(entity);
        }
    }
}
