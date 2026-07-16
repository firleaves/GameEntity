# GameEntity 专用知识库

本知识库面向 GameEntity 的使用者、维护者和自动化代码助手。内容以当前仓库源码、测试和可运行示例为准；`docs/` 下的设计草稿只用于理解演进方向，不作为公开 API 承诺。

## 从哪里开始

- 第一次使用纯 C# 核心库：阅读[核心快速开始](01-core-quick-start.md)。
- 正在设计业务实体：阅读[实体层级与生命周期](02-hierarchy-and-lifecycle.md)。
- 不确定该用 Child、Component 还是容器：阅读[Entity 与普通数据的建模边界](02-entity-vs-data.md)。
- 不确定是否应该新建 Scene：阅读[Scene 的创建与边界规范](02-scene-boundaries.md)。
- 需要更新、更新要求或对象池：阅读[调度、更新要求与池化](03-scheduling-dependency-pooling.md)。
- 需要安全保存引用或定位结构问题：阅读[引用、诊断与日志](04-reference-diagnostics-logging.md)。
- 在 Unity 中接入：阅读[Unity 适配层](05-unity-integration.md)。
- 使用完整游戏服务：阅读[Unity Framework](06-unity-framework.md)。
- 使用远程调试或 GPU 地形：阅读[工具与扩展包](07-tools-and-extensions.md)。
- 查方法、约束或常见报错：阅读[API 速查](08-api-cheatsheet.md)和[故障排查](09-troubleshooting.md)。

## 包边界

```text
GameEntity Core（纯 C#，net8.0 / netstandard2.1）
└── GameEntity for Unity（Unity 生命周期、日志和 Hierarchy 投影）
    ├── GameEntity Unity Framework（游戏服务集合，依赖 YooAsset、uGUI）
    │   ├── Framework Extension（当前提供输入扩展）
    │   └── GPU Terrain（Unity 6 + URP 17.3）
    └── GameEntity Unity Debugger（远程诊断客户端）
        └── GameEntity Debug Server（Go 服务 + Web Console）
```

核心库不知道 Unity。Unity Hierarchy 中的 GameObject 是调试视图，不是 Entity 数据源。Framework 和 GPU Terrain 都是可选层，不应反向侵入 Core。

## 核心心智模型

1. GameEntity 采用单 World 架构：一个进程内同一时刻只允许一个有效的 `World.Instance`，所有 Scene 和 Entity 都属于它。
2. `Scene` 是每棵实体树的注册根；Scene 派生类型只能通过 `World.Instance.AddScene` 注册，不能作为 Child 或 Component，声明了 `ChildOf` 的 Scene 类型也不能成为 SceneRoot。
3. Child Entity 表达所有权和业务树，Component Entity 表达 owner 的组合能力；普通 Component 查询、存在性判断和移除统一使用精确运行时类型。
4. `IAwake` 立即接收创建参数，`IStart` 在首次满足运行条件时执行一次，`IFixedUpdate` 与 `IUpdate` 分别定义固定模拟和普通帧更新，`IDestroy` 负责清理。创建期间在 `Awake/RegisterSystem` 中结束自身生命会令创建失败，不会返回无效对象。
5. 结构修改必须走 `AddChild`、`AddComponent`、`ReparentTo`、`Destroy` 等 Core API；`ChildOf` 可声明真实运行时放置约束。
6. 长期运行时引用优先使用 `EntityRef<T>`；跨系统消息可携带 `EntityHandle` 后重新解析。
7. `World.Dispose()` 是当前运行时会话的终点；Dispose 开始后，保存的旧 World 引用会对公开实例 API 抛出 `ObjectDisposedException`。之后再次访问 `World.Instance` 只表示顺序开启下一次会话，两个 World 不得并存。
8. Entity 树表达所有权和级联销毁，不自动传播 Enable、暂停或时间缩放；这些运行域必须显式建模。
9. 每次 `World.Update/FixedUpdate` 在入口冻结一次全局调度快照，同一 Handle 在一个 pass 中最多运行一次；更新入口不允许重入。

两条更新通道遵守同一组状态与要求，区别只在时间来源和普通 Update 的调用频率控制：

```text
World.Update(deltaTime)
  -> IsUpdateEnabled
  -> RequireForUpdate
  -> Start once（两个通道共享）
  -> UpdateInterval
  -> IUpdate.Update

World.FixedUpdate(fixedDeltaTime)
  -> IsUpdateEnabled
  -> RequireForUpdate
  -> Start once（两个通道共享）
  -> IFixedUpdate.FixedUpdate
```

Scene Root 不参与这两条 Scheduler 通道；`IEntityUpdateState` 只控制当前 Entity，不向 Child 或 Component 传播。

## 单 World 硬约束

`World` 不是业务建模层级，也不是可按玩法、Unity Scene、玩家或对局拆分的对象。GameEntity 的固定契约是：

- 一个进程内同一时刻只有一个有效的 `World.Instance`。
- 业务不得自行构造、缓存并继续使用旧 World，也不得并行维护、切换或嵌套多个 World。
- 多个玩法会话、服务器对局、Unity Scene 或顶层业务领域统一建模为同一 World 下的多个 GameEntity Scene。
- `World.Dispose()` 只用于整个 GameEntity 运行时退出、测试隔离或完整重启；重启是前后两个会话，不是多 World 并存。
- `Dispose()` 幂等，但一旦开始就不可逆；旧引用不能再查询、创建、更新、观察或移除 Scene，这些操作统一抛出 `ObjectDisposedException`。
- 除非任务本身是在修改 Core 的单例架构，否则 AI 不得把“再建一个 World”“每个对局一个 World”或“World 间迁移”作为业务方案；应继续在 Scene、Child、Component 和普通数据之间选择。
- 日常设计、代码审核和问题分析直接把单 World 当作既定前提，不再重复讨论多 World 的优劣或兼容性；只有任务明确要求修改 Core 单例/生命周期架构时才重新评估。

后续文档中的“当前 World”“本次 World 会话”都只用于描述引用的有效期，不表示框架支持多个同时运行的 World。

## 建模默认规则

AI 和开发者设计业务模型时，默认先使用普通数据。只有对象需要独立生命周期、调度、层级迁移、运行时引用、更新要求/Ready 状态或子树时，才提升为 Entity：

- 独立运行时对象使用 Child Entity。
- owner 的唯一能力使用 Component Entity。
- 状态、配置、记录和批量对象使用普通容器数据。
- World 下可独立整体销毁的顶层运行时作用域使用 Scene。

详细准入条件、决策流程和案例见[Entity 与普通数据的建模边界](02-entity-vs-data.md)与[Scene 的创建与边界规范](02-scene-boundaries.md)。

## 文档可信度

每篇文档头部的“依据”列出主要源码或测试入口。发生冲突时，可信度从高到低为：

1. 当前分支的编译结果与自动化测试。
2. `src/`、`unity/Packages/`、`tools/` 下的实现。
3. 本知识库。
4. 根目录历史说明和 `docs/` 中的设计/计划文档。

## 维护入口

API、包版本或运行方式变化后，按[知识库维护指南](10-maintenance.md)同步更新。知识库采用相对链接，既可在 GitHub 阅读，也可由本地全文检索和代码助手索引。
