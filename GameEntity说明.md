# GameEntity使用指南

## 目录
1. [框架概述](#框架概述)
2. [核心特性](#核心特性)
3. [系统详解](#系统详解)
4. [推荐实践](#推荐实践)

## 框架概述

GameEntity (GE) 是一个实体组件系统(ECS)框架。它提供了一套完整的游戏对象管理解决方案，包括实体管理、层级（子节点），组件系统、依赖组件、异步处理、对象池化，生命周期（Awake/Update/LateUpdate/Destroy）等核心功能。

## 核心特性
- **场景树结构**: 基于场景树结构管理节点组件，子节点，完整生命周期管理
- **实体-组件系统**: 灵活的ECS架构，支持动态组件管理
- **生命周期管理**： 挂在场景树节点下面有完整的生命周期管理控制
- **自定义更新策略**: 可定制的实体更新机制，比如每秒固定帧数，超出相机范围停止更新等等
- **高性能对象池**: 线程安全的无锁对象池实现
- **组件依赖管理**: 自动化的组件依赖管理
- **异步实体加载**: 基于UniTask的异步实体加载


### 调试
- **Unity深度集成**: 每个Entity自动创建GameObject，Inspector界面显示Entity内部数据 


## 系统详解

### 0.启动与基础概念
- 启动：在首个场景放置 `GameEntityIniter`（MonoBehaviour）。它会设置 `World.Instance.Root`、注册单例（`EntitySystem`、`ObjectPool`、`IdGenerator`、`TimeInfo`）并初始化依赖系统。
- 实体可视：每个 `Entity` 会生成一个 `ViewGO` 并挂载 `ComponentView`，层级与父子关系同步，便于在 Inspector 查看。
- 生命周期： 跟随GameEntityIniter Awake ，Update，LateUpdate，Destroy控制World的生命周期

### 1. World - 全局世界容器

World是整个框架的核心容器，管理所有的单例系统和场景。

```csharp
// 获取World实例（单例）
World world = World.Instance;

// 添加单例系统
world.AddSingleton<EntitySystem>();
world.AddSingleton<ObjectPool>();

// 场景管理
Scene scene = new MyScene("MainScene");
world.AddScene("MainScene", scene);
```

### 2.Scene - 场景树

- 新增业务场景：继承 `GE.Scene`，在构造函数指定名称
- 添加 `World.Instance.AddScene(name, scene)` 注册。
- 获取场景 World.Instance.GetScene(name)
- 移除场景 World.Instance.RemoveScene(name)

注意：Scene 不支持 ILifecycle（IAwake，IUpdate，ILateUpdate，IDestroy）里面生命周期接口继承使用
```csharp
public class MainScene : GE.Scene 
{ 
    public MainScene(): base("Main") 
    {
    } 
    public override void Awake() 
    { 
    } 
}

World.Instance.AddScene("Main", new MainScene());
World.Instance.GetScene("Main");
World.Instance.RemoveScene("Main");
```
### 3.Entity- 实体基类,子节点与组件

Entity是所有游戏对象的基类，支持父子关系和组件管理。
- Entity的创建都是基于其他Entity去创建它，比如 Scene创建第一个节点 ，其他Entity对象创建
- InstanceId 代表Entity的唯一ID，由IdGenerator生成，用来对比entity是否是一个，可以参考EntityRef实现
- Id，可以由IdGenerator，也可以外部传入，是属于业务Id,`AddComponentWithId`和`AddChildWithId`就可以传入业务Id
- 子节点：`AddChild<T,T1...>(...)`,如果Entity实现了IAwake，会调用Awake函数，Entity实现IUpdate，会加入Update系统里面，进行更新，同一个Parent下面支持多个Child Entity。
- 组件：同实体同类型仅一个。常用：`AddComponent<T>(...)`、`GetComponent<T>()`、`RemoveComponent<T>()`。
```csharp
public class MainScene : GE.Scene 
{ 
    public MainScene(): base("Main") 
    {

        
    } 
    public override void Awake() 
    { 
        AddComponent<ComponentA>();
        var b = AddComponent<ComponentB>();

        var c = b.AddChild<ComponentC>();
        c.AddComponent<ComponentD>();
    } 
}


//其他地方创建一个child挂在场景下面的一个子节点，
var scene = World.Instance.GetScene("Main") as MainScene;
scene.AddChild<ComponentC>();

```


### 4.生命周期和接口

实体只要挂在场景树下面，都可以自动管理生命周期，不必担心悬空对象无法释放问题,如果


ILifecycle下面定义了生命周期接口

- IAwake ： 支持多个泛型类型
- IUpdate ： `void Update(float time);`参数为时间差,unity环境下面为unscaledDeltaTime
- ILateUpdate： 和unity  lateupdate触发规则一样
- IDestroy ： Entity销毁的时候会触发，用来清理entity内部数据

```csharp
public class MyComponent : Entity, IAwake, IAwake<int>, IUpdate, ILateUpdate, IDestroy
{
    public void Awake()
    {
        // 无参数初始化
    }

    public void Awake(int parameter)
    {
    }

    public void Update(float deltaTime)
    {
    }

    public void LateUpdate()
    {
        // 延迟更新
    }

    public void OnDestroy()
    {
        // 销毁时清理
    }
}
```

#### 自定义更新策略
实现 `IHasUpdateStrategy` 返回自定义 `IUpdateStrategy` 可改变调用频率；也可组合 `AllStrategy/AnyStrategy`。

- IUpdateStrategy: 用来告诉 EntitySystem 触发多少次Entity的Update
- IHasUpdateStrategy：在Entity实现这个接口，改变更新频率
- AllStrategy/AnyStrategy： 组合多种更新策略


```csharp

public class FixedRateStrategy : IUpdateStrategy 
{
    public FixedRateStrategy(float updatesPerSecond,bool useUnscaledTime = false){}
    public int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime){}
}

public class InCamreraStrategy : IUpdateStrategy 
{
    public InCamreraStrategy(int inCameraCount,int outCameraCount){}

    public int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime){}
}

public class MyEntity : Entity, IUpdate, IHasUpdateStrategy
{
    private IUpdateStrategy _strategy;

    public void Awake()
    {
        // 所有子策略都满足时才更新，取最小更新次数
        // 相机范围内，每秒30次
        // 相机范围外，每秒5次
        _strategy = new AllStrategy(
            new FixedRateStrategy(30), // 每秒固定30次
            new InCamreraStrategy(60，5)     // 在相机范围内才能每秒60次，不在相机范围内每秒5s
        );
    }

    public IUpdateStrategy GetUpdateStrategy() => _strategy;

    public void Update(float deltaTime)
    {
        // 更新逻辑
    }
}
```

### 5.依赖组件管理
- 标注依赖：在组件类上添加 `[DependsOn(typeof(Foo), typeof(Bar))]`，或实现 `IDependentComponent.GetDependencyTypes()`。或者 继承 DependentComponentBase
- 依赖组件条件满足，OnDependencyStatusChanged会被调用，AreAllDependenciesMet变量也会改变
- Update，依赖条件成立，如果Entity实现了IUpdate接口，EntitySystem才会触发Entity的Update，如果条件不满足，就不会出触发Update。
- 查询：`entity.AreDependenciesMet<T>()` 可检查某组件依赖是否满足。


### 6.异步实体（依赖UniTask插件）
- 继承 `AsyncEntity` 并实现 `OnLoadAsync`，通过 `AddComponentAsync<T>(...)` 或 `AddChildAsync<T>(...)` 添加。加载成功后才会注册更新并触发依赖处理，取消/异常会自动移除组件。

```csharp
public class AsyncResourceLoader : AsyncEntity, IAwake<string>
{
    private string _resourcePath;

    public void Awake(string resourcePath)
    {
        _resourcePath = resourcePath;
    }

    protected override async UniTask OnLoadAsync(CancellationToken cancelToken)
    {
        // 异步加载资源
        await LoadResourceAsync(_resourcePath, cancelToken);
    }

    protected override void OnLoaded()
    {
        // 加载完成后的处理
        GELog.Info($"Resource loaded: {_resourcePath}");
    }

    private async UniTask LoadResourceAsync(string path, CancellationToken token)
    {
        // 模拟异步加载
        await UniTask.Delay(1000, cancellationToken: token);
    }
}

// 使用异步组件
var loader = await entity.AddComponentAsync<AsyncResourceLoader, string>("path/to/resource");
```

### 6.Entity 使用对象池与释放
- 大多创建 API 有 `isFromPool` 参数；若为 `true`，实例来自 `ObjectPool`。
- 调用 `Dispose()` 会：解绑视图、释放子节点与组件、通知系统 `Destroy`、将对象回收到池。

### 7.ObjectPool
- Fetch<T> : 从对象池获得对象
- Recycle ：回收对象到内存池

```csharp
var hashset = ObjectPool.Instance.Fetch<HashSet<string>>();
ObjectPool.Instance.Recycle(hashset);
```

## 推荐实践

### 1. 职责单一的组件，多用组合，代替继承
```csharp
// 推荐：职责单一的组件
public class HealthComponent : Entity, IAwake<int>
{
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public void Awake(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
}

// 推荐：组合多个简单组件
public class CharacterEntity : Entity, IAwake
{
    public void Awake()
    {
        AddComponent<HealthComponent, int>(100);
        AddComponent<MovementComponent>();
        AddComponent<CombatComponent>(); // 依赖于Health和Movement
    }
}
```

### 2.对于多态行为，使用接口拓展，Entity作为容器持有引用

```csharp
public interface IAction
{
    void OnInit();
    void OnExecute();
    void OnDestroy();
}
// 推荐：组合多个简单组件
public class ActionEntity : Entity, IAwake<IAction>,IUpdate,IDestroy
{
    private IAction _actioin;

    public void Awake(IAction action)
    {
       _actioin = action
       _action?.OnInit();
    }

    public void Update(float deltaTime)
    {
        _action?.OnExecute();
    }

    public void OnDestroy()
    {
        // 销毁时清理
        _action?.OnExecute();
        _action = null;
    }
}
```



