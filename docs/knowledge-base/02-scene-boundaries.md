# Scene 的创建与边界规范

> 本规范用于回答：什么时候应该创建新的 GameEntity Scene，什么时候应该继续使用现有 Scene 中的 Child Entity、Component Entity 或普通数据。

## 核心原则

> 默认不要新建 Scene。只有当一组 Entity 构成独立的顶层运行时作用域，并且需要整体创建、整体销毁时，才建立新的 Scene。

> GameEntity 固定采用单 World 架构。需要隔离多个玩法、对局或顶层领域时，在同一个 `World.Instance` 下建立多个 Scene；不得用多个 World 代替 Scene 边界。

Scene 不是普通分组节点，也不等同于 Unity Scene。它在 GameEntity 中表达：

- 顶层所有权根。
- 独立生命周期边界。
- 整棵实体树的原子清理边界。
- 内部更新调度分区。
- 诊断快照中的 Scene 分区。
- 一个顶层业务会话或实例。

Scene 数量应该与活跃的顶层会话或运行时实例数量相关，不应该与 Entity 数量、代码模块数量、UI 页面数量或地图数据数量相关。

## 当前 Core 的真实能力

Scene 通过 `World.Instance.AddScene` 注册后形成独立的 `SceneRoot`：

```text
World
├── GlobalServicesScene
├── LobbyScene
├── BattleScene:10001
└── BattleScene:10002
```

当前实现保证：

- Scene 在 World 中使用唯一名称注册。
- `AddScene` 的 key 必须与 `Scene.Name` 完全一致。
- Scene 派生类型只能作为 SceneRoot，不能通过 `AddChild/AddComponent` 嵌入其他 Scene；声明 `ChildOf` 的 Scene 类型不能注册为 SceneRoot。
- 每个 Scene 是一棵 Entity 树的根，不存在业务 Parent。
- 每个 Scene 有独立的内部更新调度桶。
- `RemoveScene` 或 `Scene.Destroy()` 会级联销毁整棵子树。
- 跨 Scene `ReparentTo` 会迁移整个子树的 Scene 和调度归属。
- `World.Dispose()` 会清理全部 Scene。

`World.AddScene` 是事务性创建入口。Core 会先把 Scene 临时加入 World，使 `Scene.Awake()` 能查询自身并创建初始子树；只有 `Awake`、子树生命周期和调度注册全部成功后，才按父到子顺序向观察者发布。任一步抛异常，或 Scene 在 `Awake/RegisterSystem` 中销毁自身，都会移除 Scene、销毁临时子树、失效 Handle，并向调用者抛出，不会返回或留下可见的半初始化 Scene。

Scene Root 本身不参与 Scheduler。Scene 类型实现 `IStart`、`IFixedUpdate`、`IUpdate` 或声明 `RequireForUpdate` 会在 `AddScene` 时被拒绝。需要 Scene 范围的运行逻辑时，在 Scene 下创建明确的 System Child/Component Entity，并让它实现相应生命周期或更新接口。

当前 Core 没有公开的“只更新某个 Scene”或“暂停某个 Scene”API。所有已注册 Scene 都由 World 的更新入口统一驱动。因此不能仅以“希望单独暂停”为理由创建新 Scene。

## AI 必须遵守的默认策略

AI 在创建 Scene 类型或生成 `World.AddScene` 调用前，必须：

1. 始终复用唯一的 `World.Instance`，不得提出第二个 World 或每个对局一个 World。
2. 默认把新对象放入现有的合适 Scene。
3. 先判断它是否只是现有作用域中的 Child、Component 或普通数据。
4. 只有对象集合没有合理业务 owner，并且需要独立整体销毁时，才创建 Scene。
5. 不得按代码模块、对象类型、UI 页面或 Inspector 分组创建 Scene。
6. 不得把 Unity Scene 与 GameEntity Scene 自动建立一一对应。
7. 必须说明 Scene 的创建者、销毁者、唯一名称和原子清理范围。

AI 的方案应明确写出依据，例如：

```text
选择：BattleScene:10001
原因：该战斗是 World 下的顶层会话，可以在 GlobalServicesScene 和其他战斗继续存活时独立销毁，战斗内全部 Entity 应原子清理。
```

或者：

```text
选择：BattleScene 下的 DungeonAreaEntity，不创建新 Scene
原因：该区域没有独立顶层生命周期，始终随当前 BattleScene 创建和销毁。
```

## 一、什么时候应该新建 Scene

### 独立的整体创建和销毁

这是最重要的条件。例如一场独立战斗：

```text
BattleScene:10001
├── BattleContextEntity
├── PlayerEntity
├── MonsterEntity
├── ProjectileSystemEntity
└── EffectSystemEntity
```

战斗结束时，所有单位、投射物、效果、任务和战斗级服务都应一起销毁：

```csharp
World.Instance.RemoveScene("Battle:10001");
```

它不应影响常驻基础服务、Lobby 或其他战斗实例，因此适合独立 Scene。

### World 下的顶层运行时领域

一组对象没有合理的业务 Entity owner，直接属于 World 时，可以形成 Scene：

```text
GlobalServicesScene
LobbyScene
BattleScene
EditorPreviewScene
```

`GlobalServicesScene` 是基础服务根，`BattleScene` 是玩法会话根。二者不存在合理的父子所有权，适合并列注册。

### 多个相互隔离的运行时实例

服务器同时运行多场对局时，仍然只使用一个 `World.Instance`，推荐在这个 World 下为每场对局建立一个 Scene：

```text
Match:10001
Match:10002
Match:10003
```

每场对局拥有独立 Entity 树、销毁边界、调度分区、快照分区和对局级服务。结束一场对局只移除对应 Scene。

### 独立加载和卸载边界

某个作用域可以完整卸载，而其他运行时作用域必须继续存在时，可以拆为 Scene：

```text
GlobalServicesScene   始终存在
LobbyScene       进入战斗后销毁
BattleScene      战斗期间存在
```

若切换只是状态变化，或者大部分对象必须跨阶段保留，则不应仅因“进入了新流程”而创建 Scene。

### 明确的诊断和故障隔离

独立 Scene 可以明确回答：某个 Entity 属于哪场战斗、哪个实例发生泄漏、移除会话后是否仍有节点残留。

诊断价值只能作为辅助条件，不能在没有独立生命周期的情况下单独证明应创建 Scene。

## 二、什么时候不应该新建 Scene

### 仅为了分类或整理 Hierarchy

错误：

```text
PlayerScene
MonsterScene
ProjectileScene
UIScene
AudioScene
```

如果这些对象都属于同一场战斗，应放在一个 `BattleScene` 下，通过 Child、Component 或普通容器组织。

> Scene 按生命周期和运行时实例划分，不按代码模块或对象类型划分。

### 每个业务对象一个 Scene

玩家、怪物和投射物在战斗中都有明确 owner，应是 BattleScene 的 Child Entity。Scene 是粗粒度根边界，不是另一种 Entity 创建方式。

### 每个 UI 页面一个 Scene

登录、背包、设置等页面通常由 UI 系统管理：

```text
GlobalServicesScene
└── UISystemEntity
    ├── LoginUIEntity
    ├── InventoryUIEntity
    └── SettingsUIEntity
```

Unity 中存在 Canvas、页面切换或独立 Prefab，不构成创建 GameEntity Scene 的理由。

### 每个地图区块一个 Scene

地图 Chunk、AOI Cell、Terrain Tile 通常使用普通数据、Child Entity 或 Streaming System 管理。大世界中的海量区块逐个 Scene 化会让 Scene 数量随空间数据增长，偏离顶层作用域语义。

只有区块确实是可以独立销毁、没有地图业务 owner 的顶层运行时实例时，才考虑 Scene。

### 每个流程状态一个 Scene

一场战斗中的 `Preparing`、`Fighting`、`Settlement` 和 `Finished` 是同一会话的状态，不是四个 Scene。应使用状态数据、Procedure、状态机、Component，必要时使用阶段 Child Entity。

只有阶段切换意味着整棵运行时对象树都应被替换时，才考虑独立 Scene。

### 仅为了暂停更新

当前 Core 没有逐 Scene 暂停接口。需要暂停单个 Entity 的 `Start/FixedUpdate/Update` 时，使用 `IEntityUpdateState.IsUpdateEnabled`；暂停范围和原因由战斗状态或 Scene 内的调度服务决定。不要用 `IEntityReadyState.IsReady` 表示暂停，因为 Ready 只表达能否满足更新要求。

`IsUpdateEnabled` 不会从 Scene 或 owner 自动传播到后代。若业务确实需要“暂停一场战斗中的所有模拟”，应由 `BattleClock`、`PauseState` 或同类业务服务定义暂停域，并让域内需要停止的 Entity 显式读取它。Core 暂不提供 Scene 时间缩放，是因为 Scene 分区目前只负责生命周期、索引和调度存储，并不是独立时钟。

## Scene 与 Child Entity 的边界

> 有明确业务 owner 的对象用 Child Entity；没有合理业务 owner、直接属于 World 的顶层运行时作用域用 Scene。

独立副本会话：

```text
World
├── GlobalServicesScene
└── DungeonScene:20001
    ├── DungeonContextEntity
    ├── PlayerEntity
    └── MonsterEntity
```

副本只是当前战斗中的一个区域：

```text
BattleScene
└── WorldMapEntity
    └── DungeonAreaEntity
```

后者始终随 BattleScene 存活，没有独立顶层所有权，因此应该是 Child Entity。

## Scene 与流程状态的边界

登录、大厅、战斗存在两种有效设计。

使用独立 Scene：

```text
GlobalServicesScene
LobbyScene
BattleScene
```

适用于大厅和战斗 Entity 树基本不同，切换时旧树应整体销毁，资源和服务边界清晰。

使用长期 Scene 和流程状态：

```text
GameScene
├── ProcedureComponent
├── PlayerSessionEntity
├── LobbyModuleEntity
└── BattleModuleEntity
```

适用于玩家会话贯穿多个流程、大量对象需要持续存在，切换只是状态变化而不是运行时实例替换。

不能机械规定登录、大厅和战斗必须各一个 Scene。应回答：

> 切换时是否应该整体销毁这棵 Entity 树？

## GameEntity Scene 与 Unity Scene

两者不能默认一一对应：

- 一个 Unity Bootstrap Scene 可以同时承载 GlobalServicesScene、LobbyScene 和 DebugPreviewScene。
- Unity 可以 Additive 加载地图、灯光和 UI，但业务仍属于一个 BattleScene。
- GlobalServicesScene 可以在 Unity Scene 切换期间继续存活。

规范是：

> Unity Scene 管 Unity Object 和资源场景；GameEntity Scene 管业务 Entity 的顶层生命周期。只有两者生命周期确实一致时才建立映射。

Unity Scene 的加载与卸载不应隐式决定 GameEntity Scene 的生命，除非生命周期协调者明确建立了这种所有权关系。

## Scene 创建准入条件

新 Scene 至少应满足以下三个核心条件中的前两个：

1. **顶层所有权**：没有合理的 Entity owner，直接属于 World。
2. **独立生命周期**：可以在其他 Scene 继续存在时整体创建和销毁。
3. **原子清理**：其中全部 Entity 应在一个明确时刻一起销毁。

以下只能作为辅助理由，不能单独证明应创建 Scene：

- 希望 Hierarchy 更整齐。
- 希望按代码模块分类。
- 希望获得独立名称。
- 希望未来可以扩展。
- 希望单独显示调试信息。
- 对象数量很多。

## 命名规范

单例型作用域使用稳定名称：

```text
GlobalServices
PlayerSession
Lobby
```

多实例会话使用“类型 + 领域 ID”：

```text
Battle:10001
Dungeon:20008
Match:EU-10086
Preview:Weapon-3001
```

使用同一个变量同时构造 Scene 和注册，避免 key 与 `Scene.Name` 不一致：

```csharp
string sceneName = $"Battle:{battleId}";

var scene = (BattleScene)World.Instance.AddScene(
    sceneName,
    new BattleScene(sceneName));
```

Scene 名称应由统一创建器生成，不要在业务代码各处拼接。

## 创建和销毁所有权

每类 Scene 应有唯一生命周期协调者，例如 `BattleSceneFactory`、`GameFlowCoordinator` 或 `ServerMatchHost`。协调者负责：

- 生成唯一名称。
- 调用 `World.AddScene`。
- 初始化顶层上下文。
- 处理切换或结束。
- 调用 `World.RemoveScene`。
- 检查销毁后的引用和层级。

普通 Entity 不应在深层业务逻辑中随意创建或移除顶层 Scene，否则会形成隐式生命周期耦合。

销毁后的 Scene 实例不得重新注册，应创建新的 Scene 对象：

```csharp
World.Instance.RemoveScene(sceneName);

var nextScene = (BattleScene)World.Instance.AddScene(
    nextSceneName,
    new BattleScene(nextSceneName));
```

## 跨 Scene 引用与迁移

被引用 Scene 可以独立销毁，因此跨 Scene 运行时引用必须允许失效：

```csharp
EntityRef<PlayerEntity> playerRef = player;

if (playerRef.TryGet(out PlayerEntity alivePlayer))
{
    // 使用仍然存活的玩家。
}
```

长期业务关系应保存领域 ID，并通过业务注册表重新定位。不要把裸 Entity、`EntityHandle` 或 `InstanceId` 当成跨 Scene 永久引用。

跨 Scene `ReparentTo` 适用于真实所有权迁移，例如玩家从 Lobby 转入 Battle。它不应作为普通分类操作。迁移后整个子树会改变 Scene 分区，依赖的 Scene 级服务和业务上下文必须重新检查。

## AI 创建 Scene 前检查清单

AI 在创建 Scene 类型或调用 `World.AddScene` 前，必须回答：

1. 这个 Scene 表达的顶层运行时作用域是什么？
2. 为什么它不能作为现有 Scene 中的 Child Entity？
3. 它是否可以在其他 Scene 存活时独立销毁？
4. 哪个系统拥有创建和移除它的权限？
5. Scene 移除时，哪些 Entity 必须一起销毁？
6. 是否存在跨 Scene 引用，这些引用如何处理失效？
7. 它与 Unity Scene 是一一对应、跨 Unity Scene 存活，还是完全无关？
8. 是否可能同时存在多个实例？
9. Scene 名称如何保证唯一和可诊断？
10. 使用一个现有 Scene 是否已经能完整满足需求？

如果不能明确回答第 1～5 项，不应该创建新 Scene。

AI 的最终方案必须明确给出 `新建 Scene` 或 `复用现有 Scene` 的结论，并说明顶层所有权、独立销毁边界和生命周期协调者。

## 团队规范摘要

1. 新对象默认进入现有的合适 Scene。
2. 整个运行时只使用唯一的 `World.Instance`；多个顶层作用域使用多个 Scene，不使用多个 World。
3. Scene 只表达 World 下的顶层运行时作用域。
4. 能独立整体销毁是创建 Scene 的核心依据。
5. 有业务 owner 的对象使用 Child Entity。
6. 不按类型、模块、UI 页面或地图记录拆 Scene。
7. 不把 Unity Scene 与 GameEntity Scene 自动一一映射。
8. 当前不能把 Scene 当成公开的暂停或独立更新单元。
9. 每类 Scene 由唯一协调者创建和移除。
10. 多实例 Scene 名称包含领域 ID，跨 Scene 引用必须允许目标失效。

可以把整个模型浓缩为：

> Scene 管顶层运行时作用域，Child 管作用域内的独立对象，Component 管对象能力，容器管状态。

以及：

> 是否创建 Scene，取决于能否独立整体销毁，而不是取决于它是否叫“场景”。

[返回知识库首页](README.md)
