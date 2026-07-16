# 引用、诊断与日志

> 依据：`Core/EntityRef.cs`、`Core/Handles/`、`Core/Diagnostics/`、`Logging/`，以及 `EntityRefHandleTests.cs`、`EntityValidationTests.cs`。

## 三种标识

- `Id`：业务查询 ID，可通过 `AddChildWithId` 指定；持久化时通常保存这个或更明确的领域 ID。
- `InstanceId`：对象某次生命的身份；对象池复用后改变。
- `EntityHandle`：当前 World 层级节点身份；销毁后永久失效，不会复用旧 NodeId。

三个运行时身份属性都由 Core 维护。业务只能读取 `Id`、`InstanceId` 和 `Handle`；需要指定 Child 查询 ID 时使用 `AddChildWithId`，需要领域身份时定义独立字段。派生类不能改写这些属性来模拟销毁、复活或迁移。

## EntityRef

业务对象需要跨帧持有实体时，优先使用 `EntityRef<T>`：

```csharp
EntityRef<UnitEntity> target = unit;

if (target.TryGet(out UnitEntity alive))
{
    // alive 同时通过 InstanceId 和 Handle 校验。
}

bool isAlive = target.IsAlive;
UnitEntity value = target.ValueOrNull;
EntityHandle handle = target.Handle;
```

实体销毁或池化复用后，旧引用返回 `false/null`。不要依赖隐式 `EntityRef<T> → T` 转换做非空假设，显式 `TryGet` 更清楚。

## Handle 解析

短消息、队列和系统边界可以只携带 Handle：

```csharp
EntityHandle handle = unit.Handle;

if (World.Instance.TryResolve(handle, out UnitEntity resolved))
{
    // 类型和存活状态均匹配。
}
```

Handle 只在唯一 `World.Instance` 的当前运行时会话内有效，不适合作为存档、网络协议的永久实体 ID。完整的单 World 契约见[知识库首页](README.md#单-world-硬约束)。

`World.Dispose()` 后不要继续调用保存的旧 World 引用。旧引用的 `TryResolve` 与其他公开实例入口会抛出 `ObjectDisposedException`；重新访问 `World.Instance` 才会顺序创建下一次全新会话。

## 结构快照

```csharp
EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();

foreach (EntityNodeInfo node in snapshot.Nodes)
{
    Console.WriteLine(
        $"{node.NodeId} {node.Kind} {node.EntityType} owner={node.OwnerNodeId}");
}
```

快照是只读投影，包含节点、业务/实例 ID、Scene 和 owner NodeId、组件类型 ID、种类、存活/销毁状态、`IsStarted`、`IsStartFaulted`、类型名与视图名。它不参与运行时修改，适合测试、调试 UI 和问题报告。

## Entity 树观察者

引擎适配器和调试工具可通过公开观察者订阅树变化：

```csharp
public sealed class MyTreeObserver : IEntityTreeObserver
{
    public void OnEntityRegistered(Entity entity) { }
    public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent) { }
    public void OnEntityDestroyed(Entity entity) { }
}

IDisposable subscription = World.Instance.ObserveEntities(
    new MyTreeObserver(),
    replayExisting: true);

// adapter 销毁时
subscription.Dispose();
```

契约如下：

- `replayExisting` 默认为 `true`，会同步按父到子顺序回放当前已提交树，因此 adapter 可以晚于 Scene/Entity 初始化。
- 新创建节点只有完成 `Awake`、结构挂接和 Scheduler 注册后才发布；回调中可读取有效的 `Handle`、`Owner` 和 `SceneRoot`。
- Scene 或 Entity 创建失败时，临时子树整体回滚，不发送 Registered 或 Destroyed 假事件。
- `ReparentTo` 对已发布节点只发送 ParentChanged，不重新发送 Registered。
- 单个观察者回调抛异常会被记录并隔离，不阻止 Core 或其他观察者；观察者仍应保持回调轻量且自行维护投影一致性。

观察者必须通过唯一的 `World.Instance` 注册，不能直接访问内部事件中心。多个投影、Unity Scene 和业务 Scene 观察的都是同一棵 World 运行时森林。

## 结构校验

```csharp
EntityValidationResult result = World.Instance.ValidateEntities();

if (!result.IsValid)
{
    foreach (EntityValidationIssue issue in result.Issues)
    {
        Log.Error($"[{issue.Code}] node={issue.NodeId}: {issue.Message}");
    }
}
```

建议在以下位置校验：复杂批量变更后、测试断言中、开发构建的诊断命令中。不要默认每帧调用，它会遍历和核对层级存储。

常见诊断码：

- `UpdateRequirementMissing`、`UpdateRequirementNotReady`：可恢复的等待条件，级别为 Warning，不令 `IsValid` 变为 false。
- `UpdateRequirementStateError`：读取要求 Component 的 `IsReady` 失败，级别为 Error。
- `UpdateRequirementCycle`：更新要求类型图存在环；`UpdateRequirementMetadataError` 表示 Attribute 元数据非法。
- `PlacementConstraintViolation`：现有节点违反 `ChildOf` 的 Child/owner 类型约束。
- `StartFaulted`：生命周期第二阶段失败，当前生命期不会再运行。
- `EntityHandleMismatch`、`EntityIdMismatch`、`EntityInstanceIdMismatch`：NodeStore 与实际 Entity 身份不一致。
- `ObjectMissing`、`ObjectWithoutNode`、`ObjectStoreHandleMismatch`：NodeStore 与 ObjectStore 的双向索引不一致。
- `SceneRegistryNameMissing`、`SceneRegistryNodeMissing`、`SceneRegistryTargetMissing`：SceneRoot 与 SceneRegistry 的双向映射不一致。
- `SchedulerDuplicateHandle`、`SchedulerDuplicateRegistration`、`SchedulerRegistrationUnlisted`：Scheduler 顺序列表、membership 或有效注册不一致。
- `NestedSceneType`、`SceneRootTypeMismatch`：Scene 派生类型与节点种类不一致。

业务不应把所有 Issue 一律当成结构损坏，但 Error 必须在测试或开发诊断中处理。

## 日志

Core 默认使用 `NullLogger`，库在未配置宿主时保持静默：

```csharp
Log.Logger = new ConsoleLogger();
Log.SetLogLevel(debug: true, info: true, warning: true, error: true);
```

也可实现 `ILogger` 接入现有日志设施。Unity 的 `GameEntityRunner` 在 `UseUnityLogger` 开启时自动注入 `UnityGameEntityLogger`。

`Warning`、`Error`、`Exception` 始终调用 logger。`Debug` 和 `Info` 受条件编译符号控制，例如 `ENABLE_LOG`、`ENABLE_DEBUG_LOG`、`ENABLE_INFO_LOG`；只设置日志级别不代表这些调用一定被编译进产物。

## Inspector 隐私与降噪

Unity Inspector 反射展示 Entity 成员。对不应展示、代价高或可能递归的字段/属性使用：

```csharp
[GameEntityInspectorIgnore("运行时缓存，不进入调试视图")]
public Dictionary<long, object> Cache { get; } = new();
```

该属性只影响调试 Inspector，不影响序列化、业务逻辑或远程调试协议。

[返回知识库首页](README.md)
