# 实体层级与生命周期

> 依据：`src/GameEntity/Core/Entity.cs`、`Core/Hierarchy/`、`Core/EntityLifecycle.cs`，以及 `EntityHierarchyTests.cs`、`CrossSceneHierarchyTests.cs`。

## 层级规则

本页说明已经选择 Entity 后如何组织层级。若尚未确定某个对象是否应该成为 Entity，请先阅读[Entity 与普通数据的建模边界](02-entity-vs-data.md)。

若尚未确定一组 Entity 应成为独立 Scene，还是现有 Scene 下的 Child 子树，请先阅读[Scene 的创建与边界规范](02-scene-boundaries.md)。

层级只有三种节点：`SceneRoot`、`ChildEntity`、`ComponentEntity`。所有节点都归属于一个 Scene 分区。

```text
BattleScene (SceneRoot)
├── Player (ChildEntity)
│   ├── Stats (ComponentEntity)
│   └── Pet (ChildEntity)
└── Monster (ChildEntity)
```

`Parent` 与 `Owner` 当前语义相同，均由内部 `EntityHierarchy` 维护。`Children` 和 `Components` 返回只读快照，枚举期间的结构修改不会直接暴露内部存储。

## `ChildOf` 放置约束

当某种 Entity 在模型上只能作为 Child，或只能归属于特定 owner 时，使用运行时约束：

```csharp
[ChildOf(typeof(UnitEntity))]
public sealed class EquipmentEntity : Entity, IAwake
{
    public void Awake() { }
}
```

- `[ChildOf]` 表示该类型只能通过 Child API 挂接，不能使用 `AddComponent`。
- `[ChildOf(typeof(UnitEntity))]` 还要求直接 owner 是 `UnitEntity` 或其派生类型。
- `AddChild`、`AddPooledChild` 和 `ReparentTo` 都执行同一约束；失败的重新挂接会保留原 owner。
- 创建入口会在构造或取出池对象前尽早验证，非法放置不会留下临时节点，也不会消耗池对象。
- `ChildOf` 只约束直接 owner 和节点种类，不表示更新要求，也不自动创建 owner。

`Scene` 派生类型是硬边界：只能通过 `World.AddScene` 成为 SceneRoot，不能作为 Child 或 Component。反过来，Scene 类型一旦声明 `ChildOf`，就表示类型声明与 SceneRoot 身份冲突，`AddScene` 会在分配运行时身份和节点前拒绝它。

未声明 `ChildOf` 的 Entity 可由业务选择作为 Child 或 Component。Core 不提供 `ComponentOf`；Component 的 owner 规则若需要更强约束，应由明确的业务 Factory 创建并校验，不能把不存在的 Attribute 当成框架能力。

## 生命周期接口

Core 通过接口而不是基类虚方法分发生命周期：

```csharp
public interface IAwake { void Awake(); }
public interface IAwake<T> { void Awake(T value); }
public interface IStart { void Start(); }
public interface IFixedUpdate { void FixedUpdate(float fixedDeltaTime); }
public interface IUpdate { void Update(float deltaTime); }
public interface IDestroy { void OnDestroy(); }
```

`IAwake` 支持 0～4 个泛型参数。实体类型实现哪个签名，就必须使用匹配的 `AddChild` 或 `AddComponent` 重载。

```text
Create / Pool.Get
  → Attach
  → Awake(args)                         立即执行
  → 等待 IsUpdateEnabled 和 RequireForUpdate
  → Start()                             第一次满足运行条件时执行一次
  → FixedUpdate() 或 Update()           可在同一次更新 pass 紧接 Start 执行
  → OnDestroy()
```

- `Awake(args)`：接收外部创建参数、重置池化状态、建立自身字段不变量。不得假设其他 Component 已经添加。
- `Start()`：可选的第二阶段初始化。只在 Entity 第一次具备运行条件时执行一次。
- `FixedUpdate()`：统一固定步长的 Model 模拟，由 `World.FixedUpdate` 驱动。
- `Update()`：普通帧循环，由 `World.Update` 驱动；可通过 `IEntityUpdateInterval` 降低单个 Entity 的调用频率。
- `OnDestroy()`：必须兼容已经 Start、尚未 Start、Awake 失败后回滚三种状态。

泛型 `AddChild/AddComponent` 是原子创建流程。`Awake` 或调度注册抛异常时，Core 会销毁刚挂接的节点并向调用者重新抛出，不会留下半初始化 Entity。Entity 在 `Awake` 或 `RegisterSystem` 中销毁自身也属于创建失败，入口抛出 `InvalidOperationException`，不会返回已销毁或已回池对象。Core 用创建前的 Handle 与 `InstanceId` 锚定原始生命，因此旧池化生命自毁并立即复用时，回滚不会销毁已经形成的新生命。若 `Awake` 内继续创建子节点，最外层创建成功后才会把整棵新增子树发布给观察者；失败则整棵临时子树回滚。`RequireForUpdate` 要求未满足不是创建异常，而是可诊断的等待状态。

`Start` 抛异常时，节点标记为 `StartFaulted`，本次生命期不再重试，也不会进入 `Update`。`ValidateEntities()` 会报告 `StartFaulted` Error；应销毁并重新创建该 Entity，而不是依赖每帧自动重试。

销毁是幂等的，并按子树级联。`OnDestroy` 只会执行一次；销毁后 `InstanceId == 0`、`IsDestroyed == true`，旧 Handle 不能再解析。`Entity.Destroy()` 是不可重写的模板入口，业务清理只实现 `IDestroy.OnDestroy()`，不要在派生类中隐藏同名方法。用户销毁回调或内部扩展钩子抛异常时，Core 会记录错误并继续完成 Scheduler、Scene 注册表、对象存储、节点和 Handle 清理。

## 查询关系

```csharp
Entity directOwner = entity.Parent;
Scene scene = entity.GetSceneRoot();

bool gotDirectOwner = entity.TryGetOwner(out StatsComponent owner);
bool foundAncestor = entity.TryFindOwner(out UnitEntity unit);
bool foundComponent = entity.TryGetComponentInAncestors(out StatsComponent stats);
bool foundSibling = entity.TryGetSiblingComponent(out StatsComponent sibling);
```

- `TryGetOwner<T>`：只检查直接 owner。
- `TryFindOwner<T>`：沿 owner 链向上查找。
- `TryGetComponentInParent<T>`：在直接 parent 上找组件。
- `TryGetComponentInAncestors<T>`：从当前实体/owner 链语义范围向上找组件。
- `TryGetSiblingComponent<T>`：当当前节点为 component 时，从相同 owner 查同类型组件。

查不到时优先使用 `Try...` 版本。当前 `GetChild`、`GetComponent` 等查询可能返回 `null`，不要仅凭方法名假设它一定抛异常。`GetComponent<T>`、`GetComponent(Type)`、`TryGetComponent`、`ContainsComponent` 和 `RemoveComponent` 统一按精确运行时类型匹配，不隐式选择派生类型；需要多态查询时显式遍历 `Components` 并处理零个、一个或多个匹配项。

## 重新挂接

```csharp
monster.ReparentTo(player);
```

该操作移动整个子树，并保证：

- 旧 owner 与新 owner 的索引同步更新。
- 跨 Scene 时，子树所有 Child 和 Component 的 Scene 分区一起迁移。
- 已注册的 `IStart`/`IFixedUpdate`/`IUpdate` 节点迁移到新 Scene 的对应调度桶，普通 Update 累计时间一并迁移。
- 只有当前 World 层级中仍存活、Handle 有效的节点可以调用 `ReparentTo`；`new Entity()`、已销毁对象和旧 World 对象都会被拒绝，不能借此插入或复活节点。
- 不能把节点挂到自己或自己的后代下，避免形成环。
- Scene 根节点不能作为普通实体重新挂接。

业务代码不能直接修改内部层级，也不能通过 Unity Transform 改写关系。

## 销毁与移除

```csharp
entity.Destroy();                    // 销毁实体及其全部子树
owner.RemoveChild(child.Id);         // 销毁指定 child 子树
owner.RemoveComponent<StatsComponent>(); // 销毁组件及组件子树
owner.ClearChildren();               // 销毁全部 child，不移除 owner 自身组件
World.Instance.RemoveScene("Battle");   // 销毁整个 Scene
```

`Scene.Destroy()` 也会从 World 的 Scene 注册表移除自身。应用结束时仍应调用 `World.Dispose()`，确保所有 Scene、调度状态和对象池一起清理。

迁移旧代码时，删除所有 `override Destroy()`；把资源释放、退订和外部对象清理移入 `IDestroy.OnDestroy()`。`Id` 与 `InstanceId` 也由 Core 独占写入，业务自定义标识使用 `AddChildWithId` 或独立领域字段。

## Ready 与更新状态

普通 Entity 不需要实现额外状态接口。未实现时，Core 默认认为该 Entity 已 Ready，并允许其正常参与更新调度。

异步初始化且会成为其他 Component 更新要求的 Entity 可实现：

```csharp
public interface IEntityReadyState
{
    bool IsReady { get; }
}
```

某 Component 作为其他 Component 的更新要求时，`IsReady == false` 表示该要求尚未就绪。`IsReady` 只表达能否满足更新要求，不会自动阻止该 Entity 自己更新。

需要保留在 Entity 树中但临时停止自身运行的 Entity 可实现：

```csharp
public interface IEntityUpdateState
{
    bool IsUpdateEnabled { get; }
}
```

- `IsUpdateEnabled == false` 时，Scheduler 跳过该 Entity 的 `Start`、`FixedUpdate` 和 `Update`，普通 Update 也不会累计降频时间。
- 更新状态只作用于当前 Entity，不自动传递给 Child 或 Component。
- 更新状态不影响 `Awake`、`Destroy`、事件、异步任务、外部方法或 Unity 行为。
- 读取 `IsUpdateEnabled` 抛异常时，Scheduler 记录错误并只跳过当前 Entity；不会中断同一次更新 pass 中的其他 Entity。
- 读取要求 Component 的 `IsReady` 抛异常时，当前 Entity 被阻止运行并报告 `UpdateRequirementStateError`；不会把异常扩散到整个 World 更新 pass。
- 如果某个 Entity 必须在自己的 Ready 后才能运行，应同时实现两个接口，并在自身的 `IsUpdateEnabled` 中明确组合 `IsReady`。
- Core 只读取这些状态，不负责启动异步任务、决定暂停原因、取消任务或释放资源；这些责任仍属于 Entity 自身或宿主。

Core 没有通用的 Entity `Enable/Disable` 生命周期，也不会沿 owner 树传播暂停。所有权树决定归属和级联销毁，不天然等于同一个暂停域。确实需要暂停整棵业务子树时，应由业务时间域或协调 Component 统一计算状态，再让需要参与的节点显式返回 `IsUpdateEnabled == false`。

## 设计建议

- Scene 只承担根服务和会话边界，不堆积具体玩法逻辑。
- Child 表达“谁拥有谁”，Component 表达“谁具有什么能力”。
- `Awake` 保存创建参数和重置本地状态；依赖其他 Component 的运行初始化放入 `Start`。
- `Start` 每个 Entity 生命期最多成功一次，不用于依赖实例的热替换通知。
- `OnDestroy` 释放外部订阅、句柄和原生资源；Entity 子树无需手工逐个销毁。
- 领域持久化保存业务 ID 和业务数据，不保存 `InstanceId` 或 `EntityHandle`。

[返回知识库首页](README.md)
