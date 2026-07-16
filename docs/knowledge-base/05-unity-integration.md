# Unity 适配层

> 依据：`unity/Packages/com.firleaves.gameentity.unity/Runtime/` 与 `Samples~/GameEntityDemo`。包版本 `0.1.0`，最低 Unity `2022.3`。

## 安装

在 Package Manager 的 Git URL 中添加：

```text
https://github.com/firleaves/GameEntity.git?path=unity/Packages/com.firleaves.gameentity.unity
```

仓库内开发可在 `Packages/manifest.json` 使用本地 `file:` 路径。安装后可从 Package Manager 导入 `GameEntity Demo` 示例。

## 场景入口

在启动场景创建 GameObject，并添加 `GameEntity/GameEntity Runner`：

```text
Bootstrap
└── GameEntityRunner
```

`GameEntityRunner` 负责：

- 每帧累计 `Time.deltaTime`，按 `FixedUpdatesPerSecond` 调用零到多次 `World.Instance.FixedUpdate(fixedDeltaTime)`，并限制最多 `MaxFixedStepsPerFrame` 次。
- 固定模拟完成后调用一次 `World.Instance.Update(Time.deltaTime)`；`FixedInterpolationAlpha` 可供 View 在前后 Model 状态间插值。
- 可选注入 Unity logger。
- 通过 `World.ObserveEntities` 监听 Entity 树事件并投影 GameObject/`ComponentView`，启动较晚时会回放已有树。
- 在拥有 World 生命周期时，于销毁或应用退出时调用 `World.Dispose()`。

同一时刻只能有一个有效 Runner，它驱动唯一的 `World.Instance`。其默认执行顺序为 `-10000`，通常早于普通业务 `MonoBehaviour` 初始化；这保证自动更新驱动尽早就绪，但不是 Entity 树投影正确性的硬性先后条件。

## Runner 配置

- `ViewRoot`：调试投影根节点；为空时使用 Runner 所在 GameObject。
- `AutoCreateViews`：是否为已注册 Entity 自动创建 GameObject。
- `DestroyViewsOnEntityDestroy`：Entity 销毁时是否销毁对应 GameObject。
- `UseUnityLogger`：是否把 `Log.Logger` 替换为 `UnityGameEntityLogger`。
- `OwnsWorldLifetime`：Runner 销毁时是否清理整个 World。

无论加载多少个 Unity Scene，GameEntity 都只有一个 World。多个 Unity Scene 共用这一运行时；Runner 只是外部宿主管理的一部分时，必须指定唯一的 World 生命周期拥有者，避免某个场景卸载时提前 `Dispose`。不得为每个 Unity Scene 创建或保存独立 World。

Unity Scene 管 Unity Object 和资源场景，GameEntity Scene 管业务 Entity 的顶层生命周期，两者不默认一一对应。一个 Unity Scene 可以承载多个 GameEntity Scene，多个 Additive Unity Scene 也可以共同服务一个 BattleScene。只有两者确实需要同步整体销毁时才建立映射，详细规范见[Scene 的创建与边界规范](02-scene-boundaries.md)。

## 创建实体树

Runner 在 `Awake` 中就绪后，业务代码按 Core API 创建：

```csharp
using GameEntity;
using UnityEngine;

public sealed class BattleBootstrap : MonoBehaviour
{
    private BattleScene _scene;

    private void Start()
    {
        const string sceneName = "Battle";
        _scene = (BattleScene)World.Instance.AddScene(
            sceneName,
            new BattleScene(sceneName));

        _scene.AddChild<UnitEntity, string>("Player");
    }

    private void OnDestroy()
    {
        if (_scene != null && !_scene.IsDestroyed)
        {
            _scene.Destroy();
        }
    }
}
```

推荐把序列化配置保存在业务 Bootstrap `MonoBehaviour` 上，并在它的 Unity `Start` 或显式 `Install()` 中把参数传给 `AddScene/AddChild/AddComponent`。这样 Runner 的宿主配置、Mono 的序列化数据和 Entity 的 `Awake(args)` 有一个明确组合点，不需要多个任意 Mono `Awake` 互相猜执行顺序。

这里的 Unity `Start` 只是组合入口；GameEntity 的 `IStart.Start()` 由 `World.Update` 在 Entity 首次满足运行条件时调用，两者不是同一套生命周期。

## Runner 与 Entity 的初始化顺序

两种顺序都受支持：

```text
Runner.Awake → AddScene/AddChild
AddScene/AddChild → Runner.Awake（回放现有树）
```

`World.Instance` 不依赖 Mono 驱动才能创建。业务若先创建 Scene/Entity，Core 生命周期中的 `Awake` 会立即完成；Runner 随后调用 `ObserveEntities(..., replayExisting: true)`，按父到子顺序补建所有 `ComponentView`。因此不需要为了 Unity 投影强制“先有 Runner，才能 AddScene”。

边界仍需明确：

- Runner 出现之前没有 Unity 自动调用 `World.FixedUpdate/World.Update`，所以 Entity 可以完成 `Awake`，但不会自动执行 `IStart/IFixedUpdate/IUpdate`。
- Entity 的 `Awake` 不应查询尚未建立的 Unity `ComponentView`。需要 View 的逻辑放在 Unity adapter、观察者回调或异步 View Entity 的 Ready/Start 阶段。
- 若某个 Mono 必须在首个 Update pass 前提交配置，仍应通过统一 Bootstrap 或 Script Execution Order 明确该业务约束；Observer 回放只解决树投影先后，不替代业务初始化顺序。
- Runner 销毁时若 `OwnsWorldLifetime == true` 会 Dispose 整个 World。跨 Unity Scene 保留 World 时，应由常驻宿主持有 Runner，或关闭该选项并指定唯一清理者。
- 切换 Unity Scene 时继续使用同一个 `World.Instance`；需要独立清理的业务范围通过 `AddScene/RemoveScene` 管理，不通过替换 World 管理。

## Hierarchy 与 Inspector

自动投影后，每个 Entity 对应一个 GameObject，并挂载 `ComponentView`。名称来自实体的 `ViewName`：

```csharp
protected override string ViewName => "Player";
```

Inspector 展示的是 Entity 当前值，可用于查看属性、集合、`EntityRef` 和接口状态。需要隐藏成员时添加 `GameEntityInspectorIgnoreAttribute`。

关键边界：

- GameObject/Transform 不是业务状态，不要通过拖动 Transform 改实体 owner。
- 删除投影 GameObject 不等价于调用 `Entity.Destroy()`。
- 真实结构永远以 `World.CaptureEntitySnapshot()` 和 Core 查询结果为准。

## 手动绑定已有 GameObject

关闭自动创建或希望 Entity 对应已有视图时：

```csharp
GameEntityRunner runner = GetComponent<GameEntityRunner>();
ComponentView view = runner.Registry.Bind(entity, existingGameObject);

if (runner.Registry.TryGetView(entity, out ComponentView current))
{
    Debug.Log(current.gameObject.name);
}

runner.Registry.Unbind(entity);
```

同一 Entity 绑定到新 GameObject 时，旧 view 会先解绑。`DestroyViewsOnEntityDestroy` 决定解绑后是否销毁视图对象。

## 推荐启动结构

```text
Bootstrap GameObject
├── GameEntityRunner        # Core 驱动与调试投影
├── FrameworkEntry          # 可选，完整游戏服务
└── GameEntityDebugClient   # 可选，远程诊断
```

通常先由 Runner 建立宿主，再由 Framework/业务创建 Scene 和 Entity；已有 Entity 树也可由 Runner 启动时回放。不要在每个玩法 Scene 重复放置 Runner。

[返回知识库首页](README.md)
