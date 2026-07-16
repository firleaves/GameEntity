# GameEntity Benchmarks

这个工程用于测量 `src/GameEntity` V2 Core 的关键路径性能。

## 运行

```bash
dotnet run -c Release --project "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -- --filter "*"
```

开发中只跑一个场景可以加 `--filter`，例如：

```bash
dotnet run -c Release --project "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -- --filter "*UpdateRegisteredEntities*"
```

BenchmarkDotNet 的详细结果会输出到：

```text
BenchmarkDotNet.Artifacts/results/
```

## 当前覆盖

- `CreateChildren`：只创建子实体，作为创建路径基线。
- `CreateChildrenWithComponent`：创建子实体并挂组件，覆盖 component index 写入。
- `QueryComponents`：批量组件查询，覆盖 component index 读取。
- `ResolveHandles`：批量 `EntityHandle` 解析。
- `ReparentAcrossScenes`：跨 scene 迁移 subtree，覆盖 scene 分区传播。
- `UpdateRegisteredEntities`：驱动已注册 `IUpdate` 实体。
- `CaptureSnapshot` / `ValidateHierarchy`：diagnostics 快照与结构校验。

## 对比建议

如果要看“新架构比旧架构快多少”，建议在旧架构分支上保留同名 benchmark 方法和相同
`EntityCount` 参数，然后分别运行：

```bash
dotnet run -c Release --project "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -- --filter "*"
```

对比生成的 `*-report-github.md` 中的 `Mean`、`Allocated` 和 `Ratio`。旧分支和新分支必须使用同一台机器、同一 SDK、同一 Release 配置运行，数据才有意义。
