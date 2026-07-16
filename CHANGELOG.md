# Changelog

## main

### Core

- 重构 Entity 层级内核，统一由 `EntityHierarchy` 管理 Scene、Child Entity、Component Entity 的归属关系。
- 新增 `EntityHandle`，用于在 `World` 内安全标识运行时节点；销毁后的 handle 不再解析到新对象。
- `EntityRef<T>` 改为同时校验 `InstanceId` 和 `EntityHandle`，避免对象池复用后引用误命中。
- `World` 增加 `TryResolve<T>(EntityHandle, out T)`、`CaptureEntitySnapshot()` 和 `ValidateEntities()`。
- `World` 收口为封闭构造的 `sealed` 单例，并新增 `ObserveEntities`；观察者可按父到子回放已提交树，解决 Unity Runner 与业务 `AddScene` 的初始化先后问题。
- `Scene` 必须通过 `World.Instance.AddScene(sceneName, scene)` 注册后才能继续挂载子节点和组件。
- Scene 和 Entity 创建改为事务式发布：完成 `Awake`、结构挂接和调度注册后才通知观察者，失败时回滚整棵临时子树，不发布伪注册/销毁事件。
- 支持跨 Scene `ReparentTo`，移动实体时会同步迁移整棵子树的 Scene 分区和调度归属。
- `IUpdate` 调度按 Scene 分区维护，Scene 移除或 Entity 销毁后会自动停止对应更新。
- 新增 `IFixedUpdate` 与 `World.FixedUpdate`，把统一固定步长的 Model 模拟和普通帧更新拆成两条 Scene 调度通道。
- 新增 `IEntityUpdateInterval.UpdateInterval`，为普通 `IUpdate` 提供按 Entity Handle 独立累计、到期至多调用一次的 View Update LOD；暂停期间不累计，跨 Scene 保留进度，池化新生命期自动重置。
- 移除 Entity 级 `IUpdateStrategy`、`IHasUpdateStrategy`、`AllStrategy` 和 `AnyStrategy`；固定步长改由 World 统一驱动，普通 Update 不再选择或接收非缩放时间。
- 使用单职责的 `IEntityReadyState.IsReady` 和 `IEntityUpdateState.IsUpdateEnabled` 替换 `IEntityLifecycleGate`；Ready 只参与更新要求判断，更新状态只控制当前 Entity 的更新调度。
- 新增 `IStart` 两阶段生命周期：`Awake(args)` 立即保存创建参数，`Start()` 在首次满足更新状态与更新要求时执行一次，并可在同一次更新 pass 进入 `Update`。
- 使用 `[RequireForUpdate]` 取代 `[DependsOn]`、`IDependentComponent` 和 `DependentComponentBase`；旧依赖能力收敛为同 owner 精确 Component 的纯 Scheduler 更新要求，不再维护反向注册表或状态变化通知。
- `RequireForUpdate` 增加类型环检测，并将 Ready getter 异常隔离为 `UpdateRequirementStateError`；`IsUpdateEnabled` getter 异常同样只跳过当前 Entity。
- `[ChildOf]` 成为创建和 `ReparentTo` 都执行的运行时放置约束；删除未形成有效契约的 `ComponentOfAttribute`。
- `AddChild/AddComponent` 在 `Awake` 或调度注册失败时会回滚新节点并重新抛出；`Start` 失败会标记 `StartFaulted`，阻止该生命期后续更新。
- 移除只挂接但不执行完整生命周期的 `AddComponent(Entity)` 及未使用的内部动态挂接入口，公开创建统一使用泛型重载。
- 日志默认改为 `NullLogger`，作为库默认保持静默；宿主可通过 `Log.Logger` 注入 `ConsoleLogger` 或自定义 logger。
- 移除 `CompositeLogger`、`FileLogger`、`LogManager`。
- 清理测试中的 V2 命名痕迹，测试类统一使用当前 core 命名。

### Unity

- 新增 Unity Package Manager 包：`unity/Packages/com.firleaves.gameentity.unity`。
- 包对外名称为 `GameEntity for Unity`，包 ID 为 `com.firleaves.gameentity.unity`，代码命名空间为 `GameEntity.Unity`。
- 用户侧入口统一为 `GameEntityRunner`，用于驱动 `World.Update`、接管 Unity 日志、把 Entity 树投影到 Unity Hierarchy。
- `GameEntityRunner` 默认以 30Hz 驱动 `World.FixedUpdate`，提供单帧最大追赶次数与 `FixedInterpolationAlpha`，随后每帧调用一次普通 `World.Update`。
- Unity 侧使用 GameObject 只做运行时数据查看和 Inspector 调试，不作为业务层级编辑入口。
- 新增 `Samples~/GameEntityDemo`，导入后可直接运行并查看 Entity 树、组件数据和 `ReparentTo` 效果。
- 修复 Unity registry 中 Unity fake-null 对象残留的问题。
- 修复同一个 Entity 重复绑定到不同 GameObject 时旧 view 未解绑的问题。
- Inspector 增加 `IStart`、`IFixedUpdate`、`IEntityUpdateInterval`、Started/Start Faulted 与更新要求 Missing/NotReady/State Error 状态，并隔离状态 getter 异常。

### Repository

- Unity 工程移动到 `unity/` 目录。
- 旧 Unity 工程基线已打 tag：`v0`。
- `.serena/` 和 `/docs/` 作为本地工具状态和未发布设计草稿，不再进入仓库跟踪。

## 2025-09-18

- Entity 分离 Awake 和 Update，没有实现 `IAwake`、只实现 `IUpdate` 的实体也可以 Update。
- 异步组件先触发 Awake，加载完成后才执行自己的 Update。
- 优化 Unity Inspector 显示，支持显示属性和更多类型。
- 增加使用说明。
