# GameEntity 使用指南

GameEntity 是一个纯 C# 的 Entity + 生命周期框架。核心库不依赖 Unity，Unity 侧只作为适配层：负责驱动 `World.Update`、接管日志，并把 Entity 树投影成 GameObject，方便在 Unity Hierarchy 和 Inspector 里查看运行时数据。

模块化、面向 AI 的最新规范以 [`docs/knowledge-base/README.md`](docs/knowledge-base/README.md) 为入口；本页示例也必须与该知识库和当前源码保持一致。

## 目录

1. [安装和工程结构](#安装和工程结构)
2. [核心概念](#核心概念)
3. [基础用法](#基础用法)
4. [EntityHandle 和 EntityRef](#entityhandle-和-entityref)
5. [生命周期和更新](#生命周期和更新)
6. [更新要求](#更新要求)
7. [对象池](#对象池)
8. [诊断和日志](#诊断和日志)
9. [Unity 用法](#unity-用法)
10. [推荐实践](#推荐实践)

## 安装和工程结构

核心库位置：

```text
src/GameEntity
```

目标框架：

```text
net8.0
netstandard2.1
```

Unity 包位置：

```text
unity/Packages/com.firleaves.gameentity.unity
```

Unity Package Manager Git 地址：

```text
https://github.com/firleaves/GameEntity.git?path=unity/Packages/com.firleaves.gameentity.unity
```

注意：`v0` 是旧 main 基线，保留的是迁移到当前 core hierarchy + Unity UPM 结构之前的仓库状态；新 Unity 包应使用当前 main 或后续正式版本 tag。

## 核心概念

- `World`：运行时世界，持有场景、层级、生命周期、调度和对象池等服务。
- `Scene`：场景根节点。只有注册进 `World` 后，才算正式进入 Entity 树。
- `Entity`：所有业务对象和组件的基类。
- Child Entity：通过 `AddChild` 挂到某个 Entity 下，表达对象树关系。
- Component Entity：通过 `AddComponent` 挂到某个 Entity 上，表达组合能力。
- `EntityHandle`：运行时节点句柄，用于安全解析 Entity。
- Unity GameObject：只做可视化投影，不是业务数据源。

当前层级关系统一由 core 内部的 `EntityHierarchy` 管理。业务侧不要直接修改 Unity GameObject 父子关系来改变 Entity 关系，应使用 `AddChild`、`AddComponent`、`ReparentTo`、`Destroy` 等 core API。

## 基础用法

### 1. 创建并注册 Scene

```csharp
using GameEntity;

public sealed class BattleScene : Scene
{
    public BattleScene(string name) : base(name)
    {
    }

    public override void Awake()
    {
        // Scene 被 World.AddScene 注册后会调用 Awake。
    }
}

Scene scene = World.Instance.AddScene("Battle", new BattleScene("Battle"));
```

`Scene` 构造后还不能挂载子节点。必须先调用：

```csharp
World.Instance.AddScene("Battle", scene);
```

再调用：

```csharp
scene.AddChild<UnitEntity, string>("Player");
```

如果 `scene.Name` 和 `AddScene` 传入的 key 不一致，会抛出异常。

`World` 是封闭构造的单例，只通过 `World.Instance` 使用。GameEntity 固定采用单 World 架构：一个进程内同一时刻只有一个有效 World；多个玩法、对局或 Unity Scene 必须作为它下面的多个 GameEntity Scene，不得各自创建、缓存或切换 World。`AddScene` 会事务性执行 Scene `Awake` 及其中创建的初始子树；全部成功后才向树观察者发布，任一步失败或 Scene 在 `Awake/RegisterSystem` 中自毁都会销毁临时子树并从 World 移除。Scene 派生类型只能通过 `AddScene` 成为 SceneRoot，不能作为 Child/Component；声明 `ChildOf` 的 Scene 类型也不能注册为根。

### 2. 创建 Child Entity

```csharp
public sealed class UnitEntity : Entity, IAwake<string>, IUpdate, IDestroy
{
    public string Name { get; private set; }
    public int UpdateCount { get; private set; }

    public void Awake(string name)
    {
        Name = name;
    }

    public void Update(float deltaTime)
    {
        UpdateCount++;
    }

    public void OnDestroy()
    {
        // 清理业务资源。
    }
}

var scene = World.Instance.GetScene("Battle");
var player = scene.AddChild<UnitEntity, string>("Player");
var monster = scene.AddChild<UnitEntity, string>("Monster");
```

`AddChild` 支持 0 到 4 个 Awake 参数：

```csharp
entity.AddChild<MyEntity>();
entity.AddChild<MyEntity, int>(100);
entity.AddChild<MyEntity, int, string>(100, "name");
```

需要指定业务 id 时使用：

```csharp
var unit = scene.AddChildWithId<UnitEntity, string>(10001, "Player");
```

### 3. 添加 Component Entity

```csharp
public sealed class StatsComponent : Entity, IAwake<int, float>
{
    public int Health { get; private set; }
    public float Energy { get; private set; }

    public void Awake(int health, float energy)
    {
        Health = health;
        Energy = energy;
    }
}

StatsComponent stats = player.AddComponent<StatsComponent, int, float>(100, 50f);
```

同一个 owner 上，同类型 component 只能有一个。重复添加会抛出异常。

只允许作为 Child 或只允许挂在特定 owner 下的类型可声明：

```csharp
[ChildOf(typeof(UnitEntity))]
public sealed class EquipmentEntity : Entity, IAwake
{
    public void Awake() { }
}
```

该约束会用于 `AddChild`、池化创建和 `ReparentTo`，并禁止 `AddComponent<EquipmentEntity>()`。Core 不提供 `ComponentOf`。

查询组件：

```csharp
StatsComponent stats = player.GetComponent<StatsComponent>();

if (player.TryGetComponent<StatsComponent>(out var currentStats))
{
    // 使用 currentStats。
}
```

`GetComponent<T>`、`GetComponent(Type)`、`TryGetComponent`、`ContainsComponent` 和 `RemoveComponent` 都按精确运行时类型匹配。查询基类不会隐式返回某个派生 Component；确实需要多态查找时，显式遍历 `Components` 并处理多个匹配结果。

移除组件：

```csharp
player.RemoveComponent<StatsComponent>();
```

### 4. 查询父子和 Scene

```csharp
Entity parent = player.Parent;
Scene root = player.GetSceneRoot();

int childCount = scene.ChildrenCount();
int componentCount = player.ComponentsCount();

foreach (Entity child in scene.Children)
{
    // Children 是只读快照。
}

foreach (Entity component in player.Components)
{
    // Components 是只读快照。
}
```

按业务 id 查询 child：

```csharp
UnitEntity unit = scene.GetChild<UnitEntity>(10001);

if (scene.TryGetChild<UnitEntity>(10001, out var found))
{
    // 使用 found。
}
```

### 5. 重新挂接 Entity

```csharp
monster.ReparentTo(player);
```

`ReparentTo` 会移动整棵子树。如果跨 Scene 移动，core 会同步迁移子树内所有节点的 Scene 分区和更新调度归属。

只有当前层级中仍存活、Handle 有效的 Entity 可以重新挂接。不能用 `new Entity().ReparentTo(owner)` 绕过创建流程，也不能重新挂接已销毁对象。更新 pass 开始后发生的迁移不会让 Entity 在目标 Scene 再执行一次。

不要通过 Unity Hierarchy 拖动 GameObject 来改变 Entity 父子关系。Unity 里的 GameObject 是调试视图，Entity 的真实关系以 core 层级为准。

### 6. 销毁

```csharp
monster.Destroy();
World.Instance.RemoveScene("Battle");
World.Instance.Dispose(); // 仅整个运行时退出、测试隔离或完整重启
```

`Destroy()` 是不可重写的模板入口，会销毁整棵子树，包括 child 和 component，并触发 `IDestroy.OnDestroy()`。业务释放逻辑只实现 `OnDestroy`，不要隐藏同名 `Destroy` 方法。

`World.Instance.Dispose()` 表示整个 GameEntity 运行时退出或完整重启，不是普通 Scene 切换 API。Dispose 开始后，保存的旧 World 引用对公开实例 API 抛出 `ObjectDisposedException`；重新访问 `World.Instance` 只表示顺序开启下一次会话，新旧 World 不得并存。

## EntityHandle 和 EntityRef

`EntityHandle` 是运行时节点 id：

```csharp
EntityHandle handle = player.Handle;
```

它适合做短期运行时定位，不适合作为存档 id。销毁后的 handle 不会解析到新对象：

```csharp
if (World.Instance.TryResolve<UnitEntity>(handle, out var resolved))
{
    // resolved 仍然存活。
}
```

`EntityRef<T>` 是更适合业务侧保存引用的轻量包装。它同时校验 `InstanceId` 和 `EntityHandle`，可以避免对象池复用导致旧引用误指向新对象：

```csharp
EntityRef<UnitEntity> target = player;

if (target.TryGet(out var aliveTarget))
{
    aliveTarget.Destroy();
}
```

常用属性：

```csharp
bool alive = target.IsAlive;
UnitEntity value = target.ValueOrNull;
EntityHandle handle = target.Handle;
```

## 生命周期和更新

生命周期接口：

```csharp
public interface IAwake
{
    void Awake();
}

public interface IAwake<T>
{
    void Awake(T p1);
}

public interface IStart
{
    void Start();
}

public interface IFixedUpdate
{
    void FixedUpdate(float fixedDeltaTime);
}

public interface IUpdate
{
    void Update(float deltaTime);
}

public interface IDestroy
{
    void OnDestroy();
}
```

`IAwake` 支持 0 到 4 个参数。Entity 被 `AddChild` 或 `AddComponent` 挂到树上后，框架立即调用对应的 `Awake(args)`，用于保存外部参数和重置本地状态。需要等待更新要求（例如其他 Component Ready）的第二阶段初始化实现 `IStart`；`Start()` 第一次满足条件时执行一次，并可在同一次更新 pass 进入 `Update`。

`Awake` 不会被 `IEntityUpdateState` 或 `RequireForUpdate` 延迟，也不能假设其他 Component 已经添加。`Awake` 抛异常，或者 Entity/Scene 在 `Awake`、`RegisterSystem` 中销毁自身时，创建流程会回滚并抛出，不会返回已销毁或已回池对象；`Start` 抛异常时当前生命期标记为 `StartFaulted`，不会自动重试或 Update。

宿主分别驱动固定模拟和普通帧更新：

```csharp
World.Instance.FixedUpdate(1f / 30f); // Model 固定模拟
World.Instance.Update(deltaTime);     // View 与普通系统帧更新
```

Unity 项目中由 `GameEntityRunner` 自动累计 `Time.deltaTime`，按 `FixedUpdatesPerSecond` 调用零到多次 `World.FixedUpdate`，随后调用一次 `World.Update`。默认固定频率为 30Hz，并通过 `MaxFixedStepsPerFrame` 限制单帧追赶次数；业务脚本不需要手动驱动 World。

### 固定更新与 View 更新降频

Model 需要保持统一模拟时间线时实现 `IFixedUpdate`。同一次 `World.FixedUpdate` 中所有 Model Entity 使用相同的 `fixedDeltaTime`，不要让每个 Model Entity 独立累计固定步长。

```csharp
public sealed class MovementModelComponent : Entity, IAwake, IFixedUpdate
{
    public void Awake()
    {
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        // Model 模拟。
    }
}
```

View 需要根据距离、可见性或性能档位降低普通 Update 频率时，实现 `IEntityUpdateInterval`：

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
        // elapsedTime 是距离上次真正执行 Update 的累计时间。
    }
}
```

`UpdateInterval == 0` 表示每次 `World.Update` 都更新；大于 0 时，Scheduler 为每个 Entity Handle 独立累计时间，到期后最多调用一次 Update，并传入完整累计时间。不会通过一帧执行多次 Update 来追赶。负数、NaN 和无穷值无效，该 Entity 本次不更新并记录错误。

Entity 树只表达所有权和销毁范围，不继承更新通道或更新频率。常见结构是 Model Component 实现 `IFixedUpdate`，异步 `GameObjectLoadEntity` 加载完成后创建的 View Controller 实现 `IUpdate`，并按需实现 `IEntityUpdateInterval`。

Core 不提供 Scene 级暂停、树级 Enable 传播、`IUnscaledUpdate` 或 `ILateUpdate`。全局减速/加速由宿主缩放传给 World 的 delta；真正的全局暂停应停止调用对应的 World 更新入口。局部暂停使用业务暂停域和 `IEntityUpdateState`，`IEntityUpdateInterval` 只做调用频率 LOD，不代表慢动作。

## 更新要求

Component 可以声明更新要求，要求满足后才会进入 `Start/FixedUpdate/Update`。它是调度条件，不是自动添加、依赖注入或结构验证。

```csharp
public sealed class MovementComponent : Entity, IAwake, IEntityReadyState
{
    public bool IsReady { get; private set; }

    public void Awake()
    {
        IsReady = true;
    }
}

[RequireForUpdate(typeof(MovementComponent))]
public sealed class CombatComponent : Entity, IAwake, IStart, IUpdate
{
    public void Awake()
    {
        // 立即保存创建参数；不能依赖 Movement 已存在。
    }

    public void Start()
    {
        // 此时 Movement 保证存在并且 Ready。
    }

    public void Update(float time)
    {
        // 更新要求不满足时不会被调度。
    }
}

var unit = scene.AddChild<UnitEntity, string>("Player");
var combat = unit.AddComponent<CombatComponent>();

World.Instance.Update(0.016f); // 缺少 Movement，不 Start/Update

unit.AddComponent<MovementComponent>();

World.Instance.Update(0.016f); // Start 与首次 Update 可同帧执行
```

当前只解析相同 owner 下的精确 Component 类型，多个要求使用 AND。要求的 Component 不存在或 `IEntityReadyState.IsReady == false` 时会在每次调度前重新检查，不发送状态变化通知。`Start` 成功后不会因 Ready 暂停和恢复而再次执行；要求的 Component 实例应在 Start 后保持稳定。

## 对象池

Entity 支持从内部对象池创建：

```csharp
var bullet = scene.AddPooledChild<BulletEntity>();
var stats = player.AddPooledComponent<StatsComponent, int, float>(100, 50f);
```

对象池由 `World` 管理。业务侧通常不直接访问 `ObjectPool`。调用 `Destroy()` 后，如果对象来自池，会回收到池中等待复用。

使用对象池时建议：

- 所有运行时状态都在 `Awake` 中重置。
- `Started/StartFaulted` 随新 Entity 节点重置，池化对象每个生命期会重新执行一次 `Start`。
- 不要在外部长期保存裸 `Entity` 引用，优先使用 `EntityRef<T>`。
- 不要把 `EntityHandle` 当作业务存档 id。

## 诊断和日志

Core 提供层级快照和校验：

```csharp
EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();
EntityValidationResult result = World.Instance.ValidateEntities();

if (!result.IsValid)
{
    foreach (EntityValidationIssue issue in result.Issues)
    {
        Console.WriteLine(issue.Message);
    }
}
```

日志默认静默：

```csharp
Log.Logger = new ConsoleLogger();
Log.SetLogLevel(debug: true, info: true, warning: true, error: true);
```

Unity 中 `GameEntityRunner.UseUnityLogger = true` 时，会自动设置 Unity logger。

## Unity 用法

Unity 包名称：

```text
GameEntity for Unity
```

代码命名空间：

```csharp
using GameEntity;
using GameEntity.Unity;
```

Unity 场景中只需要一个入口组件，它驱动唯一的 `World.Instance`：

```text
GameEntityRunner
```

推荐配置：

- `ViewRoot`：Entity 调试视图挂载根节点；为空时使用 runner 所在 GameObject。
- `AutoCreateViews`：自动为 Entity 创建 GameObject 视图。
- `DestroyViewsOnEntityDestroy`：Entity 销毁时同步销毁视图。
- `UseUnityLogger`：使用 Unity `Debug` 输出 GameEntity 日志。
- `OwnsWorldLifetime`：runner 销毁时是否 `World.Instance.Dispose()`。

Runner 与业务 Scene 没有强制的创建先后。Runner 先启动时会观察后续节点；业务先 `AddScene/AddChild` 时，Runner 会通过 `World.ObserveEntities(..., replayExisting: true)` 按父到子回放已有树。区别是 Runner 出现前没有 Unity 自动更新驱动，因此已有 Entity 只会完成 `Awake`，不会自动进入 `IStart/IFixedUpdate/IUpdate`。

示例：

```csharp
public sealed class GameStarter : MonoBehaviour
{
    private Scene _scene;

    private void Start()
    {
        _scene = World.Instance.AddScene("Battle", new BattleScene("Battle"));

        var player = _scene.AddChild<UnitEntity, string>("Player");
        player.AddComponent<StatsComponent, int, float>(100, 50f);
    }

    private void OnDestroy()
    {
        if (_scene != null && !_scene.IsDestroyed)
        {
            _scene.Destroy();
        }
    }
}
```

导入 sample 后可运行：

```text
Samples/GameEntity for Unity/0.1.0/GameEntity Demo/GameEntityDemo.unity
```

或直接查看包内源示例：

```text
unity/Packages/com.firleaves.gameentity.unity/Samples~/GameEntityDemo
```

运行 demo 后，Hierarchy 中会出现 Entity 树投影。3 秒后示例会调用 `Entity.ReparentTo`，可以看到 Monster 从 Scene 下移动到 Player 下。

## 推荐实践

### 1. 用 Child 表达对象树，用 Component 表达能力

```csharp
var player = scene.AddChild<UnitEntity, string>("Player");
player.AddComponent<StatsComponent, int, float>(100, 50f);
player.AddComponent<InventoryComponent, int>(20);
```

不要为了复用逻辑写很深的继承树。优先拆成小组件挂载。

### 2. Scene 只做根和编排

`Scene` 负责创建初始实体、承载场景级根节点。具体业务逻辑放到 Entity 或 Component 中。

### 3. 生命周期状态在 Awake 重置

尤其是对象池实体，所有可变状态必须在 `Awake` 中重置：

```csharp
public sealed class BulletEntity : Entity, IAwake<float>, IUpdate
{
    private float _speed;
    private float _life;

    public void Awake(float speed)
    {
        _speed = speed;
        _life = 0f;
    }

    public void Update(float time)
    {
        _life += time;
        if (_life > 3f)
        {
            Destroy();
        }
    }
}
```

### 4. 引用跨帧保存用 EntityRef

```csharp
private EntityRef<UnitEntity> _target;

public void SetTarget(UnitEntity target)
{
    _target = target;
}

public void Update(float time)
{
    if (_target.TryGet(out var target))
    {
        // target 仍然存活。
    }
}
```

### 5. Unity 只看数据，不改数据

Unity 里的 GameObject 层级是 Entity 树的投影，方便观察数据。业务侧改变父子关系时，始终调用 core API：

```csharp
child.ReparentTo(newParent);
```

不要通过拖动 GameObject 或直接设置 `transform.parent` 来表达 Entity 关系。
