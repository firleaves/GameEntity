# Changelog

## main

### Core

- 重构 Entity 层级内核，统一由 `EntityHierarchy` 管理 Scene、Child Entity、Component Entity 的归属关系。
- 新增 `EntityHandle`，用于在 `World` 内安全标识运行时节点；销毁后的 handle 不再解析到新对象。
- `EntityRef<T>` 改为同时校验 `InstanceId` 和 `EntityHandle`，避免对象池复用后引用误命中。
- `World` 增加 `TryResolve<T>(EntityHandle, out T)`、`CaptureEntitySnapshot()` 和 `ValidateEntities()`。
- `Scene` 必须通过 `World.Instance.AddScene(sceneName, scene)` 注册后才能继续挂载子节点和组件。
- 支持跨 Scene `ReparentTo`，移动实体时会同步迁移整棵子树的 Scene 分区和调度归属。
- `IUpdate` 调度按 Scene 分区维护，Scene 移除或 Entity 销毁后会自动停止对应更新。
- 依赖组件继续支持 `[DependsOn]`、`IDependentComponent` 和 `DependentComponentBase`，依赖不满足时不会进入更新调度。
- 日志默认改为 `NullLogger`，作为库默认保持静默；宿主可通过 `Log.Logger` 注入 `ConsoleLogger` 或自定义 logger。
- 移除 `CompositeLogger`、`FileLogger`、`LogManager`。
- 清理测试中的 V2 命名痕迹，测试类统一使用当前 core 命名。

### Unity

- 新增 Unity Package Manager 包：`unity/Packages/com.firleaves.gameentity.unity`。
- 包对外名称为 `GameEntity for Unity`，包 ID 为 `com.firleaves.gameentity.unity`，代码命名空间为 `GameEntity.Unity`。
- 用户侧入口统一为 `GameEntityRunner`，用于驱动 `World.Tick`、接管 Unity 日志、把 Entity 树投影到 Unity Hierarchy。
- Unity 侧使用 GameObject 只做运行时数据查看和 Inspector 调试，不作为业务层级编辑入口。
- 新增 `Samples~/GameEntityDemo`，导入后可直接运行并查看 Entity 树、组件数据和 `ReparentTo` 效果。
- 修复 Unity registry 中 Unity fake-null 对象残留的问题。
- 修复同一个 Entity 重复绑定到不同 GameObject 时旧 view 未解绑的问题。

### Repository

- Unity 工程移动到 `unity/` 目录。
- 旧 Unity 工程基线已打 tag：`v0`。
- `.serena/` 和 `/docs/` 作为本地工具状态和未发布设计草稿，不再进入仓库跟踪。

## 2025-09-18

- Entity 分离 Awake 和 Update，没有实现 `IAwake`、只实现 `IUpdate` 的实体也可以 Update。
- 异步组件先触发 Awake，加载完成后才执行自己的 Update。
- 优化 Unity Inspector 显示，支持显示属性和更多类型。
- 增加使用说明。
