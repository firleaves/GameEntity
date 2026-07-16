# 调度、更新要求与池化

> 依据：`Core/Scheduling/EntityScheduler.cs`、`Dependency/`、`Core/ObjectPool.cs`，以及 `EntitySchedulerTests.cs`、`CoreCoverageTests.cs`。

## 更新调度

实现 `IStart`、`IFixedUpdate` 或 `IUpdate` 的实体在完成 `Awake` 后自动注册。每个 Scene 有独立调度分区，并分别维护固定更新与普通更新通道；实体销毁、Scene 移除或跨 Scene 移动时，两条通道及普通 Update 的累计时间自动同步。

```csharp
public sealed class MotionComponent : Entity, IAwake, IUpdate
{
    public void Awake() { }

    public void Update(float deltaTime)
    {
        // 执行一帧逻辑。
    }
}
```

普通调度顺序固定为 `IsUpdateEnabled → RequireForUpdate → Start once → UpdateInterval → Update`；固定调度顺序为 `IsUpdateEnabled → RequireForUpdate → Start once → FixedUpdate`。首次成功执行 `Start` 后，同一次更新 pass 可以紧接着执行对应更新。

每次 `World.Update` 或 `World.FixedUpdate` 都在入口一次性冻结所有 Scene 的全局工作快照，并按 Handle 去重。其边界契约是：

- 同一 Entity Handle 在一个 pass 中最多执行一次，即使本次回调中跨 Scene 迁移，或底层列表存在重复项。
- pass 开始后新建的 Entity 不参与当前 pass；迁入另一个 Scene 的 Entity 也不会因为目标 Scene 尚未处理而再次执行，统一等待下一次对应更新入口。
- `World.Update` 与 `World.FixedUpdate` 不能在任一更新回调中嵌套调用，包括两个通道互相调用；重入会抛出 `InvalidOperationException`。需要追加工作时写入业务队列，由宿主在当前 pass 返回后再驱动下一次更新。

调度器捕获单个 `FixedUpdate` 或 `Update` 的异常并写入 `Log.Error`，不会让其余实体停止更新。`Start` 异常只记录一次并把节点标记为 `StartFaulted`，该生命期不再重试或更新。业务仍应自行维护异常后的数据一致性。

## 固定 Model 更新

固定步长属于一组 Model 的共同时间线，不属于单个 Entity 的策略。Model Entity 实现 `IFixedUpdate`，宿主统一调用 `World.FixedUpdate`：

```csharp
public sealed class MovementModelComponent : Entity, IAwake, IFixedUpdate
{
    public void Awake() { }

    public void FixedUpdate(float fixedDeltaTime)
    {
        // 同一次 World.FixedUpdate 中的所有 Model 使用同一个 fixedDeltaTime。
    }
}
```

```csharp
World.Instance.FixedUpdate(1f / 30f);
```

Unity 的 `GameEntityRunner` 默认以 30Hz 累计并驱动固定更新，每个渲染帧最多执行 `MaxFixedStepsPerFrame` 次，随后调用一次 `World.Update(Time.deltaTime)`。`FixedInterpolationAlpha` 提供剩余累计时间与固定步长的比例，Unity View 可据此在 Model 的前后状态之间插值。

这正是“Model 30Hz、View 60Hz”的推荐实现：Model 使用统一的 `IFixedUpdate` 通道，View 使用每渲染帧一次的 `IUpdate` 通道并对 Model 状态插值。不要给每个 Model Entity 配独立更新策略，否则同一模拟域会产生不同时间线。

## 时间语义与暂停边界

Core 只接受宿主提供的一条游戏时间：

```csharp
World.Instance.FixedUpdate(fixedDeltaTime);
World.Instance.Update(deltaTime);
```

- `IFixedUpdate` 收到本次固定模拟步长；`IUpdate` 收到本次普通游戏帧时间。
- Core 不同时传入 scaled/unscaled 两套时间，也没有 `IUnscaledUpdate`。全局时间缩放由宿主在调用 World 前决定；局部真实时间需求应注入业务 Clock 或留在引擎适配层。
- Core 没有 `ILateUpdate`。View 的渲染后处理、相机和引擎时序留在 Unity 层；只有形成跨引擎的稳定需求后才考虑进入 Core。
- `IEntityUpdateState` 是当前 Entity 的调度开关，不是完整时间系统；它不会暂停异步任务、外部事件或子树。
- `IEntityUpdateInterval` 是调用频率 LOD，不是减速。它减少调用次数，但到期仍把完整累计时间传给 `IUpdate`。

因此，全局减速/加速应由宿主传入缩放后的普通 `deltaTime`，并按业务规则调整固定更新的累计。真正的全局暂停应让宿主停止调用需要暂停的 World 更新入口；`World.Update(0)` 仍会让符合条件的 `IStart` 和无间隔 `IUpdate` 以 0 delta 执行。局部暂停使用显式业务暂停域加 `IEntityUpdateState`。当前 Core 不提供 Scene 级 Clock、Scene 级暂停或树级自动传播。

## 普通 Update 降频

`IEntityUpdateInterval` 只控制当前 Entity 的普通 `IUpdate` 最小调用间隔，适合距离、可见性或性能档位驱动的 View Update LOD：

```csharp
public sealed class ViewControllerComponent : Entity, IAwake, IUpdate, IEntityUpdateInterval
{
    public float UpdateInterval { get; set; }

    public void Awake()
    {
        UpdateInterval = 0f;
    }

    public void Update(float elapsedTime)
    {
        // elapsedTime 是该 Entity 距离上次真正 Update 的累计时间。
    }
}
```

当前契约：

- 未实现 `IEntityUpdateInterval` 或 `UpdateInterval == 0` 时，每次 `World.Update` 调用一次 Update。
- `UpdateInterval > 0` 时，Scheduler 按 Entity Handle 独立累计；未到间隔不调用，到期后只调用一次，不做多次追帧。
- 到期调用收到完整累计时间；调用前累计值归零，即使 Update 抛异常也不会在下一帧重复补算旧时间。
- `IsUpdateEnabled == false` 或更新要求未满足期间不累计，恢复后不会收到暂停期间的巨大 delta。
- 跨 Scene 移动会保留当前生命期的累计时间；销毁或对象池复用产生新 Handle 后从 0 开始。
- `UpdateInterval` 必须是有限且大于等于 0 的值；负数、NaN 或无穷值会阻止本次更新并记录错误。
- 读取 `UpdateInterval` 抛异常时会清零当前累计、记录 `Update interval error` 并只跳过该 Entity，其他 Entity 继续运行。
- `IEntityUpdateInterval` 只作用于 `IUpdate`，不能单独使用，也不影响 `IFixedUpdate`。

Entity 树只表达所有权和销毁范围，不继承更新通道或更新频率。例如 `ModelComponent` 可使用 `IFixedUpdate`，异步 `GameObjectLoadEntity` 下的 `ViewControllerComponent` 可使用 `IUpdate + IEntityUpdateInterval`。

## 更新要求

`RequireForUpdate` 只声明进入 `Start/FixedUpdate/Update` 的软条件。它不自动添加 Component、不改变 `Awake` 调用时间，也不表示 Entity 树结构非法。

```csharp
public sealed class TransformComponent : Entity, IAwake, IEntityReadyState
{
    public bool IsReady { get; private set; }

    public void Awake()
    {
        IsReady = true;
    }
}

[RequireForUpdate(typeof(TransformComponent))]
public sealed class MovementComponent : Entity, IAwake<MovementConfig>, IStart, IUpdate
{
    private MovementConfig _config;
    private TransformComponent _transform;

    public void Awake(MovementConfig config)
    {
        // 外部参数必须立即保存；这里不能假设 Transform 已经添加。
        _config = config;
    }

    public void Start()
    {
        // 第一次进入运行态时，Transform 保证存在且 Ready。
        _transform = Owner.GetComponent<TransformComponent>();
    }

    public void Update(float deltaTime)
    {
        // 正常移动逻辑。
    }
}
```

```csharp
var movement = unit.AddComponent<MovementComponent, MovementConfig>(config);
World.Instance.Update(0.016f); // Awake 已执行；缺少 Transform，不 Start/Update

unit.AddComponent<TransformComponent>();
World.Instance.Update(0.016f); // Transform Ready 后，Start 与首次 Update 同帧执行
```

当前契约：

- 只允许 Component Entity 使用 `RequireForUpdate`；Scene 会在注册前被拒绝，Child 会在创建时回滚并抛异常，已声明更新要求的 Component 也不能通过 `ReparentTo` 转成 Child。
- 只查询相同 owner 下的精确 Component 类型，不匹配接口、基类、Child、祖先或其他 Scene。
- 多个声明使用 AND，全部满足才可运行；不自动添加要求的 Component，也不计算传递要求。
- 要求的 Component 存在且未实现 `IEntityReadyState` 时默认 Ready；实现后要求 `IsReady == true`。
- `IEntityReadyState` 只表示该 Component 能否满足其他 Component 的更新要求，不自动阻止它自己的更新。
- 要求的 Component 缺失或 Not Ready 时不触发状态回调，只在每次调度前重新检查。Inspector 与 `ValidateEntities()` 会显示 Missing/NotReady 原因。
- `IsReady` getter 抛异常时只阻止声明要求的一方，Scheduler 记录错误，Validator 报告 `UpdateRequirementStateError`；其他 Entity 继续运行。
- 更新要求声明存在类型环时，Scheduler 注册会抛出包含完整路径的错误并回滚创建；Validator 使用 `UpdateRequirementCycle` 报告已存在的非法状态。
- `Start` 每个生命期只成功执行一次。后续 Ready 变为 false 会暂停 Update，恢复后继续 Update，不会再次 Start。

`RequireForUpdate` 不承诺要求目标实例热替换。`Start` 后应保持要求的 Component 实例结构稳定；可以动态改变同一实例的 `IsReady`，但不要移除后换成另一个实例并期望自动重新绑定。真实业务若需要热替换，应另行定义重新绑定生命周期。

添加顺序无关不等于任何时候都允许缺失。若某 Component 从创建完成起就必须存在，属于结构依赖，应由业务 Factory 在一组 Component 添加完成后统一校验；不要把 `RequireForUpdate` 当成结构验证或依赖注入。

Core 没有异步 `Awake`。`Awake(args)` 必须同步保存外部参数并建立最小本地不变量；异步加载由 Entity 自己启动，加载完成后更新 `IEntityReadyState.IsReady`。需要加载结果的一次性初始化放入 `IStart`，不要让 Scheduler 等待 `Task` 或把异步异常藏在创建事务中。

## Core 对象池

仅 `AddPooledChild` 和 `AddPooledComponent` 会从 Core 内部对象池取对象：

```csharp
var effect = unit.AddPooledChild<EffectEntity, string>("Burn");
effect.Destroy();

var reused = unit.AddPooledChild<EffectEntity, string>("Freeze");
```

池化实体重新取出时会获得新的 `InstanceId`、业务 `Id`（未指定时）和 `EntityHandle`。旧 `EntityRef<T>` 与旧 Handle 都不会误命中新生命。

`Started/StartFaulted` 保存于当前生命期的层级节点。池化对象重新取出时会创建新节点，因此会重新等待并执行一次 `Start`。

Scheduler 在调用 `Start` 前后会同时核对原始 Handle 与 `InstanceId`。旧生命若在 `Start` 中销毁并立即复用同一个池对象，新生命不会继承旧节点的 Started 状态，也不会参与已经开始的 pass。

池化类型必须把所有可变状态在 `Awake` 中完全重置，并在 `OnDestroy` 中释放外部资源。不能依赖字段初始化器在每次复用时重新执行。

## 两种“池”不要混淆

- Core 对象池：复用纯 C# Entity 对象，通过 `AddPooledChild/Component` 使用。
- Unity Framework `IInstancePool`：复用由资源系统加载的 GameObject，通过 `RentAsync`/`InstanceRef.Dispose` 使用。

二者生命周期、资源所有权和 API 完全不同。

[返回知识库首页](README.md)
