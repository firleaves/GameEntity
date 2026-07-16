# 核心快速开始

> 依据：`src/GameEntity/Core/World.cs`、`Entity.cs`、`Scene.cs`，以及 `apps/GameEntity.CoreTestApp/Program.cs`。

## 适用范围

Core 是无引擎依赖的 C# 实体与生命周期框架，目标框架为 `net8.0` 和 `netstandard2.1`。它适合服务器、控制台程序、测试工程，也可被 Unity 适配层驱动。

## 引用和构建

仓库内项目可直接添加项目引用。下面命令需在仓库根执行，消费项目路径按实际位置替换：

```bash
dotnet add "你的项目.csproj" reference "src/GameEntity/GameEntity.csproj"
```

构建与运行现有示例：

```bash
dotnet build "src/GameEntity/GameEntity.csproj"
dotnet run --project "apps/GameEntity.CoreTestApp/GameEntity.CoreTestApp.csproj"
```

## 最小可运行示例

```csharp
using GameEntity;

public sealed class BattleScene : Scene
{
    public BattleScene(string name) : base(name) { }
}

public sealed class UnitEntity : Entity, IAwake<string>, IUpdate, IDestroy
{
    public string Name { get; private set; }
    public int UpdateCount { get; private set; }

    protected override string ViewName => Name ?? base.ViewName;

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
        // 释放该实体拥有的外部资源。
    }
}

public sealed class StatsComponent : Entity, IAwake<int>
{
    public int Health { get; private set; }

    public void Awake(int health)
    {
        Health = health;
    }
}

const string sceneName = "Battle";
var scene = (BattleScene)World.Instance.AddScene(
    sceneName,
    new BattleScene(sceneName));

var player = scene.AddChild<UnitEntity, string>("Player");
var stats = player.AddComponent<StatsComponent, int>(100);

World.Instance.Update(0.016f);

Console.WriteLine($"{player.Name}: hp={stats.Health}, updates={player.UpdateCount}");

player.Destroy();
World.Instance.RemoveScene(sceneName);
World.Instance.Dispose(); // 仅整个运行时退出、测试隔离或完整重启时调用
```

## 必须遵守的顺序

```text
new Scene
  → World.AddScene
  → AddChild / AddComponent
  → 宿主循环调用 World.Update
  → Destroy / RemoveScene
  → World.Dispose
```

`new BattleScene(...)` 只构造对象，不会注册层级。未注册的 Scene 调用 `AddChild` 会抛异常；`AddScene` 的 key 也必须与 `Scene.Name` 完全一致。

`World` 是封闭构造的单例，只能通过 `World.Instance` 访问。一个进程内同一时刻只使用这一个 World；多个玩法、对局或 Unity Scene 必须作为同一 World 下的不同 GameEntity Scene，而不是各自创建 World。`AddScene` 会事务性执行 Scene `Awake` 和其中创建的初始子树；失败时 Scene 与临时节点全部回滚。Scene 派生类型只能走 `AddScene`，不能通过 `AddChild/AddComponent` 嵌入另一棵树；声明了 `ChildOf` 的 Scene 类型会被拒绝注册为 SceneRoot。

`World.Dispose()` 结束整个 GameEntity 运行时会话。只有应用退出、测试隔离或完整重启才调用它；之后再次访问 `World.Instance` 表示顺序开启一个全新会话，不允许保留并继续使用旧 World，也不允许新旧 World 同时工作。

默认复用已有的合适 Scene。只有一组 Entity 是 World 下的顶层运行时作用域，并且可以在其他 Scene 继续存在时独立整体销毁，才新建 Scene。完整规则见[Scene 的创建与边界规范](02-scene-boundaries.md)。

## 初始化与开始运行

`AddChild/AddComponent` 会先挂接节点，再立即调用匹配的 `Awake(args)`。外部参数必须在 `Awake` 保存；如果还需要等待其他 Component 或 Ready 状态，实现 `IStart` 做第二阶段初始化。`IStart.Start()` 在第一次满足更新状态与更新要求的 `World.Update` 中执行一次，并可在同一次 Update pass 紧接着进入 `Update`。

`Awake` 不受 `IEntityUpdateState` 或 `RequireForUpdate` 阻止，也不应读取尚未完成组合的 Component。创建期间 `Awake` 抛异常，或者 Entity/Scene 在 `Awake`、`RegisterSystem` 中销毁自身，都会回滚并向调用方抛出；创建 API 不会返回已销毁或已回池对象。

## Child 与 Component 怎么选

- 使用 Child：对象有独立业务身份、可拥有子树、可能移动到其他 owner。
- 使用 Component：对象是 owner 的一种能力，且同一个 owner 上同类型只能存在一个。
- 两者都继承 `Entity`，都可继续拥有 Child；Component 也能实现生命周期接口。

默认不要把普通状态和集合记录做成 Entity。只有需要独立生命周期、调度、迁移、运行时引用或子树时才使用 Child；完整判定规则见[Entity 与普通数据的建模边界](02-entity-vs-data.md)。

若某个类型只能作为特定 owner 的 Child，可声明 `[ChildOf(typeof(OwnerType))]`。Core 会在 `AddChild`、池化创建和 `ReparentTo` 时执行约束，并禁止该类型通过 `AddComponent` 挂接。

```csharp
var pet = player.AddChild<UnitEntity, string>("Pet");
var playerStats = player.AddComponent<StatsComponent, int>(100);

StatsComponent found = player.GetComponent<StatsComponent>();
bool hasStats = player.TryGetComponent(out StatsComponent current);
player.RemoveComponent<StatsComponent>();
```

上述 Component API 都按精确运行时类型匹配。`GetComponent<BaseComponent>()` 不会隐式返回某个派生 Component；需要多态查找时显式遍历 `Components`/`GetAllComponents()` 并处理多个匹配项。

## 自定义业务 ID

普通 `AddChild` 自动分配 `Id`。需要用领域 ID 查询时，使用 `AddChildWithId`：

```csharp
var unit = scene.AddChildWithId<UnitEntity, string>(10001, "Player");

UnitEntity sameUnit = scene.GetChild<UnitEntity>(10001);
bool exists = scene.TryGetChild(10001, out UnitEntity result);
```

`Id` 是业务实体 ID；`InstanceId` 标识某次对象生命；`Handle.NodeId` 标识本次 World 中的层级节点。三者用途不同，不应互换。

## 宿主循环

非 Unity 宿主应在自己的循环中分别驱动固定模拟和普通帧更新：

```csharp
World.Instance.FixedUpdate(1f / 30f); // 按宿主累计结果调用零到多次
World.Instance.Update(deltaTime);     // 每个宿主帧调用一次
```

`IFixedUpdate` 接收宿主传给 `World.FixedUpdate` 的固定步长；`IUpdate` 接收宿主传给 `World.Update` 的游戏帧时间。Core 不额外提供 unscaled delta；缩放语义由宿主决定。View 需要动态降频时实现 `IEntityUpdateInterval`，固定 Model 更新和 View Update LOD 的完整规范见[调度、更新要求与池化](03-scheduling-dependency-pooling.md)。`IStart` 在当前 Entity 第一次进入其所属更新通道前执行一次，不受 Update 降频间隔延迟。

[返回知识库首页](README.md)
