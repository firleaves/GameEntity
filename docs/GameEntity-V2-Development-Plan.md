# GameEntity V2 开发计划

## 目标

这份计划用于把 `GameEntity` V2 从当前“已经开始 hierarchy 内核化”的状态，推进到可作为主线使用的完整 V2。

推进顺序明确分为两条主线：

- 第一阶段：先完整实现 `src/GameEntity` 纯 C# 核心 V2
- 后续阶段：再迁移和升级 Unity 适配层

这样可以先把运行时语义、数据结构、引用模型和测试边界稳定下来，再让 Unity Hierarchy 映射成为 hierarchy ownership 的调试投影，而不是继续绑定旧的 V1 对象树实现。

## 当前状态判断

当前 `src/GameEntity` 已经不是纯 V1，已经具备 V2 第一阶段的雏形：

- 已有 `EntityHierarchy` 作为统一运行时入口
- 已有 `NodeStore`、`ObjectStore`、`SceneRegistry`
- `Entity` 已经不再保存 `_parent`、`_children`、`_components` 作为结构真相
- `Parent`、`Children`、`Components` 已变成 façade 查询或兼容快照
- 已有 `EntityNode`、`EntityHandle`、`EntityNodeKind`、`EntityNodeFlags`
- `EntityHandle` 已收敛为 `long NodeId` 句柄，`NodeId` 由 `World.IdGenerator` 生成，销毁后不复用
- 已有第一批语义查询 API
- `EntityRef<T>` 已具备 `IsAlive`、`ValueOrNull`、`TryGet`

但它还不是完整 V2：

- `NodeStore` 仍使用多份字典索引，还不是文档中的统一节点表 + 链表关系形态
- scheduler、scene bucket、snapshot、sync、diagnostics 还没有完整实现
- `Entity` façade 仍承担较多生命周期逻辑
- Unity 新适配包当前还需要同步到最新 C# 核心
- 旧 `Assets/GameEntity` 仍是 V1 实现

## 总体原则

### 1. 先完成 C# 核心，再碰 Unity

Unity 适配层依赖核心语义。  
如果核心的 handle、destroy、scene 分区、owner 查询和事件模型还在变化，Unity 映射会反复返工。

因此第一阶段只处理：

- `src/GameEntity`
- 核心文档
- 核心测试
- 核心构建产物

暂不迁移：

- `Assets/GameEntity`
- `unity/GameEntity.Unity`
- Unity Inspector
- Unity Hierarchy 映射

### 2. 外部主心智是 ownership，不是自由树

V2 不要求用户理解一棵可任意导航的业务树。  
用户应该理解的是：

- 对象属于哪个 owner
- owner 决定生命周期
- owner 销毁时 owned 对象一起销毁
- component 是宿主的一部分
- child entity 是宿主拥有的运行时实例

树只作为 ownership 的调试视图和销毁路径存在。

### 3. 保留顺手 API，弱化路径依赖

继续保留：

- `AddChild<T>()`
- `AddComponent<T>()`
- `GetComponent<T>()`
- `Destroy()`

逐步弱化：

- `.Parent.Parent`
- 直接依赖 `Children` / `Components` 字典修改结构
- 长期缓存裸 `Entity`

逐步强化：

- `Owner`
- `FindOwner<T>()`
- `TryFindOwner<T>()`
- `GetComponentInAncestors<T>()`
- `EntityRef<T>`
- `EntityHandle`

### 4. 测试先锁语义，再做优化

V2 第一阶段的重点不是立即追求极限性能，而是先锁定运行时契约：

- 创建即挂接
- rooted 才激活
- 单 owner
- scene 分区
- component 与 child 语义区分
- destroy cascade
- 引用失效可判定
- 结构快照不反向修改 hierarchy

只有这些语义被测试覆盖后，才能继续做底层存储优化和 Unity 适配。

## 阶段一：完整实现 C# 核心 V2

阶段目标：

**让 `src/GameEntity` 成为完整、可测试、可独立发布的 V2 hierarchy core。**

### 1.1 梳理并冻结核心运行时契约

任务：

- 明确 `EntityHierarchy` 是唯一结构真相
- 明确 `Entity` 是业务 façade
- 明确 `NodeStore` 是 ownership、scene、component 关系来源
- 明确 `ObjectStore` 只负责 handle 到对象实例解析
- 明确 component 快速查询由 `NodeStore` 统一负责
- 明确 `SceneRegistry` 只负责 scene 名称与 scene root 节点映射
- 明确 `World.AddScene` 是 scene root 唯一正式注册入口，`Scene` 构造不直接进入 hierarchy

需要产出：

- 更新 V2 文档中的术语和约束
- 给核心类型补充必要中文注释
- 明确哪些 API 是推荐入口，哪些是兼容入口

验收标准：

- 代码中不再出现新的结构真相分叉
- `Entity` 不重新引入 `_parent`、`_children`、`_components`
- 所有结构变更都经过 `EntityHierarchy`

### 1.2 完善 NodeStore 与 EntityNode

任务：

- 补齐 `EntityNode` 中的结构字段，向草案靠拢
- 明确 `NodeId`、`EntityId`、`InstanceId` 的职责
- 决定是否从当前字典索引过渡到显式 sibling 链表结构
- 补齐 scene root、child entity、component entity 的统一校验
- 防止残留 cross-scene owning relation
- 允许受控 reparent 将整棵 subtree 迁移到目标 scene 分区
- 防止循环 owner 关系

建议实现顺序：

1. 保持当前字典索引，先补校验和测试
2. 再决定是否引入 `FirstChildNodeId`、`NextChildSiblingNodeId`
3. 如果引入链表结构，保留必要索引用于快速查询

验收标准：

- 同一节点只能有一个 owner
- 普通节点只能属于一个 scene
- 不允许把 ancestor 挂到 descendant 下
- 不允许残留跨 scene owning relation
- 跨 scene reparent 后，迁移节点及其 children/components 必须全部归入目标 scene 分区
- scene root 销毁等价于销毁整个 scene subtree

### 1.3 完整实现 EntityHandle 与引用模型

任务：

- 明确 `EntityHandle` 是否公开
- 如果公开，设计只读、可比较、可序列化的 API
- 保持 `NodeId` 为 `long`，由 `World.IdGenerator` 生成，并在 World 生命周期内单调递增、不复用
- 明确 handle 失效依赖 node 记录删除
- 明确 `EntityRef<T>` 与 `EntityHandle` 的关系
- 给长期引用制定推荐规则

建议方向：

- `EntityHandle` 表达 hierarchy 节点身份
- `EntityRef<T>` 继续作为业务层友好的泛型安全引用
- 长生命周期字段优先使用 `EntityRef<T>`
- hierarchy 内部和诊断工具优先使用 `EntityHandle`

验收标准：

- 对象销毁后旧 handle 无法解析成功
- 节点复用后旧 handle 不会误指向新对象
- `EntityRef<T>.TryGet` 在对象销毁后稳定返回 false
- 没有测试依赖裸 `Entity` 长期有效

### 1.4 完善 lifecycle 与 rooted 规则

任务：

- 明确未 rooted 对象的合法状态
- 明确对象何时获得 `InstanceId`
- 明确对象何时注册到 World scheduler
- 明确 scene 传播只由 hierarchy 执行
- 明确 destroy 流程只由 `EntityHierarchy.DestroySubtree` 驱动
- 检查对象池复用时 hierarchy 状态是否完全重置

验收标准：

- 只有挂入 rooted owner 链后对象才激活
- `Destroy(owner)` 必然级联销毁 children 和 components
- 重复 destroy 安全
- 销毁过程中不会重复触发 destroy
- 对象池复用不会携带旧 handle、旧 scene、旧 owner

### 1.5 完善语义化查询 API

任务：

- 保留现有 `FindOwner<T>()`
- 保留现有 `TryFindOwner<T>()`
- 保留现有 `GetComponentInParent<T>()`
- 保留现有 `GetComponentInAncestors<T>()`
- 保留现有 `GetSiblingComponent<T>()`
- 增加 `Owner` 或明确 `Parent` 的兼容定位
- 增加 `GetSceneRoot()` 或 `TryGetSceneRoot(out Scene scene)`
- 增加必要的 `TryGetComponent...` 系列 API
- 文档中标注 `Children` / `Components` 是快照或兼容入口

验收标准：

- 常见业务查询不需要 `.Parent.Parent`
- 查询 API 表达意图，而不是表达路径
- 修改 `Children` / `Components` 返回值不会影响 hierarchy
- 所有新增查询有单元测试覆盖

### 1.6 完善 Component 与 Child 的语义边界

任务：

- 明确 `ComponentEntity` 是宿主的一部分
- 明确 `ChildEntity` 是宿主拥有的运行时实例
- 明确 component 默认共享宿主业务身份的策略
- 明确 child 默认生成独立业务身份的策略
- 检查 `AddComponentWithId` 与 `AddChildWithId` 的语义一致性
- 检查 component reattach、remove、destroy 的边界行为

验收标准：

- 同一 owner 下同类型 component 只能存在一个
- child 按 business id 唯一
- component 从 owner 移除时只销毁 component subtree
- owner 销毁时 component 和 child 都被销毁
- component 与 child 在 hierarchy 记录中 `EntityNodeKind` 清晰区分

### 1.7 补齐核心测试

任务：

- 为 `src/GameEntity` 增加纯 C# 测试工程
- 覆盖 scene 创建与销毁
- 覆盖 AddChild / AddComponent
- 覆盖 reparent
- 覆盖 remove child / remove component
- 覆盖 destroy cascade
- 覆盖 EntityRef 失效
- 覆盖 EntityHandle 销毁后失效、NodeId 不复用
- 覆盖 snapshot 不可反向修改
- 覆盖 cross-scene owning 禁止
- 覆盖循环 owner 禁止

测试约束：

- 单次测试命令最大运行时间控制在 60 秒内
- 优先使用 `dotnet test`
- 测试不依赖 Unity

验收标准：

- `dotnet build src/GameEntity/GameEntity.csproj --no-restore` 通过
- `dotnet test` 通过
- 关键生命周期语义有明确断言
- 失败时能定位到具体 hierarchy 契约

### 1.8 增加 Diagnostics 基础能力

任务：

- 提供 hierarchy 结构快照 API
- 提供 scene subtree 遍历 API
- 提供 orphan 检查
- 提供 invalid handle 检查
- 提供 cross-scene owning 检查
- 提供节点统计信息

注意：

第一阶段只做基础诊断，不做 Unity UI。

验收标准：

- 纯 C# 下可以导出 scene ownership 结构
- 可以统计 scene 下 entity、component、child 数量
- 可以检测 hierarchy 内部明显结构错误
- 这些能力可供后续 Unity Hierarchy 映射复用

### 1.9 明确 Scheduler 后续边界

任务：

- 梳理 World scheduler 与 V2 hierarchy 的关系
- 决定 scheduler 是否在第一阶段只保留现状
- 为后续 scene bucket、phase bucket 留接口
- 不在第一阶段做大规模调度重构，除非当前设计阻塞 V2 语义

验收标准：

- 当前 update / late update 行为不回退
- hierarchy 能为后续 scene bucket 提供 scene node 信息
- 文档明确 scheduler 是阶段二优化，不阻塞第一阶段核心完成

## 阶段一完成定义

当下面条件全部满足时，可以认为 C# 核心 V2 完整落地：

- `src/GameEntity` 内部结构真相统一在 `EntityHierarchy / NodeStore`
- `Entity` 只作为 façade，不保存真实结构
- ownership、component、scene 分区、destroy cascade 行为稳定
- `EntityRef<T>` 与 `EntityHandle` 边界清晰
- `EntityHandle` 销毁后不会误解析到新对象
- 语义化查询覆盖主要业务场景
- 纯 C# 测试覆盖核心生命周期契约
- 基础 diagnostics 能导出 ownership 结构
- 构建和测试稳定通过

## 阶段二：同步和清理核心发布形态

阶段目标：

**让 V2 C# 核心成为 Unity 和其他宿主可稳定引用的产物。**

任务：

- 明确 `GameEntity.csproj` 的目标框架
- 确认 `netstandard2.1` 产物用于 Unity
- 增加构建脚本或发布脚本
- 防止 Unity 包继续引用旧 DLL
- 清理或标记旧 `Assets/GameEntity` 的 V1 状态
- 更新 README 中的包使用方式

验收标准：

- 可以一条命令构建核心 DLL
- Unity 适配包引用的是最新 V2 DLL
- 不再出现同名但不同版本的 `GameEntity.dll` 混用
- 文档清楚说明旧包与新包的区别

## 阶段三：迁移 Unity 适配层

阶段目标：

**把 Unity Hierarchy 从 V1 对象树映射，升级为 V2 hierarchy ownership 的调试投影。**

任务：

- 更新 `unity/GameEntity.Unity` 引用的 `GameEntity.dll`
- 让 `UnityEntityViewRegistry` 基于 V2 事件和 diagnostics 同步视图
- 明确 Unity `Transform.parent` 只是调试镜像，不是 hierarchy 真相
- 将 `ComponentView` 从长期缓存裸 `Entity` 逐步升级到 handle / ref 解析
- 在 Inspector 中区分 component entity 与 child entity
- reference relation 只在 Inspector 展示，不映射成 Hierarchy child
- 增加 Unity PlayMode 回归测试

验收标准：

- hierarchy 创建 entity 后 Unity 自动生成 debug GO
- owner 变化后 GO hierarchy 跟随更新
- entity destroy 后 GO 正确释放
- component 与 child 在 UI 上可区分
- Unity 层级拖拽不会绕过 hierarchy 结构真相
- Unity 测试覆盖基础生命周期和 hierarchy 投影

## 阶段四：可选性能与数据化演进

阶段目标：

**在 V2 语义稳定后，再推进热点数据 packed 化和调度优化。**

候选方向：

- scene bucket scheduler
- phase bucket scheduler
- dirty-driven processing
- attribute packed data
- effect packed data
- tag packed data
- replication state
- snapshot / sync 增量事件

原则：

- 只优化热点
- 不破坏 façade API
- 不牺牲 ownership 生命周期语义
- 每次优化都要有测试和性能数据支撑

## 推荐近期执行顺序

近期建议只执行阶段一，并按下面顺序推进：

1. 增加纯 C# 测试工程，先锁当前行为
2. 补 cross-scene owning 和循环 owner 校验
3. 完成 `EntityHandle` 的销毁失效机制
4. 梳理 `EntityRef<T>` 与 `EntityHandle` 的公开边界
5. 补 `Owner`、`GetSceneRoot`、`TryGet...` 系列语义查询
6. 增加 diagnostics 的基础结构快照能力
7. 更新 V2 文档，把已落地行为和暂缓行为标清楚

## 暂不处理清单

阶段一期间暂不处理：

- Unity Inspector 视觉改造
- Unity Hierarchy 分组展示
- 旧 `Assets/GameEntity` 包迁移
- scheduler 大重构
- packed data
- 网络同步
- 编辑器拖拽反向修改 hierarchy

这些都等 C# 核心 V2 稳定后再处理。

## 一句话总结

第一阶段的目标不是“让 Unity 看起来像 V2”，而是先让 `src/GameEntity` 本身真正成为 V2：

**内部结构统一、生命周期可靠、引用安全、查询语义化、测试可验证。**
