using System;
using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class EntityScheduler
    {
        private enum UpdatePhase
        {
            Update,
            FixedUpdate,
        }

        private readonly struct UpdateWorkItem
        {
            public UpdateWorkItem(long sceneNodeId, EntityHandle handle)
            {
                SceneNodeId = sceneNodeId;
                Handle = handle;
            }

            public long SceneNodeId { get; }

            public EntityHandle Handle { get; }
        }

        private readonly EntityHierarchy _hierarchy;
        private readonly Dictionary<long, SceneScheduleBucket> _sceneBuckets = new Dictionary<long, SceneScheduleBucket>();

        public EntityScheduler(EntityHierarchy hierarchy)
        {
            _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        }

        public void Register(Entity entity)
        {
            if (entity == null || entity.IsDestroyed || !_hierarchy.TryGetNode(entity, out var record) || record.SceneNodeId == 0)
            {
                return;
            }

            bool participatesInUpdate = entity is IUpdate || (entity is IStart && entity is not IFixedUpdate);
            bool participatesInFixedUpdate = entity is IFixedUpdate;
            bool participatesInUpdateLifecycle = participatesInUpdate || participatesInFixedUpdate;

            if (entity is IEntityUpdateInterval && entity is not IUpdate)
            {
                throw new InvalidOperationException(
                    $"{entity.GetType().FullName} implements IEntityUpdateInterval but does not implement IUpdate.");
            }

            Type[] requirementTypes = UpdateRequirementMetadata.GetRequirementTypes(entity.GetType());
            if (requirementTypes.Length > 0)
            {
                UpdateRequirementMetadata.ValidateNoCycles(entity.GetType());

                if (record.Kind != EntityNodeKind.ComponentEntity)
                {
                    throw new InvalidOperationException(
                        $"{entity.GetType().FullName} uses RequireForUpdate but is not attached as a Component.");
                }

                if (!participatesInUpdateLifecycle)
                {
                    throw new InvalidOperationException(
                        $"{entity.GetType().FullName} uses RequireForUpdate but implements none of IStart, IFixedUpdate, or IUpdate.");
                }
            }

            if (!participatesInUpdateLifecycle || record.IsStartFaulted)
            {
                return;
            }

            var bucket = GetOrCreateSceneBucket(record.SceneNodeId);
            if (participatesInFixedUpdate)
            {
                bucket.FixedUpdate.Register(entity.Handle);
            }

            if (participatesInUpdate)
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
                bucket.FixedUpdate.Unregister(handle);
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
            MoveIfRegistered(oldBucket.FixedUpdate, newBucket.FixedUpdate, handle);
            MoveIfRegistered(oldBucket.Update, newBucket.Update, handle);
        }

        public void RemoveScene(long sceneNodeId)
        {
            if (_sceneBuckets.TryGetValue(sceneNodeId, out var bucket))
            {
                bucket.Clear();
                _sceneBuckets.Remove(sceneNodeId);
            }
        }

        public void Update(float deltaTime)
        {
            UpdatePhaseBuckets(UpdatePhase.Update, deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            UpdatePhaseBuckets(UpdatePhase.FixedUpdate, fixedDeltaTime);
        }

        public void Clear()
        {
            foreach (var bucket in _sceneBuckets.Values)
            {
                bucket.Clear();
            }

            _sceneBuckets.Clear();
        }

        public IReadOnlyList<SceneScheduleBucket> GetSceneBucketsSnapshot()
        {
            return new List<SceneScheduleBucket>(_sceneBuckets.Values);
        }

        private void UpdatePhaseBuckets(UpdatePhase phase, float deltaTime)
        {
            IReadOnlyList<UpdateWorkItem> workItems = CreatePassSnapshot(phase);
            var processedHandles = new HashSet<EntityHandle>();
            foreach (UpdateWorkItem workItem in workItems)
            {
                if (!_sceneBuckets.TryGetValue(workItem.SceneNodeId, out var sceneBucket))
                {
                    continue;
                }

                UpdateEntity(sceneBucket, phase, workItem.Handle, deltaTime, processedHandles);
            }

            foreach (SceneScheduleBucket sceneBucket in _sceneBuckets.Values)
            {
                GetUpdateBucket(sceneBucket, phase).Compact();
            }
        }

        private void UpdateEntity(
            SceneScheduleBucket sceneBucket,
            UpdatePhase phase,
            EntityHandle handle,
            float deltaTime,
            HashSet<EntityHandle> processedHandles)
        {
            EntityUpdateBucket updateBucket = GetUpdateBucket(sceneBucket, phase);
            if (!updateBucket.IsRegistered(handle))
            {
                return;
            }

            if (!_hierarchy.TryResolve(handle, out Entity entity) || !ParticipatesInPhase(entity, phase))
            {
                updateBucket.Unregister(handle);
                return;
            }

            if (!EnsureHandleInCurrentScene(sceneBucket, entity, phase))
            {
                return;
            }

            if (!processedHandles.Add(handle) || !CanUpdate(entity))
            {
                return;
            }

            if (!EnsureStarted(entity, handle))
            {
                return;
            }

            // Start can destroy, move, or disable the current lifetime.
            if (!updateBucket.IsRegistered(handle) ||
                !_hierarchy.TryResolve(handle, out entity) ||
                !EnsureHandleInCurrentScene(sceneBucket, entity, phase))
            {
                return;
            }

            if (!CanUpdate(entity))
            {
                return;
            }

            if (phase == UpdatePhase.FixedUpdate)
            {
                if (entity is not IFixedUpdate fixedUpdateEntity)
                {
                    updateBucket.Unregister(handle);
                    return;
                }

                RunFixedUpdate(fixedUpdateEntity, deltaTime);
                return;
            }

            if (entity is not IUpdate updateEntity)
            {
                updateBucket.Unregister(handle);
                return;
            }

            if (!TryGetUpdateDeltaTime(entity, updateBucket, handle, deltaTime, out float updateDeltaTime))
            {
                return;
            }

            RunUpdate(updateEntity, updateDeltaTime);
        }

        private bool EnsureHandleInCurrentScene(SceneScheduleBucket currentSceneBucket, Entity entity, UpdatePhase phase)
        {
            long currentSceneNodeId = _hierarchy.GetSceneNodeId(entity);
            if (currentSceneNodeId == currentSceneBucket.SceneNodeId)
            {
                return true;
            }

            EntityUpdateBucket currentUpdateBucket = GetUpdateBucket(currentSceneBucket, phase);
            if (!currentUpdateBucket.TryUnregister(entity.Handle, out float elapsedTime))
            {
                return false;
            }

            if (currentSceneNodeId != 0)
            {
                EntityUpdateBucket newUpdateBucket = GetUpdateBucket(GetOrCreateSceneBucket(currentSceneNodeId), phase);
                newUpdateBucket.Register(entity.Handle, elapsedTime);
            }

            return false;
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

        private IReadOnlyList<UpdateWorkItem> CreatePassSnapshot(UpdatePhase phase)
        {
            var workItems = new List<UpdateWorkItem>();
            foreach (long sceneNodeId in GetSceneNodeIdsSnapshot())
            {
                if (!_sceneBuckets.TryGetValue(sceneNodeId, out var sceneBucket))
                {
                    continue;
                }

                foreach (EntityHandle handle in GetUpdateBucket(sceneBucket, phase).Snapshot())
                {
                    workItems.Add(new UpdateWorkItem(sceneNodeId, handle));
                }
            }

            return workItems;
        }

        private static EntityUpdateBucket GetUpdateBucket(SceneScheduleBucket sceneBucket, UpdatePhase phase)
        {
            return phase == UpdatePhase.FixedUpdate ? sceneBucket.FixedUpdate : sceneBucket.Update;
        }

        private static bool ParticipatesInPhase(Entity entity, UpdatePhase phase)
        {
            if (phase == UpdatePhase.FixedUpdate)
            {
                return entity is IFixedUpdate;
            }

            return entity is IStart || entity is IUpdate;
        }

        private static void MoveIfRegistered(
            EntityUpdateBucket oldBucket,
            EntityUpdateBucket newBucket,
            EntityHandle handle)
        {
            if (!oldBucket.TryUnregister(handle, out float elapsedTime))
            {
                return;
            }

            newBucket.Register(handle, elapsedTime);
        }

        private static bool TryGetUpdateDeltaTime(
            Entity entity,
            EntityUpdateBucket bucket,
            EntityHandle handle,
            float deltaTime,
            out float updateDeltaTime)
        {
            updateDeltaTime = 0f;
            if (!bucket.TryAccumulate(handle, deltaTime, out float elapsedTime))
            {
                return false;
            }

            float updateInterval = 0f;
            if (entity is IEntityUpdateInterval updateIntervalState)
            {
                try
                {
                    updateInterval = updateIntervalState.UpdateInterval;
                }
                catch (Exception e)
                {
                    bucket.ResetElapsed(handle);
                    Log.Error($"Update interval error: {entity.GetType().FullName}: {e}");
                    return false;
                }

                if (float.IsNaN(updateInterval) || float.IsInfinity(updateInterval) || updateInterval < 0f)
                {
                    bucket.ResetElapsed(handle);
                    Log.Error(
                        $"Invalid UpdateInterval on {entity.GetType().FullName}: {updateInterval}. " +
                        "The value must be finite and greater than or equal to zero.");
                    return false;
                }
            }

            if (updateInterval > 0f && elapsedTime < updateInterval)
            {
                return false;
            }

            bucket.ResetElapsed(handle);
            updateDeltaTime = elapsedTime;
            return true;
        }

        private static void RunUpdate(IUpdate updateEntity, float deltaTime)
        {
            try
            {
                updateEntity.Update(deltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"Update error: {e}");
            }
        }

        private static void RunFixedUpdate(IFixedUpdate fixedUpdateEntity, float fixedDeltaTime)
        {
            try
            {
                fixedUpdateEntity.FixedUpdate(fixedDeltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"FixedUpdate error: {e}");
            }
        }

        private static bool CanUpdate(Entity entity)
        {
            if (entity is IEntityUpdateState state)
            {
                try
                {
                    if (!state.IsUpdateEnabled)
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Update state error: {entity.GetType().FullName} ({entity.Handle}): {e}");
                    return false;
                }
            }

            try
            {
                UpdateRequirementResult requirementResult = UpdateRequirementResolver.Check(entity);
                if (requirementResult.BlockReason == UpdateRequirementBlockReason.ComponentStateError)
                {
                    string requirementName = requirementResult.RequirementType?.FullName ?? "-";
                    Log.Error(
                        $"Update requirement state error: {entity.GetType().FullName} ({entity.Handle}) " +
                        $"requires {requirementName}: {requirementResult.Exception}");
                }

                return requirementResult.CanUpdate;
            }
            catch (Exception e)
            {
                Log.Error($"Update requirement error: {entity.GetType().FullName} ({entity.Handle}): {e}");
                return false;
            }
        }

        private bool EnsureStarted(Entity entity, EntityHandle originalHandle)
        {
            if (entity is not IStart startEntity)
            {
                return true;
            }

            long originalInstanceId = entity.InstanceId;
            if (!TryGetSameLifetime(entity, originalHandle, originalInstanceId, out var record))
            {
                return false;
            }

            if (record.IsStartFaulted)
            {
                Unregister(originalHandle);
                return false;
            }

            if (record.IsStarted)
            {
                return true;
            }

            try
            {
                startEntity.Start();
            }
            catch (Exception e)
            {
                if (TryGetSameLifetime(entity, originalHandle, originalInstanceId, out record))
                {
                    _hierarchy.Nodes.SetStartFaulted(record.NodeId);
                    Unregister(originalHandle);
                }

                Log.Error($"Start error: {entity.GetType().FullName}: {e}");
                return false;
            }

            if (!TryGetSameLifetime(entity, originalHandle, originalInstanceId, out record))
            {
                return false;
            }

            _hierarchy.Nodes.SetStarted(record.NodeId);
            return true;
        }

        private bool TryGetSameLifetime(
            Entity entity,
            EntityHandle originalHandle,
            long originalInstanceId,
            out EntityNode record)
        {
            record = default;
            if (entity == null || entity.Handle != originalHandle || entity.InstanceId != originalInstanceId ||
                !_hierarchy.Nodes.TryGetNode(originalHandle, out record) || record.InstanceId != originalInstanceId ||
                !_hierarchy.TryResolve(originalHandle, out Entity resolved))
            {
                return false;
            }

            return ReferenceEquals(entity, resolved);
        }
    }
}
