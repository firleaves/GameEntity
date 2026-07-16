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

远程调试器发送实体树快照、Framework 快照、指标和日志；服务端只解析 JSON 外壳，大 payload 原样缓存、转发和录制，因此新增 payload 字段通常不要求同步修改 Go 核心。

## GPU Terrain

包：`unity/Packages/com.firleaves.gameentity.unity.gpu-terrain`，版本 `0.1.0`。它要求 Unity `6000.0`、URP `17.3.0`，并依赖 Unity Framework，不适用于 Unity 2022.3 项目。

主要能力：

- GPU Driven Terrain 与 Vegetation。
- Compute culling、indirect draw、Hi-Z 遮挡剔除。
- Unity 6 URP RenderGraph RendererFeature。
- 自定义 Terrain/Grass/Tree Shader 与主光阴影。
- Terrain/Vegetation 烘焙窗口、样例构建和批处理验证。
- F8 Debug Overlay、GPU readback 统计和运行时快照。

推荐接入顺序：

1. 确认 Unity 6、URP 17.3 与 Framework 已正常启动。
2. 通过包内菜单构建/导入 `GpuTerrainDemo`。
3. 用一键工具把 RendererFeature 安装到当前 URP RendererData。
4. 创建并烘焙 `GpuTerrainAuthoring` 与 `GpuVegetationAuthoring` 数据。
5. 运行样例，使用 F8 Overlay 检查 terrain、draw、buffer、culling 和错误状态。
6. 再替换为项目资产与参数，不从空场景手工拼装底层 RenderPass。

常用运行时接口是 `IGpuTerrainSystem` 与 `IGpuTerrainVegetationSystem`。出现空画面时先读取两者 `LastError`，再检查 RendererFeature、烘焙资产、相机和材质，不要直接改底层 buffer/RenderGraph 实现。

GPU Terrain 的完整现状与批处理验证入口以包内 `README.md` 和 `docs/gpu-driven-terrain-vegetation-implementation-status.md` 为准；计划文档描述目标，不等于全部已经交付。

## 包兼容矩阵

- Core：普通 .NET，`net8.0` / `netstandard2.1`。
- GameEntity for Unity：Unity `2022.3+`。
- Unity Framework：Unity `2022.3+`，YooAsset `2.3.19`，uGUI `2.0.0`。
- Framework Extension：Unity `2022.3+`，依赖 Framework。
- Unity Debugger：Unity `2022.3+`，依赖 GameEntity for Unity。
- GPU Terrain：Unity `6000.0+`，URP `17.3.0`，依赖 Framework。

[返回知识库首页](README.md)
