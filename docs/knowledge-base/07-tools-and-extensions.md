# 工具与扩展包

## 远程调试器

远程调试由两部分组成：

```text
Unity GameEntityDebugClient
  → ws://host:9527/ws/device?token=...
  → Go Debug Server
  → Web Console / 录制文件 / 回放
```

Unity 包：`unity/Packages/com.firleaves.gameentity.unity.debugger`，版本 `0.1.0`，最低 Unity `2022.3`，依赖 GameEntity for Unity。

在 Bootstrap GameObject 添加 `GameEntity/GameEntity Debug Client`，配置：

- `ServerUrl`：默认 `ws://127.0.0.1:9527/ws/device`。真机必须改为电脑的局域网 IP。
- `AccessToken`：必须与服务端启动输出的 Token 一致。
- 采样、队列和重连选项：按目标设备负载调整，先采用默认值。

启动电脑侧服务：

```bash
cd "tools/gameentity-debug-server"
npm --prefix "webapp" install
npm --prefix "webapp" run build
go run ./cmd/gameentity-debug-server --enable-mock=true
```

服务端会输出 Web Console、设备 WebSocket、Token 和录制目录。先用 mock 设备验证页面，再关闭 mock 接入 Unity 真机。完整参数和协议见 `tools/gameentity-debug-server/README.md`。

远程调试器发送实体树快照、指标和日志；服务端只解析 JSON 外壳，大 payload 原样缓存、转发和录制，因此新增 payload 字段通常不要求同步修改 Go 核心。

## 包兼容矩阵

- Core：普通 .NET，`net8.0` / `netstandard2.1`。
- GameEntity for Unity：Unity `2022.3+`。
- Unity Debugger：Unity `2022.3+`，依赖 GameEntity for Unity。

[返回知识库首页](README.md)
