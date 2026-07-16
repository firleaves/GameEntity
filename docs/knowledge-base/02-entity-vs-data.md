# Entity 与普通数据的建模边界

> 本规范用于回答：什么时候使用 Child Entity、什么时候使用 Component Entity、什么时候直接用 `List<T>`、`Dictionary<TKey, TValue>`、普通 class、record 或 struct 保存数据。

## 核心原则

> 默认使用普通数据。只有当一个对象需要独立的运行时身份、生命周期或层级行为时，才提升为 Entity。

不要根据“数据多不多”“类复杂不复杂”“以后可能扩展”决定是否使用 Entity。真正的判断标准是：

> 这个对象是否需要被 GameEntity 运行时单独管理？

可以把三种模型浓缩为：

- Entity 管独立行为和生命。
- Component Entity 管 owner 的唯一能力。
- 普通容器管状态、记录和批量数据。

## AI 必须遵守的默认策略

AI 在为 GameEntity 设计或生成代码时，必须按以下顺序判断：

1. 先尝试把对象设计为普通数据。
2. 只有出现明确的独立运行时需求，才使用 Entity。
3. Entity 表达 owner 的唯一能力时，使用 Component Entity。
4. Entity 表达可独立存在的运行时实例时，使用 Child Entity。
5. 不得仅为拆文件、方便 Inspector、看起来组件化或假设未来扩展而创建 Entity。
6. 无法说明 Entity 准入理由时，保持普通数据模型。

AI 给出设计时，应明确写出选择依据，例如：

```text
选择：普通 ItemInstanceData
原因：生命周期完全依附 InventoryComponent，不独立更新、销毁、迁移或引用。
```

或者：

```text
选择：CastTaskEntity，作为 SkillComponent 的 Child Entity
原因：一次施法可独立打断和销毁，需要目标 EntityRef、逐帧更新与子任务。
```

## 一、什么时候使用普通数据

普通数据包括容器、普通 class、record、struct、DTO、配置和状态对象。对象符合以下大部分条件时，应使用普通数据：

- 生命周期完全依附拥有者。
- 不需要独立 `Awake`、`Update`、`OnDestroy`。
- 不需要独立 `Destroy` 或 `ReparentTo`。
- 不需要通过 `EntityHandle` 或 `EntityRef<T>` 单独定位。
- 不会拥有自己的 Child 或 Component。
- 不参与更新要求或 Ready 状态判断。
- 总是和拥有者一起加载、保存、复制或回滚。
- 数量较大，需要批量遍历或连续存储。
- 修改入口集中在一个明确的 owner 或 system 中。

示例：背包堆叠数据。

```csharp
public readonly struct ItemStackData
{
    public ItemStackData(int itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }

    public int ItemId { get; }
    public int Count { get; }
}

public sealed class InventoryState
{
    private readonly Dictionary<int, ItemStackData> _items = new();

    public IReadOnlyDictionary<int, ItemStackData> Items => _items;

    public void Add(int itemId, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        int currentCount = _items.TryGetValue(itemId, out ItemStackData current)
            ? current.Count
            : 0;
        _items[itemId] = new ItemStackData(itemId, currentCount + count);
    }
}
```

普通容器不等于公开可变集合。推荐使用私有集合、只读视图和领域方法，避免多个系统任意修改同一份状态。

## 二、什么时候使用 Component Entity

Component Entity 表达：

> 某个 Entity 拥有的一项唯一能力或子系统。

优先使用 Component Entity 的条件：

- 离开 owner 后没有独立业务意义。
- 同一个 owner 上同类型通常只能有一个。
- 其他逻辑需要通过类型查询这项能力。
- 需要独立初始化、清理、更新或作为更新要求时的 Ready 状态。
- 需要封装一组领域规则和内部状态。
- 可能作为其他 Component 的更新要求。

典型结构：

```text
UnitEntity
├── InventoryComponent
├── AttributeComponent
├── SkillComponent
├── BuffComponent
└── AIComponent
```

Component Entity 可以拥有普通数据容器：

```csharp
public readonly struct ItemInstanceData
{
    public ItemInstanceData(long instanceId, int configId)
    {
        InstanceId = instanceId;
        ConfigId = configId;
    }

    public long InstanceId { get; }
    public int ConfigId { get; }
}

public sealed class InventoryComponent : Entity, IAwake, IDestroy
{
    private readonly Dictionary<long, ItemInstanceData> _items = new();

    public IReadOnlyDictionary<long, ItemInstanceData> Items => _items;

    public void Awake()
    {
    }

    public void AddItem(ItemInstanceData item)
    {
        // 通过领域方法维护背包不变量。
        _items.Add(item.InstanceId, item);
    }

    public void OnDestroy()
    {
        _items.Clear();
    }
}
```

这里 `InventoryComponent` 是能力，`ItemInstanceData` 是该能力管理的数据。不能因为 Inventory 是 Component，就把每个物品也自动设计成 Child Entity。

## 三、什么时候使用 Child Entity

Child Entity 表达：

> 一个具有独立运行时身份和生命过程，并且当前由另一个 Entity 拥有的对象。

出现以下任意一个强条件时，应优先考虑 Child Entity：

- 需要独立创建和销毁。
- 需要单独执行 `Update`。
- 需要独立更新状态、Ready 状态或更新要求判断。
- 需要被其他系统长期运行时引用。
- 需要独立 `EntityHandle` 或 `EntityRef<T>`。
- 需要在不同 owner 之间通过 `ReparentTo` 转移。
- 需要拥有自己的 Child 或 Component。
- 销毁时需要释放自己的订阅、资源或外部句柄。
- 需要在层级快照、Unity Hierarchy 或远程调试器中作为独立节点出现。
- 生命周期可能早于或晚于 owner 中的其他数据。

典型结构：

```text
BattleScene
├── PlayerEntity
├── MonsterEntity
├── ProjectileEntity
└── SummonEntity
```

Child Entity 可以与 owner 一起级联销毁，但“会级联销毁”不等于“没有独立生命周期”。关键是它在存活期间是否需要被单独创建、更新、引用、迁移或结束。

## 决策流程

每次建模按以下顺序执行：

```text
对象是否需要独立 Destroy、Update、Reparent、Handle 或子树？
├── 是：使用 Entity
│   ├── 表达 owner 的唯一能力：Component Entity
│   └── 表达独立运行时实例：Child Entity
└── 否
    ├── 是否需要类型查询、更新要求/Ready 状态或独立能力生命周期？
    │   ├── 是：Component Entity
    │   └── 否：普通数据
    └── 是否只是集合中的一条记录？
        └── 是：普通数据
```

判断“是否需要 Handle”时不能倒置因果。不能先决定使用 Entity，再以“Entity 有 Handle”为理由证明它应该是 Entity。必须先存在真实的跨系统运行时定位需求。

## 典型场景

### 属性

生命值、攻击力、防御力只是 Unit 状态，且只随 Unit 保存和销毁时，优先使用普通 `UnitState`。

属性需要被其他组件按类型查询、处理修改器、发布变化、参与依赖或拥有独立生命周期时，才提升为 `AttributeComponent`。

示例中的 `StatsComponent` 用于演示 Component API，不代表所有属性字段都必须 Entity 化。

### 背包和物品

默认结构：

```text
UnitEntity
└── InventoryComponent
    └── Dictionary<ItemInstanceId, ItemInstanceData>
```

普通物品、堆叠数量、绑定状态使用普通数据。

某件物品只有在具备独立耐久度生命周期、可挂载宝石/效果、可跨背包迁移、被交易系统长期引用，或者离开背包后仍作为运行时对象存在时，才考虑 `ItemEntity`。

持久化物品数据和运行时装备 Entity 应保持分离：存档保存 DTO，加载后按需要重建 Entity。

### Buff

大量 Buff 由一个系统统一结算时：

```text
BuffComponent
└── List<BuffState>
```

`BuffComponent.Update` 批量处理持续时间、叠层和结算，`BuffState` 使用普通数据。

每个 Buff 需要独立回调、更新策略、驱散引用、异步资源、子效果或复杂状态机时：

```text
UnitEntity
└── BuffComponent
    ├── PoisonBuffEntity
    ├── ShieldBuffEntity
    └── BurningBuffEntity
```

Buff 是否为 Entity 取决于执行模型，不取决于它在概念上是不是“游戏对象”。

### 技能

推荐拆分：

```text
SkillConfig       普通配置数据
SkillState        普通运行时状态
SkillComponent    角色的技能管理能力
CastTaskEntity    一次独立施法过程
```

等级、冷却、解锁状态通常是容器数据；一次施法需要前摇、引导、打断、目标引用、逐帧更新和子任务时，适合成为 Child Entity。

### 投射物

少量复杂投射物需要独立移动、命中、销毁和引用时，可以使用 Child Entity。

同屏存在成千上万个同构弹丸时，应由 `ProjectileSystem` 使用连续容器批量处理 `ProjectileState`。不要为了统一对象模型把所有高频细粒度数据都 Entity 化。

## 为什么不能过度 Entity 化

每个 Entity 节点都可能涉及：

- 层级节点和 Handle 分配。
- Child/Component 查询索引。
- Scene 调度分区。
- Entity 树事件通知。
- Unity Hierarchy 投影。
- 快照和结构校验。
- 对象池与生命周期状态。

Entity 是运行时管理单元，不是零成本的数据记录。大量、同构、高频遍历的数据优先保存在容器中，可以减少层级节点数量、随机访问和调试投影开销。

性能不是唯一标准。即使数量很少，没有独立运行时语义的数据也不应该无理由 Entity 化。

## 持久化边界

Entity 是运行时对象，不是存档模型：

- 存档保存 DTO、State、Record 和领域 ID。
- 不持久化 `EntityHandle` 或 `InstanceId`。
- 加载存档后，通过领域数据重建 Scene、Child 和 Component。
- 普通持久化数据不得保存裸 Entity。
- 临时运行时数据确实需要引用 Entity 时使用 `EntityRef<T>`，并处理引用失效。

## Entity 准入与降级规则

新增 Entity 时，代码评审必须能指出它至少承担以下一项真实职责：独立生命周期、独立调度、独立层级或迁移、稳定运行时引用、依赖/Ready 状态、独立资源所有权，或者自己的 Child/Component 子树。

如果一个 Entity 没有生命周期逻辑、不更新、不参与依赖、没有子树、不迁移、没有代码通过 Handle/EntityRef 引用，并且只保存若干字段，那么它通常应该降级为普通数据。

## AI 生成代码前检查清单

AI 在输出实现前必须逐项回答：

1. 谁拥有这份数据？
2. 它是否能脱离 owner 独立存活？
3. 它是否需要独立创建、销毁或迁移？
4. 它是否需要每帧或按策略单独更新？
5. 是否存在真实的跨系统运行时引用需求？
6. 它是否需要拥有自己的 Child 或 Component？
7. 它是 owner 的唯一能力，还是集合中的一个实例？
8. 它的数量级是多少，是否需要批量处理？
9. 它如何保存和恢复，是否错误地把 Entity 当成存档 DTO？
10. 如果不用 Entity，普通数据模型是否已经能完整满足需求？

AI 的最终方案必须给出明确结论：`普通数据`、`Component Entity` 或 `Child Entity`，并用一到三句话说明原因。不能只给出代码而不解释建模选择。

## 团队规范摘要

1. 新业务对象默认从普通数据开始。
2. Component Entity 只表达 owner 的唯一能力。
3. Child Entity 只表达独立运行时实例。
4. 集合中的普通记录默认不做 Entity。
5. 资源、订阅和异步任务具有独立所有权时，才形成 Entity 的重要理由。
6. 大量同构对象优先容器化和批量处理。
7. Entity 与持久化 DTO 分离。
8. 不为 Inspector、拆文件或未来假设创建 Entity。
9. 先使用普通数据，需求出现后再提升为 Entity。
10. 无法说明独立运行时语义的 Entity 应考虑降级。

[返回知识库首页](README.md)
