# 故障排查

## `AddChild` 报未挂接或层级不可用

原因通常是 Scene 只构造、未注册。

```csharp
var scene = new BattleScene("Battle");
World.Instance.AddScene("Battle", scene);
scene.AddChild<UnitEntity>();
```

确认 key 与 `Scene.Name` 完全一致。

## `scene name mismatch`

`AddScene("A", new MyScene("B"))` 的两个名称不同。统一为一个常量，避免分散硬编码。

## Scene 数量不断膨胀

通常是按对象类型、UI 页面、地图区块或流程状态错误拆分 Scene。Scene 应只表示 World 下可以独立整体销毁的顶层运行时作用域；有业务 owner 的对象改为 Child Entity，owner 的唯一能力改为 Component Entity，批量记录改为普通容器。按[Scene 的创建与边界规范](02-scene-boundaries.md)重新执行准入检查。

## 重复 Component

同一 owner 不允许两个相同运行时类型的 Component。创建前使用 `TryGetComponent`，或先 `RemoveComponent<T>()`。这些 API 都使用精确运行时类型：查询基类不会返回派生 Component。需要多态查找时遍历 `Components` 并明确处理多个匹配项。不要用 Child 绕过约束来模拟同类型组件；若业务需要多实例能力，应建一个聚合 Component，在其下挂 Child。

## Entity 不更新

依次检查：

1. 类型是否实现 `IUpdate` 或 `IFixedUpdate`，且是通过 `AddChild/AddComponent` 创建。
2. Scene 是否已注册，宿主是否持续调用对应的 `World.Update` 或 `World.FixedUpdate`。
3. Entity 是否已销毁或从 Scene 移除。
4. 是否实现 `IEntityUpdateState` 且 `IsUpdateEnabled == false`。
5. 是否声明 `RequireForUpdate` 且同 owner 的精确 Component 不存在/未 Ready。
6. 普通更新是否实现 `IEntityUpdateInterval`，且累计时间尚未达到 `UpdateInterval`。
7. `UpdateInterval` 是否为负数、NaN 或无穷值。
8. `ValidateEntities().Issues` 是否包含 `StartFaulted`、`UpdateRequirementCycle` 或 `UpdateRequirementStateError`。
9. logger 中是否有 `Update state error`、`Update requirement state error`、`Start error`、`FixedUpdate error`、`Update interval error` 或 `Update error`。

`IUpdate` 收到的是传给 `World.Update(deltaTime)` 的游戏帧时间；`IFixedUpdate` 收到的是传给 `World.FixedUpdate(fixedDeltaTime)` 的固定步长。`IEntityUpdateInterval` 到期后收到从上次实际 Update 起累计的时间，而不是当前单帧 delta。

Inspector 显示 `Ready State Error`、`Update State Error` 或更新要求 `State Error` 时，说明对应 getter 抛出了异常。Inspector 会隔离该异常，但业务仍应修复 getter，使其成为快速、无副作用且不抛异常的状态查询。

## `Awake` 执行了但 `Start` 没执行

`Awake(args)` 在挂接后立即执行，`Start()` 要等到第一次满足运行条件的普通或固定更新 pass。依次检查对应的 World 更新入口、`IsUpdateEnabled`、`RequireForUpdate` 的 Missing/NotReady 状态。Inspector 会显示 `Waiting to Start` 和具体要求原因；`ValidateEntities()` 会把未满足要求报告为 Warning。

`Start` 抛异常后不会自动重试。节点会进入 `StartFaulted`，应修复原因并销毁、重建 Entity。

## 暂停、减速或加速结果不符合预期

- `IEntityUpdateInterval` 只降低 `IUpdate` 调用频率，不改变游戏时间，不能用于慢动作。
- `IEntityUpdateState` 只跳过当前 Entity，不自动暂停 Child、Component、异步任务或 Unity Mono。
- `World.Update(0)` 仍可能执行 `Start` 和无间隔的 `IUpdate(0)`；真正的全局暂停应由宿主停止调用对应的 World 更新入口，或显式关闭需要停止的 Entity 更新状态。
- 全局减速/加速由宿主传入缩放后的 delta；局部或 Scene 级时间域目前属于业务层，Core 没有内置 Scene Clock/Pause。

## `AddChild/AddComponent` 在 `Awake` 抛异常

这是创建失败，不是等待依赖。Core 会回滚刚挂接的节点并重新抛出原异常。Entity 在 `Awake` 或 `RegisterSystem` 中主动 `Destroy()` 也会得到 `InvalidOperationException`，入口不会返回已销毁或已回池对象。`Awake` 只保存创建参数和重置本地状态；依赖其他 Component 的初始化移入 `Start`。`OnDestroy` 必须兼容尚未 Start 和 Awake 只执行了一部分的情况。

Scene `Awake` 失败或主动销毁自身时，`World.AddScene` 同样会回滚 Scene 和其中已创建的整棵临时子树。若回滚后仍能从 `GetScene` 或观察者中看到节点，应视为 Core 缺陷并附快照报告。

## `ChildOf` 放置失败

`[ChildOf]` 类型只能作为 Child；`[ChildOf(typeof(OwnerType))]` 还要求直接 owner 匹配该类型或派生类型。检查是否误用了 `AddComponent`，或 `ReparentTo` 的新 owner 类型不满足声明。失败后不应留下新节点，非法 Reparent 也应保留原 owner；可用 `PlacementConstraintViolation` 辅助排查已有非法状态。

Scene 派生类型不能使用 `AddChild/AddComponent`，也不能声明 `ChildOf` 后再调用 `AddScene`。Scene 始终是根类型；需要嵌套作用域时在 Scene 下建立普通 Child Entity。

## 旧引用指向空或解析失败

这是实体销毁、对象池复用，或整个运行时在 `World.Dispose()` 后顺序重启时的预期行为。用 `EntityRef<T>.TryGet` 或 `World.TryResolve` 每次校验，不缓存裸 Entity 并假设永久存活。存档使用领域 ID，重新从业务索引定位。GameEntity 始终按[单 World 契约](README.md#单-world-硬约束)使用。

若异常是 `ObjectDisposedException`，说明调用方保存并继续使用了已经结束的旧 World 引用。不要捕获后重试旧引用；释放它，并在确实要开启下一次完整会话时重新取得 `World.Instance`。`Dispose` 回调中也不能新建 Scene、注册观察者或驱动更新。

## `ReparentTo` 报环或关系未变化

不能挂到自己、自己的后代、未注册节点、已销毁节点、旧 World 节点或非法根节点。失败后 Core 应保持原结构；调用 `ValidateEntities()` 并打印 Issues，确认没有内部结构损坏。不要通过 `new Entity().ReparentTo(owner)` 绕过 `AddChild/AddComponent` 创建事务。

## 更新回调中出现重入异常

`World.Update` 与 `World.FixedUpdate` 使用不可重入的全局 pass。不要在 `IUpdate`、`IFixedUpdate` 或 `IStart` 中再次调用任一 World 更新入口；把追加工作写入队列，在宿主收到当前调用返回后再执行。pass 中新建或迁入后续 Scene 的 Entity 会从下一次对应更新开始运行，这是稳定边界，不是漏调度。

## Unity Hierarchy 与 Entity 树不一致

先以 `CaptureEntitySnapshot` 为准，再检查：

- 是否只有一个有效 `GameEntityRunner`。
- `AutoCreateViews` 是否启用。
- `runner.Registry` 是否已初始化；Runner 晚于 Entity 创建是受支持的，默认会回放已有树。
- 投影 GameObject 是否被外部脚本销毁。
- `Registry.LastError` 是否提示 parent view 缺失。

不要通过手工拖动 Transform 修复；应调用 `ReparentTo` 或重新 `Bind`。

## Framework 尚未初始化

`GameEntry` 强类型属性要求 `FrameworkEntry.IsReady == true`。异步启动流程应等待 `InitializeAsync`，普通组件可在业务启动 Procedure 中访问。可选功能先用 `GameEntry.TryGet<T>`。

## Framework 功能依赖异常

检查 `FrameworkOptions.Features`：Data/Instance/Audio/Localization 需要 Asset；UI 使用实例池时需要 InstancePool；Asset/ResourceUpdate/Scene 需要正确的 YooAsset 模式和参数。

## 真机连不上 Debug Server

- `127.0.0.1` 在手机上指手机自身，改为电脑局域网 IP。
- 设备和电脑应在可互通网络，端口默认 `9527` 未被防火墙拦截。
- Client Token 与服务端输出完全一致。
- URL 路径使用 `/ws/device`，Web Console 使用 `/ws/web`，不能混用。
- 先启动 `--enable-mock=true` 验证服务端与页面，再排查设备连接。

## GPU Terrain 空白或无植被

按顺序检查：Unity/URP 版本、RendererFeature、烘焙 Asset、Authoring 引用、相机、材质/Shader、Compute Shader 支持。打开 F8 Overlay，并读取 `IGpuTerrainSystem.LastError`、`IGpuTerrainVegetationSystem.LastError`；若样例也失败，再运行包内验证入口定位工具链问题。

## 最小问题报告

报告至少附带：

- Core/Unity/包版本与目标平台。
- 最小复现步骤和期望/实际结果。
- 完整异常栈或 Debug Server 日志。
- `CaptureEntitySnapshot()` 的相关节点。
- `ValidateEntities().Issues`。
- Unity 问题附 Runner/Framework 配置；GPU 问题附 F8 Overlay 截图与 RendererData。

[返回知识库首页](README.md)
