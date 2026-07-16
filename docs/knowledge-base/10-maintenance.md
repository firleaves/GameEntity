# 知识库维护指南

## 目标

知识库必须回答三类问题：第一次怎么跑、某个 API 的边界是什么、出错后按什么证据排查。它不是设计愿望清单，也不复制全部源码。

## 变更时必须同步的页面

- Core 公共 API 或生命周期：`01`～`04`、`08`、`09`。
- Entity/Component/容器建模边界：`02-entity-vs-data.md`、快速开始、API 速查和相关业务示例。
- Scene 创建、销毁和 Unity Scene 映射边界：`02-scene-boundaries.md`、快速开始、层级、Unity 接入、API 速查和故障排查。
- Unity Runner/投影行为：`05`、`08`、`09`。
- 包版本/Unity 版本/依赖：总入口、`05`、`07`。
- Debug Server 参数或协议：`07`、`09`。

## 更新流程

1. 先读取实现、测试和可运行示例，不从旧文档反推 API。
2. 用 `rg` 搜索公开类型、方法声明和调用点，确认重载与访问级别。
3. 修改文档中的最小示例，避免引入与主题无关的基础设施。
4. 全文搜索旧类型名、旧包 ID、旧路径和旧版本号。
5. 检查所有 Markdown 相对链接。
6. 构建 Core 并运行测试；Unity 变更按包执行 EditMode/PlayMode 或对应验证入口。
7. 在变更说明中记录“行为变化、迁移方式、验证命令”。

## 写作规范

- 使用中文说明，代码标识保持源码原名。
- 一个页面解决一个主题，标题可被全文搜索命中。
- 示例必须使用当前公开 API，不调用 `internal` 层级、调度或对象池实现。
- 明确“必须”“推荐”“当前实现”的差异。
- 设计草稿必须标为计划，不把未落地能力写成已支持。
- 资源/订阅/句柄示例必须展示释放或所有权边界。
- 新增常见失败模式时同步补充 `09-troubleshooting.md`。
- 面向 AI 的架构规范必须给出可执行判定条件，不只使用“视情况而定”等模糊表述。
- 新示例必须明确普通数据、Component Entity 和 Child Entity 的选择理由。
- 新增 Scene 的示例必须说明顶层作用域、独立整体销毁边界、唯一名称和生命周期协调者。
- 所有文档和 AI 方案都以单 World 为硬约束；除非任务明确修改 Core 单例架构，否则不得把多个 World、每个对局一个 World 或 World 间迁移写成受支持方案。

## 本地验证命令

```bash
dotnet build "src/GameEntity/GameEntity.csproj"
dotnet test "tests/GameEntity.Tests/GameEntity.Tests.csproj" --no-restore
dotnet run --project "apps/GameEntity.CoreTestApp/GameEntity.CoreTestApp.csproj"

go test ./...
npm --prefix "tools/gameentity-debug-server/webapp" test -- --run
```

Go 命令在 `tools/gameentity-debug-server` 目录执行。单元测试应设置不超过 60 秒的外部超时，避免 CI 或自动化会话无限等待。

## 文档验收清单

- 总入口能到达所有页面，所有页面能返回总入口。
- 安装路径、包 ID、版本和最低 Unity 版本与 `package.json` 一致。
- 示例中的类型、成员、泛型约束和命名空间真实存在。
- 核心示例可编译，命令可在仓库根执行。
- 不引用 `Library/`、`Temp/`、生成的 `.csproj` 或 Web `node_modules` 作为事实来源。
- 用户已有设计草稿和历史文档未被无意覆盖。

## 主要事实来源

```text
src/GameEntity/                                  Core 实现
tests/GameEntity.Tests/                          Core 行为测试
apps/GameEntity.CoreTestApp/                     Core 可运行示例
unity/Packages/com.firleaves.gameentity.unity/   Unity 适配与示例
unity/Packages/com.firleaves.gameentity.unity.debugger/
tools/gameentity-debug-server/                   电脑侧调试服务
```

[返回知识库首页](README.md)
