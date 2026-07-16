# API 速查

> 本页只列业务侧常用公开入口。`EntityHierarchy`、Scheduler、ObjectPool 等当前为内部实现，不应从业务程序集直接调用。

## 建模选择

```text
独立运行时对象   → Child Entity
owner 的唯一能力 → Component Entity
状态/配置/集合记录 → 普通数据容器
```

默认使用普通数据；只有存在独立生命周期、调度、迁移、Handle/EntityRef、更新要求/Ready 状态或子树需求时才提升为 Entity。完整规范见[Entity 与普通数据的建模边界](02-entity-vs-data.md)。

## World 与 Scene

```csharp
World.Instance.AddScene(name, scene);
World.Instance.GetScene(name);
World.Instance.RemoveScene(name);
World.Instance.FixedUpdate(fixedDeltaTime);
World.Instance.Update(deltaTime);
World.Instance.TryResolve(handle, out MyEntity entity);
World.Instance.CaptureEntitySnapshot();
World.Instance.ValidateEntities();
World.Instance.ObserveEntities(observer, replayExisting: true);
World.Instance.Dispose(); // 仅整个运行时退出、测试隔离或完整重启
```

`World` 是 `sealed` 单例，不能 `new`。整个进程同一时刻只使用唯一的 `World.Instance`；多个玩法、对局和 Unity Scene 统一放在它下面的多个 GameEntity Scene 中，不创建、缓存或切换多个 World。`Dispose` 结束整个运行时会话，保存的旧引用随后对全部公开实例入口抛出 `ObjectDisposedException`；重新访问 `World.Instance` 只用于顺序开启全新会话。Scene key 必须等于 `Scene.Name`；同名 Scene 不能重复注册；构造但未注册的 Scene 不能挂载实体。默认复用现有 Scene，只有 World 下可独立整体销毁的顶层运行时作用域才新建 Scene，详见[Scene 的创建与边界规范](02-scene-boundaries.md)。

## 创建

```csharp
owner.AddChild<T>();
owner.AddChild<T, P1>(p1);                  // 最多 4 个参数
owner.AddChildWithId<T, P1>(id, p1);
owner.AddPooledChild<T, P1>(p1);

owner.AddComponent<T>();
owner.AddComponent<T, P1>(p1);              // 最多 4 个参数
owner.AddComponentWithId<T, P1>(id, p1);
owner.AddPooledComponent<T, P1>(p1);
```

约束：泛型类型必须实现对应参数签名的 `IAwake`；同一 owner 上同类型 Component 只能有一个。Entity/Scene 在 `Awake` 或 `RegisterSystem` 中结束自身生命时创建失败并抛出，不返回已销毁或已回池对象。

```csharp
[ChildOf]                         // 只能作为 Child
[ChildOf(typeof(UnitEntity))]     // 且 owner 必须是 UnitEntity 或派生类型
```

`ChildOf` 同时约束创建与 `ReparentTo`；声明它的类型不能 `AddComponent`。Scene 派生类型只能通过 `World.AddScene` 成为 SceneRoot，不能作为 Child/Component；Scene 类型也不能声明 `ChildOf` 后再注册为根。Core 没有 `ComponentOf`。

## 查询

```csharp
entity.Parent;
entity.Owner;
entity.Children;
entity.Components;
entity.ChildrenCount();
entity.ComponentsCount();

entity.GetChild<T>(id);
entity.TryGetChild(id, out T child);
entity.ContainsChild(id);

entity.GetComponent<T>();
entity.TryGetComponent(out T component);
entity.ContainsComponent<T>();

entity.GetSceneRoot();
entity.TryGetSceneRoot(out Scene scene);
entity.TryGetOwner(out T directOwner);
entity.TryFindOwner(out T ancestorOwner);
entity.TryGetComponentInParent(out T component);
entity.TryGetComponentInAncestors(out T component);
entity.TryGetSiblingComponent(out T component);
```

全部 Component 查询、存在性判断和移除 API 按精确运行时类型匹配。基类/接口多态查找必须显式遍历 `Components`，并自行处理多个派生 Component 同时匹配的歧义。

## 结构修改与销毁

```csharp
entity.ReparentTo(newOwner);
entity.Destroy();
owner.RemoveChild(childId);
owner.ClearChildren();
owner.RemoveComponent<T>();
```

结构修改由层级内核同步处理 Scene 分区、索引和调度；不要直接操作投影视图。`ReparentTo` 只接受当前层级中仍存活的节点，不能用于挂入 `new Entity()` 或复活已销毁对象。`Destroy` 是非虚模板入口，业务释放逻辑实现 `IDestroy.OnDestroy()`。

## 生命周期

```csharp
IAwake
IAwake<T1>
IAwake<T1, T2>
IAwake<T1, T2, T3>
IAwake<T1, T2, T3, T4>
IStart
IFixedUpdate
IUpdate
IDestroy
IEntityReadyState
IEntityUpdateState
IEntityUpdateInterval
```

Scene Root 不参与 Scheduler。Core 当前没有 `IEnable/IDisable`、`IUnscaledUpdate`、`ILateUpdate` 或 `IAsyncAwake`；不要在业务代码中假设这些接口存在。

## 树观察

```csharp
IDisposable registration = World.Instance.ObserveEntities(observer);
registration.Dispose();
```

默认同步回放已提交节点，顺序为父到子；新节点只在创建事务成功后发布。用于 Unity/调试 adapter，不用于替代业务事件总线。

## 引用

```csharp
EntityRef<T> reference = entity;
reference.TryGet(out T alive);
reference.IsAlive;
reference.ValueOrNull;
reference.Handle;

EntityHandle handle = entity.Handle;
handle.IsValid;
handle.NodeId;
```

`Id`、`InstanceId` 和 `Handle` 都由 Core 写入，业务只读；指定 Child ID 使用 `AddChildWithId`。

## 更新要求

```csharp
[RequireForUpdate(typeof(RequiredComponent))]
public sealed class Dependent : Entity, IAwake, IStart, IUpdate
{
    public void Awake() { }
    public void Start() { }
    public void Update(float deltaTime) { }
}

EntityValidationResult result = World.Instance.ValidateEntities();
```

要求范围为同一 owner 的精确 Component 类型；要求的 Component 不存在或未 Ready 时跳过 `Start/FixedUpdate/Update`。`Awake` 始终立即执行。`RequireForUpdate` 只允许用于 Component，不提供自动添加、注入或状态变化回调；声明环会在注册时拒绝并回滚创建。

## Unity

```csharp
runner.Registry.Bind(entity, gameObject);
runner.Registry.TryGetView(entity, out ComponentView view);
runner.Registry.Unbind(entity);
```

Runner 通常自动投影，无需业务手动调用；即使 Runner 晚于 Entity 创建，也会回放已有树。

## Framework

```csharp
GameEntry.Asset;
GameEntry.Data;
GameEntry.ResourceUpdate;
GameEntry.Scene;
GameEntry.Instance;
GameEntry.Audio;
GameEntry.Timer;
GameEntry.Event;
GameEntry.Localization;
GameEntry.Settings;
GameEntry.UI;
GameEntry.Save;
GameEntry.Procedure;
GameEntry.Network;

GameEntry.Get<T>();
GameEntry.TryGet(out T service);
GameEntry.Has<T>();
GameEntry.HasFeature(feature);
```

只有 `FrameworkEntry.IsReady` 后才可使用强类型静态属性。

[返回知识库首页](README.md)
