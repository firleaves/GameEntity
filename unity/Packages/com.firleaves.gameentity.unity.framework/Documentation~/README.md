---
title: GameEntity Unity Framework 知识库
package: com.firleaves.gameentity.unity.framework
version: 0.1.0
unity: 2022.3
language: zh-CN
source_of_truth: ../Runtime
---

# GameEntity Unity Framework 知识库

本知识库用于帮助开发者和 AI 正确使用 `GameEntity.Unity.Framework`。它只描述当前源码中已经存在的能力，不把规划、猜测或内部实现当作公开契约。

## AI 使用约定

回答 Framework 问题时按以下顺序取证：

1. 先读本页判断问题属于哪个模块。
2. 初始化、服务依赖、销毁顺序问题读[架构与生命周期](02-架构与生命周期.md)。
3. 查询方法签名、返回值所有权和模块能力读[服务 API 指南](03-服务API指南.md)。
4. 需要组合代码时读[常用场景手册](04-常用场景手册.md)。
5. 遇到异常、空服务、资源泄漏或配置问题读[排错与约束](05-排错与约束.md)。
6. 已知类型名但不知道所属模块时查[符号索引](06-符号索引.md)。
7. 文档与源码冲突时，以 `../Runtime` 下的公开接口和当前测试为准，并指出版本差异。

生成代码时必须遵守这些规则：

- 业务代码优先依赖 `IAssetPool`、`IUISystem` 等接口，通常从 `GameEntry` 获取服务。
- 访问 `GameEntry.Asset` 等强类型属性前，必须保证 `FrameworkEntry.IsReady` 为 `true`。
- 可选模块使用 `GameEntry.TryGet<T>` 或 `GameEntry.HasFeature` 探测，不用异常做正常分支。
- `AssetRef<T>`、`SubAssetsRef<T>`、`RawFileRef`、`AssetPreloadToken`、`InstanceRef`、`SceneRef` 都代表资源或实例所有权，使用后必须 `Dispose`、`Release`、`Return` 或卸载。
- 所有 `UniTask` 异步 API 都应传递上层 `CancellationToken`；MonoBehaviour 中优先使用与销毁绑定的令牌。
- 不直接构造 `AssetPoolEntity`、`UISystemEntity` 等服务实现，除非正在写该模块的隔离测试。
- 不通过 Unity Hierarchy 改变 GameEntity 的真实父子关系。
- 不虚构同步版本、协程版本、自动重试或线程安全保证。

## 一句话模型

`FrameworkEntry` 创建一个 `FrameworkScene`，根据 `FrameworkOptions.Features` 将服务 Entity 挂到该 Scene 下，再把接口注册到服务表；业务代码在初始化完成后通过 `GameEntry` 访问这些接口。

```text
Unity GameObject
└── FrameworkEntry
    ├── GameEntityRunner（缺少时自动补充）
    ├── FrameworkScene（GameEntity Scene）
    │   ├── IAssetPool
    │   ├── IInstancePool
    │   ├── IUISystem
    │   └── 其他启用的服务
    └── Runtime Root（对象池、音频、UI 等 Unity 对象根节点）
```

## 按问题路由

- “怎么启动、放哪些组件”：读[快速开始](01-快速开始.md)。
- “为什么服务为空、功能怎么裁剪”：读[架构与生命周期](02-架构与生命周期.md)。
- “如何加载资源、打开 UI、切场景”：读[服务 API 指南](03-服务API指南.md)。
- “如何写启动任务、流程状态、网络 RPC”：读[常用场景手册](04-常用场景手册.md)。
- “Framework 尚未初始化、功能未启用、ViewKey 缺失”：读[排错与约束](05-排错与约束.md)。
- “某个类在哪、该查哪个接口”：读[符号索引](06-符号索引.md)。

## 模块总览

- 核心入口：`FrameworkEntry`、`GameLauncher`、`GameEntry`、`FrameworkOptions`。
- 资源链路：Asset、ResourceUpdate、Scene、InstancePool。
- 表现与交互：Audio、UI、Localization、GameSettings。
- 游戏逻辑：Timer、Event、GameData、Save、Procedure、Network。
- 扩展机制：`FrameworkExtensionAsset`、`FrameworkExtensionContext`、`IGameLaunchTask`。

## 版本与维护

- 包版本：`0.1.0`。
- 最低 Unity 版本：`2022.3`。
- 直接依赖：`com.firleaves.gameentity.unity`、`com.tuyoogame.yooasset`、`com.unity.ugui`。
- 源码变化后，应同步更新受影响的服务页、场景示例和符号索引。
- 验证依据位于 `Tests/EditMode` 与 `Tests/PlayMode`。
