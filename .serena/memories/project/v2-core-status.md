# GameEntity V2 Core 架构状态

范围：`src/GameEntity` 纯 C# 核心、`tests/GameEntity.Tests`、`apps/GameEntity.CoreTestApp`、V2 相关 docs。Unity 包未改造。

当前状态：V2 Core 已不再保留旧 Runtime 架构命名兼容层，并已完成 World 服务收口。顶层 `src/GameEntity/Runtime` 已合并进 `src/GameEntity/Core`。旧 `EntityGraph` 命名已改为 `EntityHierarchy`。`NodeRecord` 已改为 `EntityNode`，`NodeKind` 改为 `EntityNodeKind`，`NodeFlags` 改为 `EntityNodeFlags`。`BusinessId` 已改为 `EntityId`，`TypeId` 已改为 `ComponentTypeId`。旧 `GraphHandle` / `AssignGraphHandle` / `DisposeSelfFromGraph` / `ResolveGraph` 等残留已改为 `HierarchyHandle` / `AssignHierarchyHandle` / `DestroySelfFromHierarchy` / `ResolveHierarchy`。`TryGetRecord` / `GetRecord` / `SetRecord` / `GetAllRecords` / `RequireRecord` / `ValidateRecord` 等 API 命名已收口为 `TryGetNode` / `GetNode` / `SetNode` / `GetAllNodes` / `RequireNode` / `ValidateNode`。`ComponentIndexStore` 已删除，component 快速查询统一由 `NodeStore` 负责，避免组件索引双写。`LateUpdate` 已从 core 生命周期中删除，只保留 `World.Tick` + `IUpdate`。

目录结构：
- `src/GameEntity/Core/Hierarchy/`：`EntityHierarchy`、`NodeStore`、`ObjectStore`、`SceneRegistry`、`EntityNode`、`EntityNodeKind`、`EntityNodeFlags`
- `src/GameEntity/Core/Scheduling/`：`EntityScheduler`、`EntityUpdateBucket`、`SceneScheduleBucket`
- `src/GameEntity/Core/Diagnostics/`：`EntitySnapshot`、`EntityNodeInfo`、`EntityValidationIssue`、`EntityValidationResult`、`EntitySnapshotBuilder`、`EntityValidator`
- `src/GameEntity/Core/Handles/`：`EntityHandle`
- `src/GameEntity/Core/EntityLifecycle.cs`：World 持有的生命周期服务，替代旧 `EntitySystem.Instance`
- 示例应用：`apps/GameEntity.CoreTestApp`
- V2 草案文档：`docs/GameEntity-V2-Core-Hierarchy-Draft.md`

World 服务收口：
- `World.Instance` 是唯一保留的默认静态入口。
- `World` 构造时创建并初始化：`TimeInfo`、`IdGenerator`、`ObjectPool`、`EntityHierarchy`、`EntityLifecycle`、`DependencyRegistry`。
- `World.Hierarchy` 是 internal hierarchy 服务入口，替代旧 `World.Graph`。
- `World.Tick(deltaTime, unscaledDeltaTime)` 更新时间并驱动 scheduler update。
- `World.LateTick()` 已删除。
- `World.Dispose()` 现在先释放 hierarchy/dependencies/object pool/time/scenes，最后在 `ReferenceEquals(_instance, this)` 时清空 `_instance`，避免销毁回调中提前创建新 World。
- `World.Dispose()` 不再重复调用 `Hierarchy.Scheduler.Clear()`，scheduler 清理由 `EntityHierarchy.Dispose()` 负责。
- 旧 `Singleton<T>`、`ASingleton`、`ISingletonAwake`、`SingletonEntity<T>` 已删除。
- 旧 `TimeInfo.Instance`、`IdGenerator.Instance`、`ObjectPool.Instance`、`EntitySystem.Instance`、`DependencyRegistry.Instance`、`World.AddSingleton`、`InitializeDependencySystem` 已移除。

Scene 注册语义：
- `Scene(string name)` 只初始化 `Name`、创建状态，不分配 `Id` / `InstanceId`，不注册 hierarchy。
- `World.AddScene(name, scene)` 是 scene root 唯一正式注册入口，并负责分配 `Id` / `InstanceId`、注册 hierarchy root、放入 `_scenes`、调用 `Awake`。
- `World.AddScene(sceneName, scene)` 会校验 `scene.Name == sceneName`，避免 `_scenes` 与 `SceneRegistry` 名字不一致。
- scene root 通过 `Scene.Destroy()` 直接销毁时，`EntityHierarchy` 会调用 `World.UnregisterScene(scene)` 同步移除 `_scenes`，`World.GetScene(name)` 不会返回已销毁 scene。
- 公开 `Scene(long id, long instanceId, string name)` 已删除。
- 未注册 scene 没有有效 `Handle`，不能继续 `AddChild`。

生命周期与调度：
- 外部业务释放实体统一调用 `Entity.Destroy()` / `Scene.Destroy()`。
- `Entity` 不再实现 `IDisposable`，不再存在可调用的 `Entity.Dispose()` 业务入口。
- 销毁状态 API 是 `Entity.IsDestroyed`，旧 `IsDisposed` 已移除。
- 内部销毁流程命名统一为 `BeginDestroyFromHierarchy`、`DestroySelfFromHierarchy`、`IsDestroying`、`SetDestroying`。
- 观察者事件改为 `IEntityTreeObserver.OnEntityDestroyed` / `EntityTreeEventHub.NotifyEntityDestroyed`。
- 保留 `IAwake`、`IUpdate`、`IDestroy`。
- 删除 `ILateUpdate`。
- `EntityScheduler` 只维护 scene update bucket，不再有 late bucket 或 `SchedulePhase.LateUpdate`。
- `SchedulePhase` 已删除；scheduler 全局策略是单个 `IUpdateStrategy` 字段，entity 自身 `IHasUpdateStrategy` 优先。

身份模型：
- `EntityHandle` 现在只包含 `long NodeId`，不再包含 `Generation`。
- `NodeId` 由 `World.IdGenerator` 生成，是当前 World 生命周期内单调递增的 runtime node id，销毁后不回收、不复用。
- `NodeStore` 构造时注入 `IdGenerator`，已删除本地 `_nextNodeId`、`_generations` 与 `_freeNodeIds`，不再做 slot-map 式节点槽位复用。
- `EntityNode.NodeId`、`SceneNodeId`、`OwnerNodeId`、`EntityNodeInfo`、`EntityValidationIssue.NodeId`、`SceneRegistry`、`ObjectStore`、scheduler scene bucket key 全部使用 `long` node id。
- 旧 handle 失效依赖 node 从 `NodeStore` 删除；新实体会获得新的 long `NodeId`。
- 对象池复用 Entity 对象的旧引用保护仍由 `EntityRef<T>` 的 `InstanceId` 校验承担。
- 当前不支持 public 多 World；暂未给 `EntityHandle` 增加 RuntimeId/WorldId。

NodeStore 职责：
- `NodeId -> EntityNode` 节点表。
- `OwnerNodeId -> EntityId -> ChildNodeId` child 索引。
- `OwnerNodeId -> ComponentTypeId -> ComponentNodeId` component 索引和快速查询。
- `TryGetComponent` / `HasComponent` 已在 `NodeStore` 内提供。

重要 API：
- `World.Instance.Tick(float deltaTime, float unscaledDeltaTime)`
- `World.Instance.CaptureEntitySnapshot()` 返回 `EntitySnapshot`
- `World.Instance.ValidateEntities()` 返回 `EntityValidationResult`，替代旧 `ValidateEntityGraph()`，不保留兼容层
- `World.Instance.TryResolve<T>(EntityHandle, out T)` 是 handle 解析入口
- `Entity.Destroy()` 是实体/场景业务销毁入口
- `Entity.IsDestroyed` 是实体销毁状态查询入口
- `EntityRef<T>` 使用 `EntityHandle + InstanceId + 强引用缓存`，最终有效性以 `World.Instance.TryResolve` 为准
- 诊断模型使用 `EntityNodeInfo`、`EntityNodeKind`、`EntityId`、`ComponentTypeId` 命名，已移除 `Generation` 字段。

API 收口：
- `ObjectPool`、`TimeInfo`、`IdGenerator`、`DependencyRegistry`、`IDependencyRegistry`、`TypeHelper`、`IScene` 均已收为 internal。
- `Entity.IsFromPool` 已收为 internal，通过 `IPool` 供对象池内部使用。
- `Entity.Parent` 保留 public getter，setter 收为 internal；外部迁移使用 `Entity.ReparentTo(Entity newOwner)`。
- `Entity.Children` / `Entity.Components` 改为 `IReadOnlyCollection<Entity>`，并新增 `ContainsChild`、`ContainsComponent` 等查询入口。
- 普通 `AddChild` / `AddComponent` 不再暴露 `isFromPool` bool；池化创建使用 `AddPooledChild` / `AddPooledComponent`。
- `IAwake<T1,T2,T3,T4>` 已补齐对应 `AddChild` / `AddComponent` / pooled / WithId 4 参数创建 API。
- `AddChildWithId` / `AddComponentWithId` 暂时仍 public，但不再暴露池化 bool。

测试覆盖：
- `EntityHierarchyV2Tests`
- `EntitySchedulerTests`
- `EntityRefHandleTests`
- `V2CoreCoverageTests`
- `EntityValidationTests`
- `CrossSceneHierarchyTests`
- 覆盖对象池复用、World 服务自动初始化、World.Dispose 后对象池隔离、EntityRef/EntityHandle、IHasUpdateStrategy、IEntityLifecycleGate、IDependentComponent、RemoveChild、ClearChildren、World.Dispose、Scene.Destroy、validation 负例、跨 scene subtree reparent、scheduler scene bucket 等。
- 新增/调整覆盖：`World.AddScene` 拒绝 sceneName 与 `scene.Name` 不一致；直接 `Scene.Destroy()` 后 `World.GetScene(name)` 返回 null。
- EntityHandle 测试现在覆盖：销毁后旧 handle 解析失败，新实体分配新 NodeId；对象池复用同一 Entity 对象时旧 EntityRef 仍因 InstanceId 失效。

验证结果：
- `dotnet build "src/GameEntity/GameEntity.csproj" --no-restore`：通过，0 警告 0 错误
- `dotnet test "tests/GameEntity.Tests/GameEntity.Tests.csproj" --no-restore`：通过，30/30，测试耗时约 28ms
- `dotnet build "apps/GameEntity.CoreTestApp/GameEntity.CoreTestApp.csproj" --no-restore`：通过，0 警告 0 错误
- `rg` 搜索旧实体释放命名 `IsDisposed|EntityDisposed|NotifyEntityDisposed|OnEntityDisposed|BeginDisposeFromHierarchy|DisposeSelfFromHierarchy|OnDispose|IsDisposing|SetDisposing|DisposedObjectStillIndexed|DisposeSubtree|cascade dispose|owner dispose|entity dispose|scene dispose|Scene.Dispose()` 在 core/test/app/docs 中无残留。
- `rg` 搜索 `.Dispose()` 在 core/test/app 中只剩 `World.Dispose()`、`EntityHierarchy.Dispose()`、日志释放、测试基类和事件订阅释放；实体业务释放不再命中。

残留说明：
- `.NET System.Runtime`、NuGet `IncludeAssets=runtime`、Unity 包路径 `unity/GameEntity.Unity/Runtime/...` 不是 V2 Core 架构命名。
- 根目录旧 Unity 工程 `Assets/GameEntity/Runtime` 和 `GameEntity.Runtime.Tests` 未改，本轮按用户要求先不处理 Unity。
- 局部变量名 `record` 和 `NodeStore._records` 作为内部实现细节保留，外部 API 和核心概念命名已使用 `Node`。

后续建议：
- 若继续收口 public API，可评估 `AddChildWithId` / `AddComponentWithId` 是否改 internal 或专门 restore API。
- 对象池语义可继续从 `IsFromPool` 重构为 `IsPoolable / IsInPool / EntityCreateMode`，并补派生类池化状态重置 hook。
- 可考虑显式生命周期状态替代 `IsDestroyed => InstanceId == 0`。
- 若未来支持多 World，`EntityRef<T>` 与 `EntityHandle` 需要扩展为 world-aware / runtime-aware ref。
