# GameEntity Core Benchmark 记录

新增 benchmark 工程：`apps/GameEntity.Benchmarks`。

用途：测量 `src/GameEntity` V2 Core 热路径性能，使用 BenchmarkDotNet `0.15.8`，目标框架 `net8.0`，默认 job 为 `.NET 8.0 ShortRun`，启用 `MemoryDiagnoser`、GitHub/HTML 报告导出和控制台日志。

覆盖场景：
- `EntityHierarchyBenchmarks.CreateChildren`：创建子实体，作为创建路径基线。
- `EntityHierarchyBenchmarks.CreateChildrenWithComponent`：创建子实体并挂组件，覆盖 component index 写入。
- `EntityHierarchyBenchmarks.QueryComponents`：批量组件查询，覆盖 component index 读取。
- `EntityHierarchyBenchmarks.ResolveHandles`：批量 `EntityHandle` 解析。
- `EntityHierarchyBenchmarks.ReparentAcrossScenes`：跨 scene 迁移 subtree，覆盖 scene 分区传播。
- `EntityHierarchyBenchmarks.CaptureSnapshot` / `ValidateHierarchy`：diagnostics 快照与结构校验。
- `EntitySchedulerBenchmarks.TickRegisteredUpdates`：驱动已注册 `IUpdate` 实体。

运行命令：
```bash
dotnet run -c Release --project "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -- --filter "*"
```

单场景冒烟示例：
```bash
dotnet run -c Release --project "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -- --filter "*ResolveHandles*" --warmupCount 1 --iterationCount 1
```

生成物：`BenchmarkDotNet.Artifacts/results/`，已在 `.gitignore` 中忽略 `/BenchmarkDotNet.Artifacts/`。

本次验证：
- `dotnet test "tests/GameEntity.Tests/GameEntity.Tests.csproj" --no-restore`：通过，29/29，约 19ms。
- `dotnet build "apps/GameEntity.Benchmarks/GameEntity.Benchmarks.csproj" -c Release --no-restore`：通过，0 警告 0 错误。
- `ResolveHandles` 冒烟通过，环境为 macOS Sequoia 15.6、Apple M4 Pro、.NET SDK 8.0.303、BenchmarkDotNet 0.15.8。冒烟只用于验证链路，不代表稳定性能结论。冒烟结果：100 = 18.12us，1000 = 89.08us，5000 = 222.58us，Allocated 显示无托管分配。

对比建议：如果要看“新架构比旧架构快多少”，在旧架构分支保留同名 benchmark 方法和相同 `EntityCount` 参数，用同一台机器、同一 SDK、同一 Release 配置跑，再比较 `*-report-github.md` 的 `Mean` / `Allocated` / `Ratio`。