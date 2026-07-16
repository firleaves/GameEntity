using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class CoreInvariantRegressionTests : GameEntityTestBase
    {
        [Fact]
        [Trait("Priority", "P1")]
        public void DisposedWorld_ShouldRejectSceneCreationThroughOldReference()
        {
            World oldWorld = World.Instance;
            oldWorld.Dispose();
            var lateScene = new TestScene("disposed-world-late-scene");

            Exception error = Record.Exception(
                () => oldWorld.AddScene(lateScene.Name, lateScene));
            bool acceptedSceneAlive = error == null && !lateScene.IsDestroyed;
            bool acceptedHandleValid = error == null && lateScene.Handle.IsValid;

            // Clean the old instance as well when the current implementation accepted the scene.
            oldWorld.Dispose();

            Assert.True(
                error is ObjectDisposedException,
                $"Expected ObjectDisposedException, but got {ExceptionName(error)}; " +
                $"acceptedSceneAlive={acceptedSceneAlive}, " +
                $"acceptedHandleValid={acceptedHandleValid}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void DisposedWorld_ShouldRejectUpdateAndObserverEntrypointsThroughOldReference()
        {
            World oldWorld = World.Instance;
            oldWorld.Dispose();

            Exception updateError = Record.Exception(() => oldWorld.Update(0.1f));
            Exception fixedUpdateError = Record.Exception(() => oldWorld.FixedUpdate(0.02f));
            IDisposable registration = null;
            Exception observeError = Record.Exception(
                () => registration = oldWorld.ObserveEntities(new NoOpEntityTreeObserver()));
            registration?.Dispose();

            Assert.True(
                updateError is ObjectDisposedException &&
                fixedUpdateError is ObjectDisposedException &&
                observeError is ObjectDisposedException,
                $"Expected all old-World entrypoints to reject access; " +
                $"Update={ExceptionName(updateError)}, " +
                $"FixedUpdate={ExceptionName(fixedUpdateError)}, " +
                $"ObserveEntities={ExceptionName(observeError)}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void DisposedWorld_ShouldRejectQueryEntrypointsThroughOldReference()
        {
            World oldWorld = World.Instance;
            oldWorld.Dispose();
            string rootName = null;

            Exception getSceneError = Record.Exception(() => oldWorld.GetScene("missing"));
            Exception resolveError = Record.Exception(
                () => oldWorld.TryResolve(EntityHandle.None, out Entity _));
            Exception snapshotError = Record.Exception(oldWorld.CaptureEntitySnapshot);
            Exception validateError = Record.Exception(oldWorld.ValidateEntities);
            Exception getRootNameError = Record.Exception(() => rootName = oldWorld.RootName);
            Exception setRootNameError = Record.Exception(() => oldWorld.RootName = "disposed");
            Exception removeSceneError = Record.Exception(() => oldWorld.RemoveScene("missing"));

            Assert.True(
                getSceneError is ObjectDisposedException && resolveError is ObjectDisposedException &&
                snapshotError is ObjectDisposedException && validateError is ObjectDisposedException &&
                getRootNameError is ObjectDisposedException && setRootNameError is ObjectDisposedException &&
                removeSceneError is ObjectDisposedException,
                $"Expected all old-World queries to reject access; " +
                $"GetScene={ExceptionName(getSceneError)}, TryResolve={ExceptionName(resolveError)}, " +
                $"Snapshot={ExceptionName(snapshotError)}, Validate={ExceptionName(validateError)}, " +
                $"RootName.get={ExceptionName(getRootNameError)}, RootName.set={ExceptionName(setRootNameError)}, " +
                $"RemoveScene={ExceptionName(removeSceneError)}, observedRootName={rootName ?? "null"}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void WorldDispose_ShouldRejectSceneCreationFromDestroyCallback()
        {
            World world = World.Instance;
            var source = new CreateSceneOnDestroyScene("dispose-callback-source", world);
            world.AddScene(source.Name, source);

            world.Dispose();

            Scene orphan = source.CreatedScene;
            bool orphanAlive = orphan != null && !orphan.IsDestroyed;
            bool orphanHandleValid = orphan != null && orphan.Handle.IsValid;
            bool oldWorldResolvesOrphan = orphan != null && world.TryResolve(orphan.Handle, out Scene _);

            Assert.True(
                source.CreationError is ObjectDisposedException && orphan == null,
                $"Destroy callback creation should be rejected; " +
                $"error={ExceptionName(source.CreationError)}, created={orphan != null}, " +
                $"alive={orphanAlive}, handleValid={orphanHandleValid}, " +
                $"oldWorldResolves={oldWorldResolvesOrphan}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void EntityDestroy_ShouldBeNonVirtualTemplateEntrypoint()
        {
            MethodInfo destroyMethod = typeof(Entity).GetMethod(nameof(Entity.Destroy));

            Assert.True(
                destroyMethod != null && !destroyMethod.IsVirtual && destroyMethod.DeclaringType == typeof(Entity),
                $"Entity.Destroy must be the non-virtual template entrypoint; " +
                $"found={destroyMethod != null}, virtual={destroyMethod?.IsVirtual}, " +
                $"declaringType={destroyMethod?.DeclaringType?.FullName ?? "none"}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void Destroy_ShouldCompleteCoreCleanupWhenInternalHookThrows()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("throwing-destroy-hook");
            ThrowingInternalDestroyEntity entity = scene.AddChild<ThrowingInternalDestroyEntity>();
            EntityHandle handle = entity.Handle;

            Exception error = Record.Exception(entity.Destroy);
            bool nodeStillExists = world.CaptureEntitySnapshot().Nodes.Any(node => node.NodeId == handle.NodeId);
            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                error == null && entity.IsDestroyed && !entity.Handle.IsValid &&
                !nodeStillExists && validation.IsValid,
                $"Core cleanup was interrupted by OnDestroyInternal; " +
                $"error={ExceptionName(error)}, destroyed={entity.IsDestroyed}, " +
                $"handleValid={entity.Handle.IsValid}, nodeStillExists={nodeStillExists}, " +
                $"validatorValid={validation.IsValid}, issues={IssueCodes(validation)}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void ReparentTo_ShouldRejectEntityThatWasNeverCreatedByWorld()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("reparent-unattached");
            var entity = new UnattachedUpdateEntity();

            Exception error = Record.Exception(() => entity.ReparentTo(scene));
            world.Update(0.1f);
            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                error is InvalidOperationException && !entity.Handle.IsValid &&
                entity.Parent == null && entity.IsDestroyed && validation.IsValid,
                $"Unattached Entity entered the hierarchy; error={ExceptionName(error)}, " +
                $"destroyed={entity.IsDestroyed}, handleValid={entity.Handle.IsValid}, " +
                $"hasParent={entity.Parent != null}, awakeCount={entity.AwakeCount}, " +
                $"updateCount={entity.UpdateCount}, validatorValid={validation.IsValid}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void ReparentTo_ShouldRejectDestroyedEntityInsteadOfRevivingIt()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("reparent-destroyed");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();
            entity.Destroy();

            Exception error = Record.Exception(() => entity.ReparentTo(scene));
            world.Update(0.1f);
            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                error is InvalidOperationException && entity.IsDestroyed &&
                !entity.Handle.IsValid && entity.Parent == null && validation.IsValid,
                $"Destroyed Entity was revived by ReparentTo; error={ExceptionName(error)}, " +
                $"destroyed={entity.IsDestroyed}, handleValid={entity.Handle.IsValid}, " +
                $"hasParent={entity.Parent != null}, awakeCount={entity.AwakeCount}, " +
                $"updateCount={entity.UpdateCount}, validatorValid={validation.IsValid}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void EntityIdentitySetters_ShouldNotBeAccessibleToExternalDerivedTypes()
        {
            MethodInfo idSetter = typeof(Entity)
                .GetProperty(nameof(Entity.Id))
                ?.GetSetMethod(nonPublic: true);
            MethodInfo instanceIdSetter = typeof(Entity)
                .GetProperty(nameof(Entity.InstanceId))
                ?.GetSetMethod(nonPublic: true);

            bool idExposed = IsAccessibleToExternalDerivedType(idSetter);
            bool instanceIdExposed = IsAccessibleToExternalDerivedType(instanceIdSetter);

            Assert.True(
                !idExposed && !instanceIdExposed,
                $"Runtime identity is writable by business subclasses; " +
                $"IdSetter={MethodVisibility(idSetter)}, " +
                $"InstanceIdSetter={MethodVisibility(instanceIdSetter)}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void EntityIdentity_ShouldStayStableAcrossAwakeAndIndexedRemoval()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("identity-index");
            IdentityObservingEntity entity = scene.AddChild<IdentityObservingEntity>();

            bool foundByCurrentId = scene.ContainsChild(entity.Id);
            Entity resolvedByCurrentId = scene.GetChild<Entity>(entity.Id);
            EntityValidationResult validationBeforeRemove = world.ValidateEntities();
            long idBeforeRemove = entity.Id;
            long instanceIdBeforeRemove = entity.InstanceId;
            bool identityStableBeforeRemove =
                entity.IdDuringAwake == idBeforeRemove &&
                entity.InstanceIdDuringAwake == instanceIdBeforeRemove;

            scene.RemoveChild(entity.Id);

            Assert.True(
                identityStableBeforeRemove &&
                foundByCurrentId && ReferenceEquals(entity, resolvedByCurrentId) &&
                entity.IsDestroyed && validationBeforeRemove.IsValid,
                $"Entity identity or child index changed unexpectedly; awakeId={entity.IdDuringAwake}, " +
                $"preRemoveId={idBeforeRemove}, awakeInstanceId={entity.InstanceIdDuringAwake}, " +
                $"preRemoveInstanceId={instanceIdBeforeRemove}, foundByCurrent={foundByCurrentId}, " +
                $"currentResolvedSame={ReferenceEquals(entity, resolvedByCurrentId)}, " +
                $"destroyedAfterRemove={entity.IsDestroyed}, validatorValid={validationBeforeRemove.IsValid}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void RoundTripSceneMove_ShouldNotDuplicateUpdateScheduling()
        {
            TestScene sceneA = CreateScene("roundtrip-update-a");
            TestScene sceneB = CreateScene("roundtrip-update-b");
            UpdateProbeEntity entity = sceneA.AddChild<UpdateProbeEntity>();

            entity.ReparentTo(sceneB);
            entity.ReparentTo(sceneA);
            World.Instance.Update(0.1f);

            Assert.Equal(1, entity.UpdateCount);
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void RoundTripSceneMove_ShouldNotDuplicateFixedUpdateScheduling()
        {
            TestScene sceneA = CreateScene("roundtrip-fixed-a");
            TestScene sceneB = CreateScene("roundtrip-fixed-b");
            FixedUpdateProbeEntity entity = sceneA.AddChild<FixedUpdateProbeEntity>();

            entity.ReparentTo(sceneB);
            entity.ReparentTo(sceneA);
            World.Instance.FixedUpdate(0.02f);

            Assert.Equal(1, entity.FixedUpdateCount);
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void RoundTripSceneMove_ShouldNotAccumulateUpdateIntervalTwiceInOnePass()
        {
            TestScene sceneA = CreateScene("roundtrip-interval-a");
            TestScene sceneB = CreateScene("roundtrip-interval-b");
            RateLimitedProbeEntity entity = sceneA.AddChild<RateLimitedProbeEntity>();
            entity.UpdateInterval = 1f;

            entity.ReparentTo(sceneB);
            entity.ReparentTo(sceneA);
            World.Instance.Update(0.5f);

            Assert.True(
                entity.UpdateCount == 0,
                $"A single 0.5s pass reached a 1.0s interval after round-trip migration; " +
                $"updateCount={entity.UpdateCount}, deliveredDelta={entity.LastDeltaTime}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void CrossSceneMoveDuringUpdate_ShouldRunEntityAtMostOnceInCurrentPass()
        {
            TestScene sceneA = CreateScene("pass-move-a");
            TestScene sceneB = CreateScene("pass-move-b");
            MoveToSceneOnUpdateEntity mover = sceneA.AddChild<MoveToSceneOnUpdateEntity>();
            mover.Destination = sceneB;
            UpdateProbeEntity sceneBPeer = sceneB.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);

            Assert.True(
                mover.UpdateCount == 1 && sceneBPeer.UpdateCount == 1,
                $"Scene migration crossed the current pass boundary; " +
                $"moverUpdates={mover.UpdateCount}, sceneBPeerUpdates={sceneBPeer.UpdateCount}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void EntityCreatedInLaterSceneDuringUpdate_ShouldWaitForNextPass()
        {
            TestScene sceneA = CreateScene("pass-create-a");
            TestScene sceneB = CreateScene("pass-create-b");
            CreateInSceneOnUpdateEntity creator = sceneA.AddChild<CreateInSceneOnUpdateEntity>();
            creator.DestinationOwner = sceneB;
            UpdateProbeEntity sceneBPeer = sceneB.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);

            Assert.NotNull(creator.CreatedEntity);
            Assert.True(
                creator.CreatedEntity.UpdateCount == 0 && sceneBPeer.UpdateCount == 1,
                $"Entity created during the pass ran immediately in a later Scene; " +
                $"createdUpdates={creator.CreatedEntity.UpdateCount}, " +
                $"sceneBPeerUpdates={sceneBPeer.UpdateCount}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void WorldUpdate_ShouldRejectReentryFromEntityCallback()
        {
            TestScene scene = CreateScene("update-reentry");
            ReentrantUpdateEntity caller = scene.AddChild<ReentrantUpdateEntity>();
            UpdateProbeEntity peer = scene.AddChild<UpdateProbeEntity>();

            World.Instance.Update(0.1f);

            Assert.True(
                caller.UpdateCount == 1 && peer.UpdateCount == 1 &&
                caller.ReentryError is InvalidOperationException,
                $"World.Update reentered the same pass; callerUpdates={caller.UpdateCount}, " +
                $"peerUpdates={peer.UpdateCount}, reentryError={ExceptionName(caller.ReentryError)}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void WorldFixedUpdate_ShouldRejectReentryFromEntityCallback()
        {
            TestScene scene = CreateScene("fixed-update-reentry");
            ReentrantFixedUpdateEntity caller = scene.AddChild<ReentrantFixedUpdateEntity>();
            FixedUpdateProbeEntity peer = scene.AddChild<FixedUpdateProbeEntity>();

            World.Instance.FixedUpdate(0.02f);

            Assert.True(
                caller.FixedUpdateCount == 1 && peer.FixedUpdateCount == 1 &&
                caller.ReentryError is InvalidOperationException,
                $"World.FixedUpdate reentered the same pass; callerUpdates={caller.FixedUpdateCount}, " +
                $"peerUpdates={peer.FixedUpdateCount}, reentryError={ExceptionName(caller.ReentryError)}.");
        }

        [Fact]
        [Trait("Priority", "P1")]
        public void PooledReplacementCreatedDuringStart_ShouldRunItsOwnStart()
        {
            PooledStartReplacementEntity.ResetProbe();
            TestScene scene = CreateScene("pooled-start-replacement");
            PooledStartReplacementEntity first = scene.AddPooledChild<PooledStartReplacementEntity>();
            EntityHandle firstHandle = first.Handle;

            World.Instance.Update(0.1f);
            PooledStartReplacementEntity replacement = PooledStartReplacementEntity.Replacement;
            World.Instance.Update(0.1f);

            Assert.NotNull(replacement);
            Assert.True(
                ReferenceEquals(first, replacement) && firstHandle != replacement.Handle &&
                PooledStartReplacementEntity.TotalStartCount == 2 && replacement.UpdateCount == 1,
                $"The replacement lifetime skipped Start; sameObject={ReferenceEquals(first, replacement)}, " +
                $"oldHandle={firstHandle}, newHandle={replacement.Handle}, " +
                $"totalStarts={PooledStartReplacementEntity.TotalStartCount}, " +
                $"replacementUpdates={replacement.UpdateCount}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void AddPooledChild_ShouldNotReturnObjectDestroyedDuringAwake()
        {
            TestScene scene = CreateScene("awake-destroy-entity");
            AwakeSelfDestroyingEntity returned = null;

            Exception error = Record.Exception(
                () => returned = scene.AddPooledChild<AwakeSelfDestroyingEntity>());

            Assert.True(
                error is InvalidOperationException && returned == null && scene.ChildrenCount() == 0,
                $"Creation returned an object destroyed and recycled during Awake; " +
                $"error={ExceptionName(error)}, returned={returned != null}, " +
                $"destroyed={returned?.IsDestroyed}, handleValid={returned?.Handle.IsValid}, " +
                $"ownerChildren={scene.ChildrenCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void AddChild_ShouldNotReturnObjectDestroyedDuringAwake()
        {
            TestScene scene = CreateScene("awake-destroy-non-pooled-entity");
            AwakeSelfDestroyingEntity returned = null;

            Exception error = Record.Exception(
                () => returned = scene.AddChild<AwakeSelfDestroyingEntity>());

            Assert.True(
                error is InvalidOperationException && returned == null && scene.ChildrenCount() == 0 &&
                World.Instance.ValidateEntities().IsValid,
                $"Creation returned a non-pooled object destroyed during Awake; " +
                $"error={ExceptionName(error)}, returned={returned != null}, ownerChildren={scene.ChildrenCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void AddComponent_ShouldNotReturnObjectDestroyedDuringAwake()
        {
            TestScene scene = CreateScene("awake-destroy-component");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            AwakeSelfDestroyingEntity returned = null;

            Exception error = Record.Exception(
                () => returned = owner.AddComponent<AwakeSelfDestroyingEntity>());

            Assert.True(
                error is InvalidOperationException && returned == null && owner.ComponentsCount() == 0 &&
                World.Instance.ValidateEntities().IsValid,
                $"Creation returned a Component destroyed during Awake; " +
                $"error={ExceptionName(error)}, returned={returned != null}, components={owner.ComponentsCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void AddChild_ShouldNotReturnObjectDestroyedDuringRegisterSystem()
        {
            TestScene scene = CreateScene("register-destroy-entity");
            RegisterSelfDestroyingEntity returned = null;

            Exception error = Record.Exception(
                () => returned = scene.AddChild<RegisterSelfDestroyingEntity>());

            Assert.True(
                error is InvalidOperationException && returned == null && scene.ChildrenCount() == 0 &&
                World.Instance.ValidateEntities().IsValid,
                $"Creation returned an object destroyed during RegisterSystem; " +
                $"error={ExceptionName(error)}, returned={returned != null}, ownerChildren={scene.ChildrenCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void CreationRollback_ShouldNotDestroyPooledReplacementLifetime()
        {
            PooledAwakeReplacementEntity.ResetProbe();
            TestScene scene = CreateScene("awake-pooled-replacement");
            PooledAwakeReplacementEntity returned = null;

            Exception error = Record.Exception(
                () => returned = scene.AddPooledChild<PooledAwakeReplacementEntity>());
            PooledAwakeReplacementEntity replacement = PooledAwakeReplacementEntity.Replacement;

            Assert.True(
                error is InvalidOperationException && returned == null && replacement != null &&
                !replacement.IsDestroyed && replacement.Handle.IsValid &&
                PooledAwakeReplacementEntity.TotalAwakeCount == 2 && scene.ChildrenCount() == 1 &&
                World.Instance.ValidateEntities().IsValid,
                $"Rollback damaged the replacement lifetime; error={ExceptionName(error)}, " +
                $"returned={returned != null}, replacement={replacement != null}, " +
                $"replacementDestroyed={replacement?.IsDestroyed}, replacementHandle={replacement?.Handle}, " +
                $"awakeCount={PooledAwakeReplacementEntity.TotalAwakeCount}, children={scene.ChildrenCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void AddScene_ShouldNotReturnSceneDestroyedDuringAwake()
        {
            World world = World.Instance;
            const string sceneName = "awake-destroy-scene";
            var candidate = new AwakeSelfDestroyingScene(sceneName);
            Scene returned = null;

            Exception error = Record.Exception(
                () => returned = world.AddScene(sceneName, candidate));

            Assert.True(
                error is InvalidOperationException && returned == null && world.GetScene(sceneName) == null,
                $"AddScene returned a Scene destroyed during Awake; error={ExceptionName(error)}, " +
                $"returned={returned != null}, destroyed={candidate.IsDestroyed}, " +
                $"handleValid={candidate.Handle.IsValid}, registered={world.GetScene(sceneName) != null}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void ComponentApis_ShouldUseTheSameExactTypeSemantics()
        {
            TestScene scene = CreateScene("component-query-semantics");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            DerivedComponentA first = owner.AddComponent<DerivedComponentA>();
            DerivedComponentB second = owner.AddComponent<DerivedComponentB>();

            PolymorphicBaseComponent genericResult = owner.GetComponent<PolymorphicBaseComponent>();
            Entity runtimeTypeResult = owner.GetComponent(typeof(PolymorphicBaseComponent));
            bool contains = owner.ContainsComponent<PolymorphicBaseComponent>();
            owner.RemoveComponent<PolymorphicBaseComponent>();

            Assert.True(
                genericResult == null && runtimeTypeResult == null && !contains &&
                !first.IsDestroyed && !second.IsDestroyed && owner.ComponentsCount() == 2,
                $"Component APIs disagree about base-type lookup; " +
                $"genericResult={genericResult?.GetType().Name ?? "null"}, " +
                $"runtimeTypeResult={runtimeTypeResult?.GetType().Name ?? "null"}, " +
                $"contains={contains}, firstDestroyed={first.IsDestroyed}, " +
                $"secondDestroyed={second.IsDestroyed}, componentCount={owner.ComponentsCount()}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void SceneDerivedType_ShouldNotBeAttachableAsChildOrComponent()
        {
            TestScene scene = CreateScene("nested-scene-placement");
            Scene child = null;
            Scene component = null;

            Exception childError = Record.Exception(
                () => child = scene.AddChild<AttachableScene>());
            Exception componentError = Record.Exception(
                () => component = scene.AddComponent<AttachableScene>());

            Assert.True(
                childError is InvalidOperationException && componentError is InvalidOperationException &&
                child == null && component == null,
                $"Scene-derived instances were attached below another Scene; " +
                $"childError={ExceptionName(childError)}, componentError={ExceptionName(componentError)}, " +
                $"childAttached={child != null}, componentAttached={component != null}, " +
                $"validatorValid={World.Instance.ValidateEntities().IsValid}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void ChildOnlyScene_ShouldNotBeRegisteredAsSceneRoot()
        {
            World world = World.Instance;
            const string sceneName = "child-only-scene-root";
            var scene = new ChildOnlyScene(sceneName);

            Exception error = Record.Exception(() => world.AddScene(sceneName, scene));

            Assert.True(
                error is InvalidOperationException && !scene.Handle.IsValid && world.GetScene(sceneName) == null,
                $"A ChildOf Scene was registered as SceneRoot; error={ExceptionName(error)}, " +
                $"handleValid={scene.Handle.IsValid}, registered={world.GetScene(sceneName) != null}, " +
                $"validatorValid={world.ValidateEntities().IsValid}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void Validator_ShouldDetectNodeAndEntityInstanceIdMismatch()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-instance-id");
            ProbeEntity entity = scene.AddChild<ProbeEntity>();
            EntityNode record = world.Hierarchy.Nodes.GetNode(entity.Handle.NodeId);
            record.InstanceId++;
            world.Hierarchy.Nodes.SetNode(record);

            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                !validation.IsValid && validation.Issues.Any(issue => issue.Code == "EntityInstanceIdMismatch"),
                $"Validator accepted node/entity InstanceId mismatch; issues={IssueCodes(validation)}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void Validator_ShouldDetectSceneRegistryMismatch()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-scene-registry");
            world.Hierarchy.Scenes.Unregister(scene.Handle.NodeId);

            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                !validation.IsValid && validation.Issues.Any(issue => issue.Code == "SceneRegistryNameMissing"),
                $"Validator accepted a SceneRoot missing from SceneRegistry; issues={IssueCodes(validation)}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void Validator_ShouldDetectObjectStoreEntryWithoutNode()
        {
            World world = World.Instance;
            CreateScene("validator-extra-object");
            var orphan = new UnattachedUpdateEntity();
            var orphanHandle = new EntityHandle(long.MaxValue);
            world.Hierarchy.Objects.Add(orphanHandle, orphan);

            EntityValidationResult validation;
            try
            {
                validation = world.ValidateEntities();
            }
            finally
            {
                world.Hierarchy.Objects.Remove(orphanHandle.NodeId);
            }

            Assert.True(
                !validation.IsValid && validation.Issues.Any(issue => issue.Code == "ObjectWithoutNode"),
                $"Validator accepted an ObjectStore entry without a node; issues={IssueCodes(validation)}.");
        }

        [Fact]
        [Trait("Priority", "P2")]
        public void Validator_ShouldDetectDuplicateSchedulerHandle()
        {
            World world = World.Instance;
            TestScene scene = CreateScene("validator-scheduler-duplicate");
            UpdateProbeEntity entity = scene.AddChild<UpdateProbeEntity>();
            EntityUpdateBucket bucket = GetUpdateBucket(world.Hierarchy.Scheduler, scene.Handle.NodeId);
            List<EntityHandle> handles = GetMutableHandles(bucket);
            handles.Add(entity.Handle);

            EntityValidationResult validation = world.ValidateEntities();

            Assert.True(
                !validation.IsValid && validation.Issues.Any(issue => issue.Code == "SchedulerDuplicateHandle"),
                $"Validator accepted duplicate Scheduler handles; occurrences=" +
                $"{handles.Count(handle => handle == entity.Handle)}, issues={IssueCodes(validation)}.");
        }

        private static EntityUpdateBucket GetUpdateBucket(EntityScheduler scheduler, long sceneNodeId)
        {
            FieldInfo bucketsField = typeof(EntityScheduler)
                .GetField("_sceneBuckets", BindingFlags.Instance | BindingFlags.NonPublic);
            var buckets = (Dictionary<long, SceneScheduleBucket>)bucketsField.GetValue(scheduler);
            return buckets[sceneNodeId].Update;
        }

        private static List<EntityHandle> GetMutableHandles(EntityUpdateBucket bucket)
        {
            FieldInfo handlesField = typeof(EntityUpdateBucket)
                .GetField("_handles", BindingFlags.Instance | BindingFlags.NonPublic);
            return (List<EntityHandle>)handlesField.GetValue(bucket);
        }

        private static bool IsAccessibleToExternalDerivedType(MethodInfo setter)
        {
            return setter != null && (setter.IsPublic || setter.IsFamily || setter.IsFamilyOrAssembly);
        }

        private static string MethodVisibility(MethodInfo method)
        {
            if (method == null)
            {
                return "missing";
            }

            if (method.IsPublic)
            {
                return "public";
            }

            if (method.IsFamilyOrAssembly)
            {
                return "protected internal";
            }

            if (method.IsFamily)
            {
                return "protected";
            }

            if (method.IsAssembly)
            {
                return "internal";
            }

            if (method.IsFamilyAndAssembly)
            {
                return "private protected";
            }

            return "private";
        }

        private static string ExceptionName(Exception exception)
        {
            return exception?.GetType().Name ?? "none";
        }

        private static string IssueCodes(EntityValidationResult validation)
        {
            return validation.Issues.Count == 0
                ? "none"
                : string.Join(",", validation.Issues.Select(issue => issue.Code));
        }
    }

    internal sealed class NoOpEntityTreeObserver : IEntityTreeObserver
    {
        public void OnEntityRegistered(Entity entity)
        {
        }

        public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
        }

        public void OnEntityDestroyed(Entity entity)
        {
        }
    }

    internal sealed class CreateSceneOnDestroyScene : Scene
    {
        private readonly World _world;

        public CreateSceneOnDestroyScene(string name, World world) : base(name)
        {
            _world = world;
        }

        public Scene CreatedScene { get; private set; }

        public Exception CreationError { get; private set; }

        public override void OnDestroy()
        {
            try
            {
                string sceneName = $"{Name}-created-during-dispose";
                CreatedScene = _world.AddScene(sceneName, new TestScene(sceneName));
            }
            catch (Exception e)
            {
                CreationError = e;
            }
        }
    }

    internal sealed class ThrowingInternalDestroyEntity : Entity, IAwake, IUpdate
    {
        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
        }

        protected override void OnDestroyInternal()
        {
            throw new InvalidOperationException("internal destroy hook failed");
        }
    }

    internal sealed class UnattachedUpdateEntity : Entity, IAwake, IUpdate
    {
        public int AwakeCount { get; private set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    internal sealed class IdentityObservingEntity : Entity, IAwake
    {
        public long IdDuringAwake { get; private set; }

        public long InstanceIdDuringAwake { get; private set; }

        public void Awake()
        {
            IdDuringAwake = Id;
            InstanceIdDuringAwake = InstanceId;
        }
    }

    internal sealed class MoveToSceneOnUpdateEntity : Entity, IAwake, IUpdate
    {
        private bool _hasMoved;

        public Entity Destination { get; set; }

        public int UpdateCount { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            if (_hasMoved)
            {
                return;
            }

            _hasMoved = true;
            ReparentTo(Destination);
        }
    }

    internal sealed class CreateInSceneOnUpdateEntity : Entity, IAwake, IUpdate
    {
        public Entity DestinationOwner { get; set; }

        public UpdateProbeEntity CreatedEntity { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            if (CreatedEntity == null)
            {
                CreatedEntity = DestinationOwner.AddChild<UpdateProbeEntity>();
            }
        }
    }

    internal sealed class ReentrantUpdateEntity : Entity, IAwake, IUpdate
    {
        private bool _attemptedReentry;

        public int UpdateCount { get; private set; }

        public Exception ReentryError { get; private set; }

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            if (_attemptedReentry)
            {
                return;
            }

            _attemptedReentry = true;
            try
            {
                World.Instance.Update(deltaTime);
            }
            catch (Exception e)
            {
                ReentryError = e;
            }
        }
    }

    internal sealed class ReentrantFixedUpdateEntity : Entity, IAwake, IFixedUpdate
    {
        private bool _attemptedReentry;

        public int FixedUpdateCount { get; private set; }

        public Exception ReentryError { get; private set; }

        public void Awake()
        {
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
            if (_attemptedReentry)
            {
                return;
            }

            _attemptedReentry = true;
            try
            {
                World.Instance.FixedUpdate(fixedDeltaTime);
            }
            catch (Exception e)
            {
                ReentryError = e;
            }
        }
    }

    internal sealed class PooledStartReplacementEntity : Entity, IAwake, IStart, IUpdate
    {
        public static int TotalStartCount { get; private set; }

        public static PooledStartReplacementEntity Replacement { get; private set; }

        public int UpdateCount { get; private set; }

        public static void ResetProbe()
        {
            TotalStartCount = 0;
            Replacement = null;
        }

        public void Awake()
        {
            UpdateCount = 0;
        }

        public void Start()
        {
            TotalStartCount++;
            if (Replacement != null)
            {
                return;
            }

            Entity owner = Owner;
            Destroy();
            Replacement = owner.AddPooledChild<PooledStartReplacementEntity>();
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    internal sealed class AwakeSelfDestroyingEntity : Entity, IAwake
    {
        public void Awake()
        {
            Destroy();
        }
    }

    internal sealed class RegisterSelfDestroyingEntity : Entity, IAwake
    {
        public void Awake()
        {
        }

        protected override void RegisterSystem()
        {
            Destroy();
        }
    }

    internal sealed class PooledAwakeReplacementEntity : Entity, IAwake
    {
        private static bool _isCreatingReplacement;

        public static int TotalAwakeCount { get; private set; }

        public static PooledAwakeReplacementEntity Replacement { get; private set; }

        public static void ResetProbe()
        {
            _isCreatingReplacement = false;
            TotalAwakeCount = 0;
            Replacement = null;
        }

        public void Awake()
        {
            TotalAwakeCount++;
            if (_isCreatingReplacement)
            {
                Replacement = this;
                return;
            }

            _isCreatingReplacement = true;
            Entity owner = Owner;
            Destroy();
            Replacement = owner.AddPooledChild<PooledAwakeReplacementEntity>();
        }
    }

    internal sealed class AwakeSelfDestroyingScene : Scene
    {
        public AwakeSelfDestroyingScene(string name) : base(name)
        {
        }

        public override void Awake()
        {
            Destroy();
        }
    }

    internal abstract class PolymorphicBaseComponent : Entity, IAwake
    {
        public void Awake()
        {
        }
    }

    internal sealed class DerivedComponentA : PolymorphicBaseComponent
    {
    }

    internal sealed class DerivedComponentB : PolymorphicBaseComponent
    {
    }

    internal sealed class AttachableScene : Scene
    {
        public AttachableScene() : base("nested-scene-instance")
        {
        }
    }

    [ChildOf]
    internal sealed class ChildOnlyScene : Scene
    {
        public ChildOnlyScene(string name) : base(name)
        {
        }
    }
}
