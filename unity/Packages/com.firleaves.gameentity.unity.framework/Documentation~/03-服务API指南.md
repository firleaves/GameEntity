---
title: Framework 服务 API 指南
scope: Asset, ResourceUpdate, Scene, InstancePool, UI, Audio, Timer, Event, Localization, GameSettings, GameData, Save, Procedure, Network
symbols: IAssetPool, IUISystem, INetworkSystem, IProcedureSystem
source: ../Runtime
---

# 服务 API 指南

本页按服务列出用途、入口、关键 API 和所有权。完整签名以 `Runtime` 对应模块目录中的公开接口文件为准。

## Asset：资源池

入口：`GameEntry.Asset` / `IAssetPool`。依赖 YooAsset。

```csharp
using AssetRef<GameObject> prefab = await GameEntry.Asset.LoadAsync<GameObject>(
    AssetKey.Main<GameObject>("Assets/Game/Prefabs/Hero.prefab"),
    ct: cancellationToken);

GameObject heroPrefab = prefab.Asset;
```

常用 API：

- `LoadAsync<T>`：加载主资源，返回 `AssetRef<T>`。
- `LoadSubAssetsAsync<T>`：加载子资源集合，返回 `SubAssetsRef<T>`。
- `LoadRawFileAsync`：加载原生文件，返回 `RawFileRef`。
- `PreloadAsync` / `PreloadGroupAsync`：预加载并返回 `AssetPreloadToken`。
- `TryGetLoaded<T>`：只查询已经加载的对象，不触发加载。
- `ReleaseGroup`、`ReleaseUnused`、`UnloadUnusedAssetsAsync`：释放管理。
- `GetSnapshot`：诊断容量、引用和状态。

`AssetKey` 必须通过类型匹配的工厂创建：`Main<T>`、`SubAssets<T>`、`SubAsset<T>`、`RawFile`、`Scene`。Location 不能为空；PackageName 为空表示默认包。

所有权：资源引用和预加载令牌必须释放。`using` 只覆盖当前作用域；需要跨帧持有时把句柄保存到拥有它的 Entity/MonoBehaviour，并在销毁时释放。

## ResourceUpdate：资源更新

入口：`GameEntry.ResourceUpdate` / `IResourceUpdateSystem`。依赖 YooAsset。

标准链路：请求版本 → 更新清单 → 准备下载器 → 下载。

```csharp
string version = await GameEntry.ResourceUpdate.RequestPackageVersionAsync(
    ct: cancellationToken);
await GameEntry.ResourceUpdate.UpdatePackageManifestAsync(
    version,
    ct: cancellationToken);

ResourceDownloader downloader = await GameEntry.ResourceUpdate.PrepareDownloaderAsync(
    ct: cancellationToken);
ResourceDownloadResult result = await GameEntry.ResourceUpdate.DownloadAsync(
    downloader,
    progress,
    cancellationToken);
```

`ResourceDownloadOptions` 可按 PackageName、Tags 或 Locations 筛选，并控制并发数与失败重试次数。`ResourceDownloader` 支持 `Begin`、`Pause`、`Resume`、`Cancel`。

## Scene：Unity 场景

入口：`GameEntry.Scene` / `ISceneSystem`。依赖 YooAsset。

- `LoadSceneAsync`：按 `SceneLoadOptions.LoadMode` 加载。
- `ChangeSceneAsync`：切换场景。
- `UnloadSceneAsync`：按 location 卸载。
- `TryGetScene`、`LoadedScenes`、`ActiveSceneLocation`：查询状态。

返回的 `SceneRef` 是场景句柄。不要把它与 GameEntity 的 `Scene` 类型混淆；前者代表 YooAsset/Unity 场景加载结果，后者代表 Entity 树根节点。

## InstancePool：GameObject 实例池

入口：`GameEntry.Instance` / `IInstancePool`。依赖 Asset。

```csharp
InstanceRef instance = await GameEntry.Instance.RentAsync(
    AssetKey.Main<GameObject>("Assets/Game/Prefabs/Bullet.prefab"),
    parent,
    new InstanceRentOptions { SetActive = true },
    cancellationToken);

// 使用结束后任选一种归还方式。
GameEntry.Instance.Return(instance);
```

- `RentAsync`：租用实例。
- `WarmupAsync`：预热指定数量。
- `Return`：归还 `InstanceRef` 或已登记的 GameObject。
- `ReleasePool` / `ReleaseUnused`：释放池。
- `GetSnapshot`：查询池状态。

不要对已归还对象继续持有业务引用，也不要同时 `Dispose` 和重复 `Return`。

## UI：界面系统

入口：`GameEntry.UI` / `IUISystem`。固定依赖 InstancePool 和 Asset。

业务面板继承 `UIEntity`，在受保护生命周期中处理视图：

```csharp
public sealed class InventoryPanel : UIEntity
{
    protected override AssetKey GetDefaultViewKey()
    {
        return AssetKey.Main<GameObject>(
            "Assets/Game/UI/InventoryPanel.prefab");
    }

    protected override UniTask OnOpenAsync(UIOpenContext context)
    {
        return UniTask.CompletedTask;
    }

    protected override UniTask OnCloseAsync(UICloseContext context)
    {
        return UniTask.CompletedTask;
    }
}
```

打开与关闭：

```csharp
InventoryPanel panel = await GameEntry.UI.OpenAsync<InventoryPanel>(
    new UIOpenParams
    {
        Group = "Main",
        Depth = 20,
        ReusePolicy = UIReusePolicy.Single,
        UserData = inventoryId
    },
    cancellationToken);

await GameEntry.UI.CloseAsync(panel, UICloseReason.User);
```

`ViewKey` 可由 `UIOpenParams` 提供，也可重写 `GetDefaultViewKey`。两者都没有有效值时会抛异常。`Single` 重用已有 UI 并调用 `OnRefocus`；可通过 `Get<TUI>`、`TryGet<TUI>` 查询，通过 `CloseGroupAsync` 批量关闭。

## Audio：音频

入口：`GameEntry.Audio` / `IAudioSystem`。依赖 Asset。

- `PlayBgmAsync`、`PlaySfxAsync` 返回 `AudioPlayHandle`。
- `Stop(handle)`、`StopBgm`、`StopAll` 停止播放。
- `SetMuted`、`SetMasterVolume`、`SetBgmVolume`、`SetSfxVolume` 控制混音。
- `AudioPlayOptions` 可设置 Channel、Volume、Loop、IgnoreMute、Parent、Position。

`AudioPlayHandle` 是轻量标识，不实现 `IDisposable`；按业务生命周期显式停止循环或长期声音。

## Timer：计时器

入口：`GameEntry.Timer` / `ITimerSystem`。

```csharp
TimerHandle delayed = GameEntry.Timer.Delay(1.5f, OnReady);
TimerHandle repeated = GameEntry.Timer.Every(
    0.25f,
    tick => UpdateCountdown(tick),
    repeatCount: 4,
    unscaled: true);

GameEntry.Timer.Cancel(delayed);
```

支持 `Cancel`、`Pause`、`Resume`、`CancelAll`。`Every` 的回调参数是从 1 开始的触发次数。持有者销毁时应取消仍可能回调其成员的计时器。

## Event：事件总线

入口：`GameEntry.Event` / `IEventBus`。

```csharp
IDisposable subscription = GameEntry.Event.Subscribe<PlayerDiedEvent>(OnPlayerDied);
GameEntry.Event.Publish(new PlayerDiedEvent(playerId));

// 持有者销毁时调用。
subscription.Dispose();
```

- `Publish`：当前调用栈内立即分发。
- `Post`：加入队列，由更新阶段 `Flush`。
- `SubscribeOnce`：首次调用后自动取消。
- `Clear<TEvent>` / `ClearAll`：清理订阅。
- `EventBusOptions` 控制队列上限、每帧刷新上限和处理器异常是否继续抛出。

默认情况下单个处理器异常会记录日志并继续其他处理器；`ThrowHandlerException = true` 时会向调用方抛出。

## Localization：本地化

入口：`GameEntry.Localization` / `ILocalizationSystem`。依赖 Asset。

```csharp
await GameEntry.Localization.LoadLanguageAsync(
    "zh-CN",
    "Assets/Game/Localization/zh-CN.json",
    ct: cancellationToken);

string title = GameEntry.Localization.GetText("ui.title", "标题");
string count = GameEntry.Localization.Format("item.count", 3);
```

通过 `LanguageChanged` 监听语言变化。找不到 key 时，`GetText` 返回 fallback；没有 fallback 时的具体结果应以实现和测试为准，不要依赖未记录的占位格式。

## GameSettings：游戏设置

入口：`GameEntry.Settings` / `IGameSettings`。

保存语言、主音量、BGM/SFX 音量和静音状态。`Set*` 修改时触发 `Changed` 并同步已启用的音频服务；`Load`、`Save`、`ResetToDefault` 管理本地设置。

## GameData：静态数据表

入口：`GameEntry.Data` / `IGameData`。依赖 Asset。

```csharp
GameEntry.Data.RegisterJson<ItemConfig>(
    "Assets/Game/Data/items.json",
    json => ParseItems(json));

await GameEntry.Data.LoadAllAsync(cancellationToken);
ItemConfig sword = GameEntry.Data.Get<ItemConfig>("sword");
```

支持注册自定义 `IDataTable<T>`、JSON 表和 ScriptableObject 表。JSON parser 必须把文本转换为以字符串 id 为键的字典。使用 `TryGet` 处理可缺失条目，`ReloadAsync<T>` 热重载单表，`TableReloaded` 接收表类型通知。

## Save：存档

入口：`GameEntry.Save` / `ISaveSystem`。

```csharp
[Serializable]
public sealed class PlayerSave
{
    public int Level;
    public string Name;
}

GameEntry.Save.Save(new PlayerSave { Level = 7, Name = "Knight" }, slot: 1);

if (GameEntry.Save.Load<PlayerSave>(1, out var data))
{
    Restore(data);
}
```

`SaveSystemConfig` 控制槽位、默认槽、自动保存间隔、校验和、格式化、销毁时保存和目录名。默认存储为 `LocalSaveStorage`。`SetData` + `MarkDirty` 适合持续维护当前存档；`Save<T>` 适合立即写入。

边界：存档数据必须可被 Unity `JsonUtility` 序列化；读取返回 `false` 时不要使用输出数据。校验失败会尝试存储层备份恢复。

## Procedure：流程状态机

入口：`GameEntry.Procedure` / `IProcedureSystem`。

继承 `Procedure` 或实现 `IProcedure`，然后注册和切换：

```csharp
GameEntry.Procedure.Register<LoginProcedure>();
await GameEntry.Procedure.ChangeStateAsync<LoginProcedure>();
```

切换顺序是退出当前状态，再进入目标状态。并发切换请求按提交顺序串行执行，每个 `ChangeStateAsync` 只在其目标状态完成进入后结束。`IsTransitioning` 表示队列正在执行；当前状态的 `Update` 只在非切换阶段执行。`StopAsync` 排在已有转换之后，退出并清空当前状态。`ProcedureContext.CancellationToken` 会在 ProcedureSystem 销毁时取消，状态中的长耗时异步操作应向下传递该令牌。

## Network：网络

入口：`GameEntry.Network` / `INetworkSystem`。

网络层由 Protocol、Channel、Transport 三部分组成。内置 `JsonNetworkProtocol`、TCP Transport 和 Mock Transport。

```csharp
var protocol = new JsonNetworkProtocol()
    .Register<LoginRequest>(100)
    .Register<LoginResponse>(101);

GameEntry.Network.SetDefaultProtocol(protocol);
INetworkChannel channel = GameEntry.Network.CreateTcpChannel("game");
await channel.ConnectAsync("127.0.0.1", 9000, cancellationToken);

LoginResponse response = await channel.CallAsync<LoginRequest, LoginResponse>(
    new LoginRequest { Account = "Player001" },
    cancellationToken);
```

- `Send<TPacket>`：单向发送。
- `CallAsync<TRequest,TResponse>`：按 RpcId 匹配响应，支持超时与取消。
- `Listen<TPacket>`：返回可释放订阅。
- Channel 事件：Connected、Closed、Error、PacketReceived、HeartbeatMissed。
- `NetworkChannelConfig` 可覆盖协议、心跳、超时、缓冲区、最大包长或自定义 Transport。

请求实现 `INetworkRequest`，响应实现 `INetworkResponse`，两者都包含 `RpcId`；响应还包含 `ErrorCode` 与 `ErrorMessage`。频道关闭默认会让待处理 RPC 失败。
