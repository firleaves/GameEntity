# GameEntity V2 Core 架构状态

范围：`src/GameEntity` 纯 C# 核心、`tests/GameEntity.Tests`、`apps/GameEntity.CoreTestApp`、V2 相关 docs。Unity 包未改造。

当前状态：V2 Core 已不再保留旧 Runtime 架构命名兼容层，并已完成 World 服务收口。顶层 `src/GameEntity/Runtime` 已合并进 `src/GameEntity/Core`。旧 `EntityGraph` 命名已改为 `EntityHierarchy`，因为该核心服务表达的是实体 owner 层级、scene 分区、subtree 迁移和级联销毁，而不是通用 graph 数据结构。`NodeRecord` 已改为 `EntityNode`，`NodeKind` 改为 `EntityNodeKind`，`NodeFlags` 改为 `EntityNodeFlags`，避免 record/graph 语义过重。`BusinessId` 已改为 `EntityId`，`TypeId` 已改为 `ComponentTypeId`。`LateUpdate` 已从 core 生命周期中删除，只保留 `World.Tick` + `IUpdate`。

目录结构：
- `src/GameEntity/Core/Hierarchy/`：`EntityHierarchy`、`NodeStore`、`ObjectStore`、`ComponentIndexStore`、`SceneRegistry`、`EntityNode`、`EntityNodeKind`、`EntityNodeFlags`
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
- 旧 `Singleton<T>`、`ASingleton`、`ISingletonAwake`、`SingletonEntity<T>` 已删除。
- 旧 `TimeInfo.Instance`、`IdGenerator.Instance`、`ObjectPool.Instance`、`EntitySystem.Instance`、`DependencyRegistry.Instance`、`World.AddSingleton`、`InitializeDependencySystem` 已移除。

生命周期与调度：
- 保留 `IAwake`、`IUpdate`、`IDestroy`。
- 删除 `ILateUpdate`。
- `EntityScheduler` 只维护 scene update bucket，不再有 late bucket 或 `SchedulePhase.LateUpdate`。
- `SchedulePhase` 已删除；scheduler 全局策略是单个 `IUpdateStrategy` 字段，entity 自身 `IHasUpdateStrategy` 优先。

重要 API：
- `World.Instance.Tick(float deltaTime, float unscaledDeltaTime)`
- `World.Instance.CaptureEntitySnapshot()` 返回 `EntitySnapshot`
- `World.Instance.ValidateEntities()` 返回 `EntityValidationResult`，替代旧 `ValidateEntityGraph()`，不保留兼容层
- `World.Instance.TryResolve<T>(EntityHandle, out T)` 是 handle 解析入口
- `EntityRef<T>` 使用 `EntityHandle + InstanceId + 强引用缓存`，最终有效性以 `World.Instance.TryResolve` 为准
- 诊断模型使用 `EntityNodeInfo`、`EntityNodeKind`、`EntityId`、`ComponentTypeId` 命名。

Scene 注册语义：
- `Scene(string name)` 只初始化 `Name`、创建状态，不分配 `Id` / `InstanceId`，不注册 hierarchy。
- `World.AddScene(name, scene)` 是 scene root 唯一正式注册入口，并负责分配 `Id` / `InstanceId`、注册 hierarchy root、放入 `_scenes`、调用 `Awake`。
- 公开 `Scene(long id, long instanceId, string name)` 已删除。
- 未注册 scene 没有有效 `Handle`，不能继续 `AddChild`。

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
- 覆盖对象池复用、World 服务自动初始化、World.Dispose 后对象池隔离、EntityRef/EntityHandle、IHasUpdateStrategy、IEntityLifecycleGate、IDependentComponent、RemoveChild、ClearChildren、World.Dispose、Scene.Dispose、validation 负例、跨 scene subtree reparent、scheduler scene bucket 等。

验证结果：
- `dotnet build "src/GameEntity/GameEntity.csproj" --no-restore`：通过，0 警告 0 错误
- `dotnet test "tests/GameEntity.Tests/GameEntity.Tests.csproj" --no-restore`：通过，29/29，测试耗时约 25ms
- `dotnet build "apps/GameEntity.CoreTestApp/GameEntity.CoreTestApp.csproj" --no-restore`：通过，0 警告 0 错误
- `rg` 搜索旧命名 `EntityGraph|ValidateEntityGraph|World.Instance.Graph|Core/Graph|Graph/|NodeRecord|NodeKind|NodeFlags|BusinessId|TypeId` 在 core/test/app/docs 中无旧语义残留；命中 `EntityNodeKind`、`ComponentTypeId` 等新命名是预期。

残留说明：
- `.NET System.Runtime`、NuGet `IncludeAssets=runtime`、Unity 包路径 `unity/GameEntity.Unity/Runtime/...` 不是 V2 Core 架构命名。
- 根目录旧 Unity 工程 `Assets/GameEntity/Runtime` 和 `GameEntity.Runtime.Tests` 未改，本轮按用户要求先不处理 Unity。

后续建议：
- 若继续收口 public API，可评估 `AddChildWithId` / `AddComponentWithId` 是否改 internal 或专门 restore API。
- 可评估 `EntityHandle` 是否加入 `InstanceId`，让 handle 自身代表完整运行时生命身份。
- 对象池语义可继续从 `IsFromPool` 重构为 `IsPoolable / IsInPool / EntityCreateMode`。
- 若未来支持多 World，`EntityRef<T>` 需要扩展为 world-aware ref。