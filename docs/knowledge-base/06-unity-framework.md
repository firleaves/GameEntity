# Unity Framework

> 依据：`com.firleaves.gameentity.unity.framework/Runtime/` 与对应 EditMode/PlayMode 测试。包版本 `0.1.0`，最低 Unity `2022.3`。

## 定位与依赖

Framework 是 Core/Unity 适配层之上的可选游戏服务集合，直接依赖：

- `com.firleaves.gameentity.unity` `0.1.0`
- YooAsset `2.3.19`
- Unity uGUI `2.0.0`

使用 Git UPM 时，应先确保 Core Unity 包和这些依赖可解析，再添加：

```text
https://github.com/firleaves/GameEntity.git?path=unity/Packages/com.firleaves.gameentity.unity.framework
```

## 启动

在唯一 Bootstrap GameObject 上添加 `FrameworkEntry`。它会：

1. 确保同对象或场景中存在 `GameEntityRunner`。
2. 创建名为 `GameEntity.Unity.Framework` 的 `FrameworkScene`。
3. 根据 `FrameworkOptions.Features` 按依赖顺序创建服务 Entity。
4. 初始化完成后注册到静态门面 `GameEntry`。

默认 `autoInitializeOnAwake = true`。手动初始化时：

```csharp
FrameworkEntry entry = GetComponent<FrameworkEntry>();
await entry.InitializeAsync(cancellationToken);

if (entry.IsReady)
{
    GameEntry.Timer.Delay(1f, () => Debug.Log("延时完成"));
}
```

在 `IsReady` 前访问 `GameEntry.Asset` 等属性会抛 `FrameworkException`。不确定功能是否启用时使用：

```csharp
if (GameEntry.TryGet<ITimerSystem>(out var timer))
{
    timer.Delay(1f, OnElapsed);
}
```

## 功能开关与依赖

`FrameworkFeatures` 是位标志。默认启用全部服务；生产项目应按需缩减。

- `GameData` 依赖 `Asset`。
- `InstancePool` 依赖 `Asset`。
- `Audio`、`Localization` 依赖 `Asset`。
- `UI` 使用实例池时依赖 `InstancePool`；也可关闭 `UIOptions.UseInstancePool`。
- `Asset`、`ResourceUpdate`、`Scene` 任一启用时会初始化 YooAsset。

```csharp
var options = FrameworkOptions.CreateDefault();
options.Features = FrameworkFeatures.Timer
    | FrameworkFeatures.Event
    | FrameworkFeatures.Save;

await entry.InitializeAsync(options, cancellationToken);
```

## 服务入口

- `GameEntry.Asset`：`IAssetPool`，资源、子资源、RawFile、预加载与释放。
- `GameEntry.Data`：`IGameData`，数据表注册、加载和查询。
- `GameEntry.ResourceUpdate`：版本、清单和资源下载。
- `GameEntry.Scene`：YooAsset 场景加载、切换和卸载。
- `GameEntry.Instance`：Prefab 实例池。
- `GameEntry.Audio`：BGM/SFX 播放与音量控制。
- `GameEntry.Timer`：延时、循环、暂停、恢复和取消。
- `GameEntry.Event`：立即发布和排队事件。
- `GameEntry.Localization`：语言与文本表。
- `GameEntry.Settings`：音量、静音、语言等设置。
- `GameEntry.UI`：UI 分组、打开、关闭、复用与焦点。
- `GameEntry.Save`：多槽位本地存档。
- `GameEntry.Procedure`：流程状态切换。
- `GameEntry.Network`：网络会话、协议与传输抽象。

## 常用模式

资源引用和实例引用都实现显式释放语义，优先使用 `using`：

```csharp
AssetKey iconKey = AssetKey.Main<Sprite>("UI/Icons/Sword");
using AssetRef<Sprite> iconRef = await GameEntry.Asset.LoadAsync<Sprite>(iconKey);
Sprite icon = iconRef.Asset;
```

事件订阅返回 `IDisposable`，应由拥有者释放：

```csharp
IDisposable subscription = GameEntry.Event.Subscribe<PlayerDied>(OnPlayerDied);
GameEntry.Event.Publish(new PlayerDied(playerId));
subscription.Dispose();
```

延时任务用 Handle 管理：

```csharp
TimerHandle handle = GameEntry.Timer.Every(
    0.5f,
    count => Refresh(count),
    repeatCount: 10,
    unscaled: true);

GameEntry.Timer.Cancel(handle);
```

Framework 的资源引用类型、`SceneRef`、`InstanceRef`、预加载 Token 都需要按所有权及时 `Dispose/Release`。不要只取出内部 Unity Object 后丢弃引用包装。

## 扩展机制

扩展继承 `FrameworkExtensionAsset`，通过 `FrameworkEntry.extensions` 安装。扩展可读取 `FrameworkExtensionContext` 的 Scene、Options 和运行时根，并向 `FrameworkScene` 注册服务。

当前 `com.firleaves.gameentity.unity.framework.extension` 提供输入系统：

- `InputFrameworkExtension`
- `IInputSystem` / `InputSystemEntity`
- `IFrameworkInputSource`
- `FrameworkExtensionLegacyInputSource`
- `SimulatedInputSource`

Unity MonoBehaviour 负责采样具体输入后端，Entity 服务只消费语义化 `FrameworkInputFrame`，避免 Core/Framework 直接耦合输入包。

## 关闭

```csharp
await entry.ShutdownAsync(cancellationToken);
```

Framework 按服务依赖的逆向顺序销毁，随后销毁 Framework Scene 和运行时 GameObject 根。若 `FrameworkEntry` 与 Runner 同时销毁，由 Runner 最终决定是否 `World.Dispose()`。

[返回知识库首页](README.md)
