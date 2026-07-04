# GameEntity 使用指南

GameEntity 是一个纯 C# 的 Entity + 生命周期框架。核心库不依赖 Unity，Unity 侧只作为适配层：负责驱动 `World.Tick`、接管日志，并把 Entity 树投影成 GameObject，方便在 Unity Hierarchy 和 Inspector 里查看运行时数据。

## 目录

1. [安装和工程结构](#安装和工程结构)
2. [核心概念](#核心概念)
3. [基础用法](#基础用法)
4. [EntityHandle 和 EntityRef](#entityhandle-和-entityref)
5. [生命周期和更新](#生命周期和更新)
6. [组件依赖](#组件依赖)
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

- `World`：运行时世界，持有场景、层级、生命周期、调度、依赖、对象池等服务。
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

查询组件：

```csharp
StatsComponent stats = player.GetComponent<StatsComponent>();

if (player.TryGetComponent<StatsComponent>(out var currentStats))
{
    // 使用 currentStats。
}
```

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

不要通过 Unity Hierarchy 拖动 GameObject 来改变 Entity 父子关系。Unity 里的 GameObject 是调试视图，Entity 的真实关系以 core 层级为准。

### 6. 销毁

```csharp
monster.Destroy();
World.Instance.RemoveScene("Battle");
World.Instance.Dispose();
```

`Destroy()` 会销毁整棵子树，包括 child 和 component，并触发 `IDestroy.OnDestroy()`。

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

public interface IUpdate
{
    void Update(float time);
}

public interface IDestroy
{
    void OnDestroy();
}
```

`IAwake` 支持 0 到 4 个参数。Entity 被 `AddChild` 或 `AddComponent` 挂到树上后，框架会调用对应的 `Awake`。

宿主需要每帧驱动：

```csharp
World.Instance.Tick(deltaTime, unscaledDeltaTime);
```

Unity 项目中由 `GameEntityRunner` 自动调用，不需要业务脚本手动 Tick。

### 自定义更新策略

实现 `IHasUpdateStrategy` 可以控制 Update 次数：

```csharp
public sealed class EveryHalfSecondStrategy : IUpdateStrategy
{
    private float _elapsed;

    public int GetUpdateCount(Entity entity, float deltaTime, float unscaledDeltaTime, out float singleDeltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed < 0.5f)
        {
            singleDeltaTime = deltaTime;
            return 0;
        }

        _elapsed = 0f;
        singleDeltaTime = 0.5f;
        return 1;
    }
}

public sealed class SlowEntity : Entity, IAwake, IUpdate, IHasUpdateStrategy
{
    private readonly IUpdateStrategy _strategy = new EveryHalfSecondStrategy();

    public void Awake()
    {
    }

    public IUpdateStrategy GetUpdateStrategy()
    {
        return _strategy;
    }

    public void Update(float time)
    {
    }
}
```

内置组合策略：

```csharp
IUpdateStrategy all = new AllStrategy(strategyA, strategyB);
IUpdateStrategy any = new AnyStrategy(strategyA, strategyB);
```

## 组件依赖

组件可以声明依赖，依赖满足后才会进入有效更新。

```csharp
public sealed class MovementComponent : Entity, IAwake
{
    public void Awake()
    {
    }
}

[DependsOn(typeof(MovementComponent))]
public sealed class CombatComponent : DependentComponentBase, IAwake, IUpdate
{
    public void Awake()
    {
    }

    protected override void OnActivationChanged(bool isActive)
    {
        // isActive 为 true 表示依赖组件已经满足。
    }

    public void Update(float time)
    {
        // 依赖不满足时不会被调度。
    }
}

var unit = scene.AddChild<UnitEntity, string>("Player");
var combat = unit.AddComponent<CombatComponent>();

bool readyBefore = unit.AreDependenciesMet<CombatComponent>(); // false

unit.AddComponent<MovementComponent>();

bool readyAfter = unit.AreDependenciesMet<CombatComponent>(); // true
```

如果不想继承 `DependentComponentBase`，可以直接实现 `IDependentComponent`。

## 对象池

Entity 支持从内部对象池创建：

```csharp
var bullet = scene.AddPooledChild<BulletEntity>();
var stats = player.AddPooledComponent<StatsComponent, int, float>(100, 50f);
```

对象池由 `World` 管理。业务侧通常不直接访问 `ObjectPool`。调用 `Destroy()` 后，如果对象来自池，会回收到池中等待复用。

使用对象池时建议：

- 所有运行时状态都在 `Awake` 中重置。
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

Unity 场景中只需要一个入口组件：

```text
GameEntityRunner
```

推荐配置：

- `ViewRoot`：Entity 调试视图挂载根节点；为空时使用 runner 所在 GameObject。
- `AutoCreateViews`：自动为 Entity 创建 GameObject 视图。
- `DestroyViewsOnEntityDestroy`：Entity 销毁时同步销毁视图。
- `UseUnityLogger`：使用 Unity `Debug` 输出 GameEntity 日志。
- `OwnsWorldLifetime`：runner 销毁时是否 `World.Instance.Dispose()`。

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
