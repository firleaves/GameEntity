using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class EntityValidator
    {
        private readonly EntityHierarchy _hierarchy;

        public EntityValidator(EntityHierarchy hierarchy)
        {
            _hierarchy = hierarchy;
        }

        public EntityValidationResult Validate()
        {
            var issues = new List<EntityValidationIssue>();
            List<EntityNode> records = _hierarchy.Nodes.GetAllNodes()
                .OrderBy(record => record.NodeId)
                .ToList();
            var recordsByNodeId = records.ToDictionary(record => record.NodeId);

            foreach (EntityNode record in records)
            {
                ValidateNode(record, issues);
            }

            ValidateObjectStore(recordsByNodeId, issues);
            ValidateSceneRegistry(recordsByNodeId, issues);
            ValidateScheduler(issues);
            return new EntityValidationResult(issues);
        }

        private void ValidateNode(EntityNode record, List<EntityValidationIssue> issues)
        {
            _hierarchy.Objects.TryGet(record.NodeId, out Entity entity);
            if (entity == null)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "ObjectMissing",
                    "节点存在，但 ObjectStore 中没有对应 Entity。"));
            }
            else
            {
                ValidateEntityIdentity(record, entity, issues);
            }

            if (!record.IsAlive)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "NodeNotAlive", "NodeStore 中保留了非 Alive 节点。"));
            }

            if (!_hierarchy.Nodes.IsAttachedToOwnerIndex(record))
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "OwnerIndexMissing",
                    "节点没有出现在 owner 对应的 child/component 索引中。"));
            }

            if (record.Kind == EntityNodeKind.SceneRoot)
            {
                ValidateSceneRoot(record, entity, issues);
            }
            else
            {
                ValidateOwnedNode(record, entity, issues);
            }

            if (record.IsStartFaulted)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "StartFaulted",
                    "Entity 的 Start 执行失败，当前生命期不会再进入 Update。"));
            }

            if (entity != null)
            {
                ValidateUpdateRequirements(record, entity, issues);
            }
        }

        private static void ValidateEntityIdentity(
            EntityNode record,
            Entity entity,
            List<EntityValidationIssue> issues)
        {
            if (entity.Handle != record.Handle)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "EntityHandleMismatch",
                    $"Entity.Handle={entity.Handle}，节点 Handle={record.Handle}。"));
            }

            if (entity.Id != record.EntityId)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "EntityIdMismatch",
                    $"Entity.Id={entity.Id}，节点 EntityId={record.EntityId}。"));
            }

            if (entity.InstanceId != record.InstanceId)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "EntityInstanceIdMismatch",
                    $"Entity.InstanceId={entity.InstanceId}，节点 InstanceId={record.InstanceId}。"));
            }

            if (entity.IsDestroyed)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "DestroyedObjectStillIndexed",
                    "已销毁 Entity 仍然保留在 EntityHierarchy 索引中。"));
            }

            bool shouldBeComponent = record.Kind == EntityNodeKind.ComponentEntity;
            if (entity.IsComponent != shouldBeComponent)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "ComponentStateMismatch",
                    $"Entity.IsComponent={entity.IsComponent} 与节点 Kind={record.Kind} 不一致。"));
            }

            bool isSceneType = entity is Scene;
            if (record.Kind == EntityNodeKind.SceneRoot && !isSceneType)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRootTypeMismatch",
                    "SceneRoot 节点对应的对象必须派生自 Scene。"));
            }
            else if (record.Kind != EntityNodeKind.SceneRoot && isSceneType)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "NestedSceneType",
                    "Scene 派生对象不能作为 Child 或 Component 节点。"));
            }
        }

        private void ValidateSceneRoot(
            EntityNode record,
            Entity entity,
            List<EntityValidationIssue> issues)
        {
            if (record.OwnerNodeId != 0)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "SceneRootHasOwner", "SceneRoot 不应该拥有 owner。"));
            }

            if (record.SceneNodeId != record.NodeId)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "InvalidSceneRootPartition",
                    "SceneRoot 的 SceneNodeId 必须指向自身。"));
            }

            if (record.ComponentTypeId != 0)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRootComponentType",
                    "SceneRoot 的 ComponentTypeId 必须为 0。"));
            }

            if (entity is not Scene scene)
            {
                return;
            }

            try
            {
                EntityPlacementMetadata.ValidateSceneRoot(scene.GetType());
            }
            catch (Exception e)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "PlacementConstraintViolation",
                    e.Message));
            }

            if (!_hierarchy.Scenes.TryGetSceneNodeId(scene.Name, out long sceneNodeId) || sceneNodeId != record.NodeId)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRegistryNameMissing",
                    $"SceneRegistry 没有将名称 {scene.Name} 映射到当前 SceneRoot。"));
            }

            if (!_hierarchy.Scenes.TryGetSceneName(record.NodeId, out string sceneName) || sceneName != scene.Name)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRegistryNodeMissing",
                    "SceneRegistry 没有将当前 SceneRoot 反向映射到正确名称。"));
            }

            if (!ReferenceEquals(scene.SceneRoot, scene))
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRootReferenceMismatch",
                    "SceneRoot 对象的 SceneRoot 引用必须指向自身。"));
            }
        }

        private void ValidateOwnedNode(
            EntityNode record,
            Entity entity,
            List<EntityValidationIssue> issues)
        {
            if (record.Kind != EntityNodeKind.ChildEntity && record.Kind != EntityNodeKind.ComponentEntity)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "UnknownNodeKind", $"未知节点类型：{record.Kind}。"));
            }

            if (record.OwnerNodeId == 0)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerMissing", "非 SceneRoot 节点必须拥有 owner。"));
            }

            bool hasOwnerRecord = _hierarchy.Nodes.TryGetNode(record.OwnerNodeId, out EntityNode ownerRecord);
            if (!hasOwnerRecord)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerNodeMissing", "节点指向的 owner 节点不存在。"));
            }
            else if (ownerRecord.SceneNodeId != record.SceneNodeId)
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "CrossSceneOwning", "节点与 owner 不在同一个 Scene 分区。"));
            }

            if (!_hierarchy.Nodes.TryGetNode(record.SceneNodeId, out EntityNode sceneRecord) ||
                sceneRecord.Kind != EntityNodeKind.SceneRoot)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "ScenePartitionMissing",
                    "节点的 SceneNodeId 没有指向有效 SceneRoot。"));
            }

            _hierarchy.Objects.TryGet(record.OwnerNodeId, out Entity owner);
            if (entity != null && owner != null)
            {
                try
                {
                    if (record.Kind == EntityNodeKind.ChildEntity)
                    {
                        EntityPlacementMetadata.ValidateChild(owner, entity.GetType());
                    }
                    else if (record.Kind == EntityNodeKind.ComponentEntity)
                    {
                        EntityPlacementMetadata.ValidateComponent(entity.GetType());
                    }
                }
                catch (Exception e)
                {
                    issues.Add(EntityValidationIssue.Error(
                        record.NodeId,
                        "PlacementConstraintViolation",
                        e.Message));
                }

                if (record.Kind == EntityNodeKind.ComponentEntity)
                {
                    long expectedTypeId = owner.GetLongHashCode(entity.GetType());
                    if (record.ComponentTypeId != expectedTypeId)
                    {
                        issues.Add(EntityValidationIssue.Error(
                            record.NodeId,
                            "ComponentTypeIdMismatch",
                            $"ComponentTypeId={record.ComponentTypeId}，期望值={expectedTypeId}。"));
                    }
                }
            }

            if (record.Kind == EntityNodeKind.ChildEntity && record.ComponentTypeId != 0)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "ChildComponentTypeId",
                    "Child 节点的 ComponentTypeId 必须为 0。"));
            }

            if (entity != null && _hierarchy.Objects.TryGet(record.SceneNodeId, out Entity sceneEntity) &&
                !ReferenceEquals(entity.SceneRoot, sceneEntity))
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "SceneRootReferenceMismatch",
                    "Entity 保存的 SceneRoot 与节点 SceneNodeId 不一致。"));
            }

            if (OwnerChainContains(record.OwnerNodeId, record.NodeId))
            {
                issues.Add(EntityValidationIssue.Error(record.NodeId, "OwnerCycle", "owner 链中存在循环关系。"));
            }
        }

        private void ValidateObjectStore(
            IReadOnlyDictionary<long, EntityNode> recordsByNodeId,
            List<EntityValidationIssue> issues)
        {
            var seenEntities = new HashSet<Entity>();
            foreach (KeyValuePair<long, Entity> entry in _hierarchy.Objects.GetAllEntries())
            {
                long nodeId = entry.Key;
                Entity entity = entry.Value;
                if (!recordsByNodeId.ContainsKey(nodeId))
                {
                    issues.Add(EntityValidationIssue.Error(
                        nodeId,
                        "ObjectWithoutNode",
                        "ObjectStore 中存在没有对应 NodeStore 节点的 Entity。"));
                }

                if (entity == null)
                {
                    issues.Add(EntityValidationIssue.Error(nodeId, "NullObjectEntry", "ObjectStore 中存在 null Entity。"));
                    continue;
                }

                if (!seenEntities.Add(entity))
                {
                    issues.Add(EntityValidationIssue.Error(
                        nodeId,
                        "ObjectStoredMultipleTimes",
                        "同一个 Entity 对象被 ObjectStore 的多个 NodeId 引用。"));
                }

                if (entity.Handle.NodeId != nodeId)
                {
                    issues.Add(EntityValidationIssue.Error(
                        nodeId,
                        "ObjectStoreHandleMismatch",
                        $"ObjectStore key={nodeId}，Entity.Handle={entity.Handle}。"));
                }
            }
        }

        private void ValidateSceneRegistry(
            IReadOnlyDictionary<long, EntityNode> recordsByNodeId,
            List<EntityValidationIssue> issues)
        {
            foreach (KeyValuePair<string, long> entry in _hierarchy.Scenes.GetNameEntries())
            {
                if (!_hierarchy.Scenes.TryGetSceneName(entry.Value, out string reverseName) || reverseName != entry.Key)
                {
                    issues.Add(EntityValidationIssue.Error(
                        entry.Value,
                        "SceneRegistryReverseMismatch",
                        $"Scene 名称 {entry.Key} 的正向和反向映射不一致。"));
                }

                ValidateSceneRegistryTarget(entry.Key, entry.Value, recordsByNodeId, issues);
            }

            foreach (KeyValuePair<long, string> entry in _hierarchy.Scenes.GetNodeEntries())
            {
                if (!_hierarchy.Scenes.TryGetSceneNodeId(entry.Value, out long forwardNodeId) || forwardNodeId != entry.Key)
                {
                    issues.Add(EntityValidationIssue.Error(
                        entry.Key,
                        "SceneRegistryForwardMismatch",
                        $"SceneRoot {entry.Key} 的正向和反向映射不一致。"));
                }
            }
        }

        private void ValidateSceneRegistryTarget(
            string sceneName,
            long sceneNodeId,
            IReadOnlyDictionary<long, EntityNode> recordsByNodeId,
            List<EntityValidationIssue> issues)
        {
            if (!recordsByNodeId.TryGetValue(sceneNodeId, out EntityNode record) ||
                record.Kind != EntityNodeKind.SceneRoot)
            {
                issues.Add(EntityValidationIssue.Error(
                    sceneNodeId,
                    "SceneRegistryTargetMissing",
                    $"SceneRegistry 的 {sceneName} 没有指向有效 SceneRoot。"));
                return;
            }

            if (!_hierarchy.Objects.TryGet(sceneNodeId, out Entity entity) ||
                entity is not Scene scene || scene.Name != sceneName)
            {
                issues.Add(EntityValidationIssue.Error(
                    sceneNodeId,
                    "SceneRegistryObjectMismatch",
                    $"SceneRegistry 的 {sceneName} 与 ObjectStore 中的 Scene 不一致。"));
            }
        }

        private void ValidateScheduler(List<EntityValidationIssue> issues)
        {
            var updateHandles = new HashSet<EntityHandle>();
            var fixedUpdateHandles = new HashSet<EntityHandle>();
            foreach (SceneScheduleBucket sceneBucket in _hierarchy.Scheduler.GetSceneBucketsSnapshot())
            {
                if (!_hierarchy.Nodes.TryGetNode(sceneBucket.SceneNodeId, out EntityNode sceneRecord) ||
                    sceneRecord.Kind != EntityNodeKind.SceneRoot)
                {
                    issues.Add(EntityValidationIssue.Error(
                        sceneBucket.SceneNodeId,
                        "SchedulerSceneMissing",
                        "Scheduler bucket 没有对应的 SceneRoot。"));
                }

                ValidateSchedulerBucket(
                    sceneBucket.SceneNodeId,
                    "Update",
                    sceneBucket.Update,
                    updateHandles,
                    fixedPhase: false,
                    issues);
                ValidateSchedulerBucket(
                    sceneBucket.SceneNodeId,
                    "FixedUpdate",
                    sceneBucket.FixedUpdate,
                    fixedUpdateHandles,
                    fixedPhase: true,
                    issues);
            }
        }

        private void ValidateSchedulerBucket(
            long sceneNodeId,
            string phase,
            EntityUpdateBucket bucket,
            HashSet<EntityHandle> phaseHandles,
            bool fixedPhase,
            List<EntityValidationIssue> issues)
        {
            IReadOnlyList<EntityHandle> handleList = bucket.GetHandleListSnapshot();
            var listSet = new HashSet<EntityHandle>();
            foreach (IGrouping<EntityHandle, EntityHandle> duplicate in handleList
                         .GroupBy(handle => handle)
                         .Where(group => group.Count() > 1))
            {
                issues.Add(EntityValidationIssue.Error(
                    duplicate.Key.NodeId,
                    "SchedulerDuplicateHandle",
                    $"{phase} bucket 中 Handle {duplicate.Key} 出现了 {duplicate.Count()} 次。"));
            }

            foreach (EntityHandle handle in handleList)
            {
                if (!handle.IsValid)
                {
                    issues.Add(EntityValidationIssue.Error(0, "SchedulerInvalidHandle", $"{phase} bucket 包含无效 Handle。"));
                }

                listSet.Add(handle);
            }

            var membership = new HashSet<EntityHandle>(bucket.GetMembershipSnapshot());
            foreach (EntityHandle handle in listSet)
            {
                if (!membership.Contains(handle))
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerMembershipMissing",
                        $"{phase} Handle 存在于顺序列表，但不在 membership 集合中。"));
                }
            }

            foreach (EntityHandle handle in membership)
            {
                if (!listSet.Contains(handle))
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerHandleListMissing",
                        $"{phase} Handle 存在于 membership 集合，但不在顺序列表中。"));
                }
            }

            foreach (EntityHandle handle in bucket.GetRegisteredHandlesSnapshot())
            {
                if (!listSet.Contains(handle) || !membership.Contains(handle))
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerRegistrationUnlisted",
                        $"{phase} 的有效注册没有唯一的顺序列表项。"));
                }

                if (!phaseHandles.Add(handle))
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerDuplicateRegistration",
                        $"Handle {handle} 同时注册在多个 {phase} Scene bucket 中。"));
                }

                if (!_hierarchy.TryResolve(handle, out Entity entity))
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerEntityMissing",
                        $"{phase} 注册无法解析到存活 Entity。"));
                    continue;
                }

                if (_hierarchy.GetSceneNodeId(entity) != sceneNodeId)
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerSceneMismatch",
                        $"{phase} 注册所在 Scene bucket 与 Entity 当前 Scene 不一致。"));
                }

                bool participates = fixedPhase
                    ? entity is IFixedUpdate
                    : entity is IUpdate || (entity is IStart && entity is not IFixedUpdate);
                if (!participates)
                {
                    issues.Add(EntityValidationIssue.Error(
                        handle.NodeId,
                        "SchedulerPhaseMismatch",
                        $"Entity 不参与 {phase}，但仍保留有效调度注册。"));
                }
            }
        }

        private static void ValidateUpdateRequirements(
            EntityNode record,
            Entity entity,
            List<EntityValidationIssue> issues)
        {
            try
            {
                if (UpdateRequirementMetadata.TryGetCycle(entity.GetType(), out Type[] cycle))
                {
                    string cyclePath = string.Join(" -> ", cycle.Select(type => type.FullName));
                    issues.Add(EntityValidationIssue.Error(
                        record.NodeId,
                        "UpdateRequirementCycle",
                        $"更新要求存在循环：{cyclePath}。"));
                    return;
                }
            }
            catch (Exception e)
            {
                issues.Add(EntityValidationIssue.Error(
                    record.NodeId,
                    "UpdateRequirementMetadataError",
                    $"更新要求元数据无效：{e.Message}"));
                return;
            }

            UpdateRequirementResult result = UpdateRequirementResolver.Check(entity);
            if (!result.HasRequirements || result.CanUpdate)
            {
                return;
            }

            string requirementName = result.RequirementType?.FullName ?? "-";
            switch (result.BlockReason)
            {
                case UpdateRequirementBlockReason.ComponentMissing:
                    issues.Add(EntityValidationIssue.Warning(
                        record.NodeId,
                        "UpdateRequirementMissing",
                        $"更新要求的 Component 不存在：{requirementName}。"));
                    break;
                case UpdateRequirementBlockReason.ComponentNotReady:
                    issues.Add(EntityValidationIssue.Warning(
                        record.NodeId,
                        "UpdateRequirementNotReady",
                        $"更新要求的 Component 尚未 Ready：{requirementName}。"));
                    break;
                case UpdateRequirementBlockReason.ComponentStateError:
                    issues.Add(EntityValidationIssue.Error(
                        record.NodeId,
                        "UpdateRequirementStateError",
                        $"读取更新要求 Component 的 Ready 状态失败：{requirementName}；{result.Exception?.Message}"));
                    break;
                case UpdateRequirementBlockReason.NotComponent:
                    issues.Add(EntityValidationIssue.Error(
                        record.NodeId,
                        "UpdateRequirementTargetNotComponent",
                        "使用 RequireForUpdate 的 Entity 必须作为 Component 挂接。"));
                    break;
                case UpdateRequirementBlockReason.OwnerMissing:
                    issues.Add(EntityValidationIssue.Error(
                        record.NodeId,
                        "UpdateRequirementOwnerMissing",
                        "使用 RequireForUpdate 的 Component 没有有效 owner。"));
                    break;
            }
        }

        private bool OwnerChainContains(long startOwnerNodeId, long targetNodeId)
        {
            var visited = new HashSet<long>();
            long currentNodeId = startOwnerNodeId;
            while (currentNodeId != 0)
            {
                if (currentNodeId == targetNodeId)
                {
                    return true;
                }

                if (!visited.Add(currentNodeId) ||
                    !_hierarchy.Nodes.TryGetNode(currentNodeId, out EntityNode currentRecord))
                {
                    return false;
                }

                currentNodeId = currentRecord.OwnerNodeId;
            }

            return false;
        }
    }
}
