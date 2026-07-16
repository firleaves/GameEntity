# GameEntity Unity Framework

GameEntity Unity Framework 是构建在 GameEntity、UniTask 和 YooAsset 之上的可选游戏框架包，提供资源、数据、场景、对象池、音频、计时器、事件、本地化、设置、UI、存档、流程和网络服务。

## 文档入口

- [Framework 知识库](Documentation~/README.md)：面向开发者与 AI 的总入口。
- [快速开始](Documentation~/01-快速开始.md)：安装、场景配置和首次调用。
- [架构与生命周期](Documentation~/02-架构与生命周期.md)：初始化顺序、依赖关系和扩展机制。
- [服务 API 指南](Documentation~/03-服务API指南.md)：所有内置服务的稳定入口和资源所有权。
- [常用场景手册](Documentation~/04-常用场景手册.md)：可直接改写的组合示例。
- [排错与约束](Documentation~/05-排错与约束.md)：异常、边界条件和诊断方法。
- [符号索引](Documentation~/06-符号索引.md)：类型名到文档和源码目录的快速映射。

包命名空间统一为：

```csharp
using GameEntity.Unity.Framework;
```

> 文档基于当前包版本 `0.1.0` 与仓库源码编写。公开接口是首选契约；具体 Entity 实现类主要用于框架装配和测试，不建议业务代码直接构造。
