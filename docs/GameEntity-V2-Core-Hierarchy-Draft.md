# GameEntity V2 Core Hierarchy Draft

## 目标

这份草案描述 `GameEntity` V2 的运行时内核方向。目标不是把 `GameEntity`
改造成纯 ECS，也不是继续维持 V1 “对象自己保存全部结构”的实现方式，而是
在保留当前开发体验和强生命周期控制的前提下，把结构真相收回到统一的
hierarchy 内核中。

V2 的核心诉求有四个：

- 保留 `AddChild<T>()`、`AddComponent<T>()` 这类顺手的开发体验
- 保留 rooted 激活、级联销毁、默认不接受悬空对象的强生命周期语义
- 让结构、引用、查询、快照、同步都建立在统一运行时模型之上
- 为后续纯 C# 的性能优化、数据导向存储和调度演进预留空间

## 当前版本的真实问题

V1 当前最有价值的能力不是“有树”，而是：

- 对象只有挂入已 rooted 的 owner 链后才真正激活
- 组件和子节点都归宿主拥有
- 父节点销毁时，整棵子树和挂载物一起销毁
- 运行时默认不鼓励长期悬空对象

这些能力都值得保留。

V1 的核心痛点主要有两个：

### 1. 结构查询过于依赖路径

业务逻辑容易写出：

```csharp
var asc = Parent.Parent as AbilitySystemComponent;
```

或者：

```csharp
var comp = Parent.GetComponent<SomeComponent>();
```

这种路径式访问会让业务代码耦合结构细节，后续一旦结构调整，调用点会大面积受影响。

### 2. 非 owning 引用缺乏统一规范

当前树负责 owning relation，但普通引用常常还是直接拿裸 `Entity`。  
这会带来典型问题：

- 对象被销毁后，外部仍持有旧引用
- 对象池复用后，旧引用可能误指向新实例
- 调用前需要大量 `IsDisposed` 判断

虽然已有 `EntityRef<T>` 雏形，但还没有提升成正式设计约束。

## 设计原则

V2 的演进遵循以下原则。

### 原则一：保留 ownership tree，不保留“自由树心智”

V2 不建议去掉树。  
更准确地说，V2 要保留的是：

- owner 关系
- rooted 激活传播
- scene 归属
- 级联销毁

但不再鼓励业务层通过手写结构路径来表达语义。

### 原则二：保留 façade API，集中 hierarchy 真相

业务开发者继续通过 `Entity` 使用运行时，但结构真相不再保存在每个对象内部。

V2 中：

- `Entity` 是面向业务的 façade
- `EntityHierarchy` 是运行时真相
- `NodeStore` 是结构与节点状态的唯一来源

### 原则三：明确区分 owning relation 与 reference relation

V2 明确区分：

- **owning relation**：谁拥有谁，由树和组件挂载表达
- **reference relation**：谁引用谁，由安全句柄或 `EntityRef<T>` 表达

两者不应继续混用。

### 原则四：多 scene 共用一个 hierarchy，但 scene 是一级生命周期分区

V2 采用：

- 一个 `EntityHierarchy`
- 多个 `SceneRoot`
- 每个节点都明确记录 `SceneNodeId`

也就是说，统一存储，按 scene 分区；统一 hierarchy，按 scene 隔离。

### 原则五：先统一结构，再逐步数据化热点

V2 的第一阶段目标不是立刻追求极致性能，而是先把结构、引用、生命周期和查询统一到一个 hierarchy 内核中。  
真正的热点 packed 化、scheduler 分组和 dirty-driven processing 应放在后续阶段推进。

## 顶层结构

V2 的建议结构如下：

```text
GameEntity V2
├── Public API Layer
│   ├── Scene
│   ├── Entity
│   ├── AddChild / AddComponent
│   ├── Query API
│   └── EntityRef / EntityHandle
├── EntityHierarchy
│   ├── SceneRegistry
│   ├── NodeStore
│   ├── ObjectStore
│   ├── ComponentIndexStore
│   ├── Scheduler
│   └── Snapshot / Sync Support
└── Optional Packed Data Layer
    ├── Attribute Data
    ├── Effect Hierarchy Data
    ├── Tag Hierarchy Data
    └── Replication State
```

业务层继续面向 `Scene` 和 `Entity` 编程；`EntityHierarchy` 负责所有运行时内核行为。

## 核心类型

### NodeId

`NodeId` 是 hierarchy 内部唯一节点身份，只对内核使用。

建议特性：

- 全局唯一
- 可重复利用，但必须配合 `Generation`
- 不与业务 `Id` 混用

### Generation

`Generation` 用于处理对象销毁后节点复用带来的失效问题。

如果一个节点被回收再利用：

- `NodeId` 可能相同
- `Generation` 必须变化

这样旧句柄就能判断自己是否已失效。

### EntityHandle

`EntityHandle` 是正式的安全句柄，用于表达非 owning 引用。

建议字段：

- `NodeId`
- `Generation`

建议能力：

- `IsValid`
- `IsAlive(EntityHierarchy hierarchy)`
- `TryResolve<T>(EntityHierarchy hierarchy, out T entity)`

### EntityNodeKind

V2 不建议把当前所有节点粗暴并成一种语义。  
建议显式保留：

- `SceneRoot`
- `ChildEntity`
- `ComponentEntity`

这与当前 V1 的 `Parent` / `ComponentParent` 双语义一致。

### EntityId

`EntityId` 对应当前 `Entity.Id` 的业务身份。

建议语义：

- 子实体通常有独立 `EntityId`
- 组件可以继续共享宿主 `EntityId`

这可以保留当前 API 和业务语义上的直觉一致性。

## EntityNode 设计

`NodeStore` 中每个节点建议至少保存以下字段：

```csharp
internal struct EntityNode
{
    public int NodeId;
    public int Generation;
    public long EntityId;
    public long InstanceId;

    public int SceneNodeId;
    public int OwnerNodeId;
    public int FirstChildNodeId;
    public int NextChildSiblingNodeId;
    public int FirstComponentNodeId;
    public int NextComponentSiblingNodeId;

    public int ComponentTypeId;
    public EntityNodeKind Kind;
    public EntityNodeFlags Flags;

    public int ObjectSlot;
}
```

这里的目标不是把一切都压成极限紧凑的存储，而是先让 hierarchy 内核有统一、显式、可计算的结构真相。

## EntityHierarchy 设计

`EntityHierarchy` 是 V2 的运行时核心。

职责建议包括：

- 持有全部节点记录
- 管理 scene root 注册
- 管理 `Entity` 对象与节点的映射
- 维护组件类型索引
- 提供句柄校验与实体解析
- 提供 subtree 销毁与 reparent
- 驱动 scheduler
- 为 snapshot / sync / diagnostics 提供统一数据入口

建议结构：

```text
EntityHierarchy
├── SceneRegistry
├── NodeStore
├── ObjectStore
├── ComponentIndexStore
├── Scheduler
└── EntityValidation
```

### SceneRegistry

`SceneRegistry` 保存：

- scene 名称到 scene root 的映射
- scene root 到 scene metadata 的映射

这样 `World` 的多 scene 语义可以继续保留，但内部不再只是字典管理 scene 对象，而是统一挂接到 hierarchy 内核。

V2 中 `Scene` 构造只负责初始化 scene 自身身份；`World.AddScene` 是 scene root 正式注册入口，负责把 scene 放入 `EntityHierarchy`、建立 `SceneRoot` 节点、注册名称映射并触发 `Awake`。

## 多 Scene 模型

V2 采用“一个 hierarchy，多棵 scene 子图”的方式。

### 基本规则

- `EntityHierarchy` 统一保存所有 scene 的节点
- 每个 scene 对应一个 `SceneRoot` 节点
- 每个普通节点必须属于且只属于一个 scene
- `SceneNodeId` 是节点的一级分区标记

### 原则

- scene 是一级生命周期边界
- scene 之间禁止残留 cross-scene owning relation
- 受控 reparent 可以把整棵 subtree 迁移到目标 scene 分区
- scene 之间允许 reference relation，但应走安全句柄

### 销毁 Scene

删除一个 scene 的本质应是：

```text
DestroySubtree(sceneRoot)
```

这样 Scene 下的全部 children、components、hierarchy objects 会一起销毁，符合 V1 现有直觉。

## Ownership 模型

V2 继续采用单 owner 模型。

### SceneRoot

`SceneRoot` 是 scene 生命周期的根。

职责：

- 作为 rooted 起点
- 作为 scene 内全部节点的最终 owner 链根节点
- 作为 scene 级查询、调度与快照边界

### ChildEntity

`ChildEntity` 表达：

- 宿主拥有的一个运行时实例
- 通常有独立业务身份
- 可以再拥有自己的 child / component

适合的对象：

- ability spec
- active effect
- targeting actor
- hierarchy task instance

### ComponentEntity

`ComponentEntity` 表达：

- 宿主的一部分
- 更偏稳定功能块而非独立运行时实例
- 通常不强调独立业务身份

适合的对象：

- attribute / tag / effect / ability 等能力块
- network adapter
- diagnostics probe

### 统一约束

- 每个节点有且只有一个 owner
- 每个节点只能 rooted 到一个 scene
- `Dispose(owner)` 默认等于 `DisposeSubtree(owner)`

## 激活与 rooted 规则

V2 继续保留“进入 rooted owner 链后才真正激活”的模型。

### 创建阶段

创建对象本身不代表 fully active。  
只有在挂入某个已 rooted 的 owner 链后，对象才应获得：

- `SceneNodeId`
- `InstanceId`
- 已注册状态
- 调度资格

### rooted 传播

当节点被挂到已 rooted 的 owner 下：

- 设置自己的 `SceneNodeId`
- 初始化或确认 `InstanceId`
- 注册生命周期事件
- 递归传播到 owned children / components

这本质上是把当前 `IScene` 传播的语义从对象字段移动到 hierarchy 内核。

## Entity façade 设计

V2 中 `Entity` 对象继续存在，但不再持有结构真相。

### Entity 内部建议只保留

- 对 hierarchy 的访问能力
- 当前节点的 handle / slot
- 少量 façade 级辅助逻辑

### 不再建议继续保留为对象真相的内容

- `_children`
- `_components`
- `_parent`

这些关系应统一由 hierarchy 解析。

### 保留的高频 API

- `AddChild<T>()`
- `AddComponent<T>()`
- `GetComponent<T>()`
- `Dispose()`

这些 API 的业务体验应继续保留。

### 新增的语义化查询 API

建议逐步提供：

- `FindOwner<T>()`
- `GetComponentInParent<T>()`
- `GetComponentInAncestors<T>()`
- `GetSiblingComponent<T>()`
- `TryGetOwner<T>(out T owner)`
- `GetSceneRoot()`

这样业务代码表达的是“我要找谁”，而不是“我往上爬几层”。

## 引用模型

V2 应显式规定：owning 和 reference 必须拆开。

### owning relation

只能通过：

- `AddChild`
- `AddComponent`
- 受控 reparent

来创建。

### reference relation

默认应通过：

- `EntityRef<T>`
- 或未来统一的 `EntityHandle`

来表达。

### 建议规则

- 不鼓励长期缓存裸 `Entity`
- 局部临时访问可以直接拿对象
- 长生命周期字段、跨系统字段、跨 scene 字段应存安全引用

### EntityRef<T> 的演进方向

当前 `EntityRef<T>` 已经有基于 `InstanceId` 的失效保护。  
V2 建议把它做得更正式：

- 增加 `IsAlive`
- 增加 `TryGet`
- 对齐未来 `EntityHandle`

最终目标是让外部引用在对象销毁后自然失效，而不是继续依赖手工 `IsDisposed` 判断。

## Scheduler 设计

V2 第一阶段不需要立刻推翻当前调度方式，但建议把 scheduler 从“全局对象队列”演化到“全局 hierarchy 调度器 + scene 分区 bucket”。

建议结构：

```text
Scheduler
├── Global Buckets
├── Scene Buckets
│   ├── Scene A
│   ├── Scene B
│   └── Scene C
└── Specialized Buckets
    ├── Update
    ├── EffectTick
    └── DirtyFlush
```

这样以后更容易支持：

- 多 scene 并存
- 单 scene 暂停 / 恢复
- 按 scene 统计运行开销
- 更细粒度的更新组织

## Snapshot / Sync / Diagnostics 的价值

把结构真相集中到 `EntityHierarchy` 后，下面这些能力会明显更好做。

### Snapshot

V2 可以围绕 scene subtree 或全局 node table 导出结构，而不必完全依赖对象递归序列化。

这更适合：

- 全量快照
- 局部 scene 快照
- 调试态导出

### Sync

一旦结构变化变成 hierarchy 内核的显式事件，就更容易追踪：

- 节点创建
- 节点销毁
- reparent
- 组件增删
- 数据变更

这对增量同步和镜像重建尤其重要。

### Diagnostics

统一内核会让以下能力更容易实现：

- hierarchy viewer
- subtree 统计
- orphan 检查
- invalid handle 检查
- cross-scene owning 检查

也就是说，V2 不只是为了性能，更是为了让 hierarchy 变得可观测。

## 性能演进路线

V2 不应该把“改成 hierarchy 内核”误解为“立刻变快很多”。  
更合理的性能演进顺序是：

### 第一阶段：结构集中化

目标：

- 统一 ownership hierarchy
- 统一 scene 分区
- 统一引用校验

收益：

- 后续所有优化都更好做

### 第二阶段：调度分组化

目标：

- scene buckets
- phase buckets
- specialized buckets

收益：

- update 与系统性处理更可控

### 第三阶段：热点数据 packed 化

目标：

- attribute graph
- effect graph
- tag graph
- replication state

收益：

- 真正进入数据导向优化阶段

V2 的 hierarchy 内核是打地基，不是性能终点。

## 迁移策略

为了降低风险，建议采用渐进式迁移。

### 阶段一：行为不改，结构收口

- façade API 尽量保持
- 内部逐步引入 hierarchy 结构
- 外部尽量无感

### 阶段二：查询 API 升级

- 增加语义化 owner / ancestor 查询
- 逐步减少 `.Parent.Parent`

### 阶段三：引用规则升级

- 补强 `EntityRef<T>`
- 逐步规范长期引用的表达方式

### 阶段四：局部数据化

- 只挑热点模块演进
- 不一次性推翻全部组件实现

## 不建议的方向

### 1. 不建议为降低心智负担而去掉 owner 模型

如果放弃单 owner、级联销毁和 rooted 语义，`GameEntity` 会失去当前最有价值的能力。

### 2. 不建议把 V2 做成纯 DOTS 风格

这会明显削弱当前顺手的开发体验，也不符合 `GameEntity` 当前的优势方向。

### 3. 不建议继续让业务代码以结构路径表达大部分语义

只要 `.Parent.Parent.GetComponent<T>()` 仍是主要写法，结构重构的成本就会持续偏高。

## 总结

如果 V2 采用这份草案，`GameEntity` 的整体形态会变成：

- 外层继续是顺手的 `Entity` / `Scene` API
- 内层改成统一的 `EntityHierarchy`
- 多 scene 共用一个 hierarchy，但按 scene 明确分区
- 树继续保留，但只承担 ownership 与生命周期职责
- 查询逐步改成语义化 API
- 引用逐步正式化为安全句柄 / `EntityRef`

一句话总结：

**V2 不是去树，也不是纯 ECS，而是 façade 风格的强生命周期运行时，底层由统一的 ownership hierarchy 和 flat hierarchy store 驱动。**
