# GameEntity V2 演化方向探讨

## 背景

`GameEntity` 当前已经不是一个“只有树结构”的轻量容器，而是一套带强生命周期约束的运行时框架。

它的核心能力并不只是：

- 能创建 `Entity`
- 能挂 `Component`
- 能形成父子结构

而是：

- 只有进入已 rooted 的运行时层级后，对象才真正激活
- 子实体、组件和宿主之间存在明确 ownership
- 父节点销毁时，整棵子树和全部挂载物一起销毁
- 运行时默认不鼓励长期悬空对象

这套语义让 `GameEntity` 在业务开发时非常顺手，尤其适合需要强生命周期管理的战斗、任务、网络运行时对象和调试对象。

V2 的讨论不应从“要不要保留树”出发，而应从“要保留哪些核心能力、要解决哪些真实痛点”出发。

## 当前模型的本质

从 [dotnet/src/GameEntity/Core/Entity.cs](/Users/zhang/AIWork/GEGAS/dotnet/src/GameEntity/Core/Entity.cs:1) 的实现看，当前模型可以概括为：

- **ownership tree**：`Entity` 通过 `Parent` 与 `_children` 构成拥有关系
- **component attachment**：组件通过 `ComponentParent` 与 `_components` 挂到宿主
- **scene rooting**：对象获得 `IScene` 后才完成 rooted、注册和激活
- **cascade dispose**：父节点 `Dispose()` 会递归销毁 children 与 components

这意味着当前模型的关键不是“树形展示”，而是：

**唯一 owner、根化激活、级联销毁、强生命周期一致性**

这也是 `GameEntity` 当前最值得保留的部分。

## 当前模型的优势

### 1. 生命周期边界天然清晰

对象一旦挂入运行时层级，就拥有明确 owner；owner 销毁时，其全部 owned 对象都会被递归销毁。

这让运行时对象的生命周期非常容易推理：

- 它归谁管
- 谁死了它也要死
- 它什么时候应该失效

相比松散引用模型，这种方式更不容易产生逻辑泄漏和悬空对象。

### 2. 创建与挂接体验顺手

当前的典型写法非常自然：

```csharp
var actor = scene.AddChild<ActorEntity>();
var gameplay = actor.AddComponent<GameplayAbilityComponent>();
var ability = gameplay.AddChild<GameplayAbilitySpec>();
```

这种“创建即挂接、挂接即拥有、拥有即纳入生命周期”的体验，是 `GameEntity` 使用上的核心优势，不应轻易放弃。

### 3. 业务建模贴近运行时直觉

对于很多运行时对象，业务开发者本来就会自然地这么思考：

- 这个对象属于哪个宿主
- 这个对象是不是宿主的一部分
- 这个对象是不是宿主拥有的一个运行时实例

`GameEntity` 当前模型和这种思维方式是对齐的。

### 4. 不需要依赖 Unity 才能成立

当前 core 的生命周期、更新注册、依赖门控和销毁语义都在纯 C# 层完成。  
这使它天然适合作为独立运行时框架继续演进，而不是 Unity 专属方案。

## 当前模型的核心矛盾

当前模型真正的矛盾，不是“树不好”，而是：

**ownership 表达很强，但查询导航和非 owning 引用还不够成熟。**

这会在两个地方暴露出来。

### 1. 树内导航容易退化成手写路径

当前 `Entity` 提供的局部查询能力主要是：

- `GetComponent<T>()`
- `GetChild<T>(id)`
- `GetAllChildren()`
- `GetAllComponents()`

这些接口足以覆盖“当前节点局部查找”，但不足以覆盖复杂业务里的 ownership 查询。

于是业务代码容易出现这种写法：

```csharp
var asc = Parent.Parent as AbilitySystemComponent;
```

仓库里已经有类似例子，例如 [dotnet/src/GEGAS/Effect/ActiveGameplayEffect.cs](/Users/zhang/AIWork/GEGAS/dotnet/src/GEGAS/Effect/ActiveGameplayEffect.cs:64)。

这说明当前暴露给开发者的仍是“结构路径”，而不是“语义化 owner 查询”。

### 2. 非 owning 引用的安全模型还不够完整

树负责拥有关系，但运行时对象之间经常还需要引用其他对象。

一旦某处缓存了一个裸 `Entity` 引用，而这个对象后续被销毁，就会出现典型问题：

- 引用方不知道对象是否还活着
- 调用前必须显式做 `IsDisposed` 判断
- 对象池复用后，旧引用容易变得危险

当前仓库其实已经有一个正确方向： [dotnet/src/GameEntity/Core/EntityRef.cs](/Users/zhang/AIWork/GEGAS/dotnet/src/GameEntity/Core/EntityRef.cs:1)

`EntityRef<T>` 通过 `InstanceId` 校验引用是否仍然有效，这证明 `GameEntity` 已经隐含区分了两类关系：

- **owning relation**：由树和组件挂载表达
- **reference relation**：应通过安全引用表达

V2 应该把这件事做得更明确。

## 对“树结构心智负担”的重新理解

很多关于 V2 的讨论，表面上是在讨论“要不要保留树”，但更准确的问题其实是：

**要不要让开发者直接面对一棵通用自由树。**

这两者不是一回事。

当前 `GameEntity` 的强生命周期控制，依赖的不是“展示意义上的树”，而是：

- 每个运行时对象有唯一 owner
- owner rooted 后对象才真正激活
- owner dispose 时进行级联销毁

因此，V2 并不一定要去掉内部 ownership tree。  
真正可以调整的是：

- 是否继续把自由 `Parent` 操作作为主业务模型
- 是否继续让业务代码通过手写路径来表达语义

换句话说，V2 更应该把树看作：

**运行时内部的 ownership 结构，而不是业务层手工维护的结构图。**

## V2 演化目标

V2 建议围绕以下目标推进。

### 1. 保留强生命周期，不放弃 ownership

这是 `GameEntity` 当前最重要的价值，不应牺牲。

必须继续保持：

- rooted 才激活
- 单一 owner
- 级联销毁
- 无默认悬空对象
- 引用失效可判定

### 2. 保留创建即挂接的开发体验

`AddChild<T>()`、`AddComponent<T>()` 这一类 API 非常符合当前运行时特点，应该继续保留为高频入口。

V2 不需要为了“更像 ECS”而放弃这种直观体验。

### 3. 降低业务层对结构路径的依赖

业务层最不应该继续扩散的是：

- `.Parent.Parent`
- `.Parent.GetComponent<T>()` 的反复链式写法
- “我必须记得自己在树上的哪一层”

V2 应该把这部分替换成语义化查询接口。

### 4. 明确区分拥有关系和引用关系

树和组件挂载表达“谁拥有谁”。  
普通对象之间的访问、缓存和跨节点关系，不应该继续依赖裸 `Entity` 强引用。

V2 应该把这层模型从隐式约定提升为显式框架能力。

## V2 的演化原则

### 原则一：树继续保留，但定位收窄为 ownership tree

V2 不建议去掉树。  
更合理的方向是：

- 树负责 owner 关系
- 树负责 rooted 传播
- 树负责级联销毁
- 树不再承担全部导航语义

这可以保留当前最强的生命周期能力，同时避免业务开发者把树当作任意结构图来思考。

### 原则二：查询 API 应按语义设计，而不是按路径设计

相比让业务代码自己爬树，更适合提供这类能力：

- `GetComponentInParent<T>()`
- `FindOwner<T>()`
- `FindRootOwner<T>()`
- `GetSiblingComponent<T>()`
- `GetSceneService<T>()`
- `TryGetOwner<T>(out T owner)`

API 命名最终可以继续打磨，但方向应明确：

**表达意图，而不是表达路径。**

### 原则三：非 owning 引用默认走安全句柄

当前的 `EntityRef<T>` 已经证明这条路可行。  
V2 应考虑把它提升为更明确的正式引用模型。

建议目标：

- 业务上凡是不拥有对方，只缓存安全引用
- 裸 `Entity` 只用于局部即时访问，不鼓励长期保存
- 安全引用提供更友好的判断和访问方式

例如未来可以逐步补充：

- `IsAlive`
- `TryGet(out T entity)`
- `ValueOrNull`

### 原则四：组件和子节点仍保留两种 ownership 语义

当前 `ComponentParent` 与 `Parent` 是两条不同语义的边，这一点非常好。

建议继续保持：

- **组件**：更适合表达“宿主的一部分”，通常共享宿主业务身份
- **子节点**：更适合表达“宿主拥有的运行时实例”，通常拥有独立身份

这比把所有东西都压成一种节点关系更清晰。

### 原则五：内部实现可以逐步数据化，但不破坏外部语义

V2 如果未来要做底层平铺、中心化索引、数据导向优化，这些都可以考虑。  
但不应以牺牲以下外部语义为代价：

- 创建即挂接
- rooted 才激活
- owner dispose = subtree dispose
- 组件与子节点的区别

也就是说，底层可以演化，核心运行时契约不宜轻易破坏。

## 可以优先推进的方向

如果以最小风险推进 V2，建议优先做下面几件事。

### 1. 增加 ownership 查询 API

这是当前最直接、收益最高的一步。

它能明显减少：

- `.Parent.Parent`
- 手写层级路径
- 业务代码对结构细节的耦合

### 2. 正式化 `EntityRef<T>` 的使用边界

建议明确规则：

- owning relation 继续走树
- reference relation 走 `EntityRef<T>` 或未来统一句柄

这样可以显著改善对象销毁后的引用安全问题。

### 3. 收紧直接改 `Parent` 的业务使用方式

`Parent` 作为底层原语可以保留，但不应鼓励业务逻辑频繁直接写结构路径和重挂接逻辑。

更合适的方向是逐步提供更语义化的挂接入口。

### 4. 在文档和示例中强调 ownership 思维

V2 的开发心智不应是：

- “我把它挂到树的哪一层”

而应是：

- “谁拥有它”
- “它是宿主的一部分，还是宿主拥有的一个运行时实例”

只要 ownership 说清楚，结构关系往往就自然成立。

## 不建议的方向

### 1. 不建议为了降低心智负担而去掉强生命周期

当前 `GameEntity` 最强的能力，恰恰来自强生命周期。  
如果为了减少结构思考而放弃 owner 关系和级联销毁，代价会远大于收益。

### 2. 不建议直接推翻成纯 ECS 式松关系模型

这会带来：

- 生命周期语义削弱
- 树状业务表达变差
- 开发体验下降
- 大量现有 API 和业务写法被迫重写

这不符合 `GameEntity` 当前的真实优势。

### 3. 不建议继续把“结构路径”当成主要查询方式

哪怕树保留，业务层也不应继续大量书写层级路径代码。  
这会放大结构调整带来的维护成本。

## 对 V2 的总体判断

`GameEntity` 的 V2 不应理解为“去树”，也不应理解为“模仿 DOTS/ECS”。

更准确的理解是：

**在保留强生命周期和创建即挂接体验的前提下，把 ownership、查询和引用三件事拆清楚。**

具体来说：

- **ownership** 继续由树和组件挂载表达
- **query** 逐步改成语义化导航
- **reference** 逐步改成安全引用

如果这三件事逐步补齐，`GameEntity` 会更像一个成熟的纯 C# 运行时框架，而不是一个只靠树结构组织对象的容器。

## 总结

V2 最值得保留的，不是“树”这个形式本身，而是树背后的运行时哲学：

- rooted 才激活
- owner 决定生命周期
- dispose 默认级联
- 默认不接受悬空对象

V2 最需要改进的，也不是“树结构太多”，而是：

- 查询语义过于依赖结构路径
- 非 owning 引用缺少统一规范

因此，`GameEntity` 的合理演化方向是：

**保留 ownership tree，弱化手写路径，强化语义查询，正式化安全引用。**

这条路线能够同时保住当前开发体验、生命周期强度和未来继续做纯 C# 数据化优化的空间。
