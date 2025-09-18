# GameEntity

GameEntity是一个轻量级的Unity实体组件系统，基于ET框架的Entity系统提取并增强而来。框架保留了ET的核心实体结构，并增加了异步加载、组件依赖等功能，为Unity游戏开发提供了更加灵活高效的实体管理解决方案。

请查看 [详细文档](./GameEntity说明.md)

## 开发背景

本框架源自对ET框架Entity系统的提取和优化，旨在提供一个专注于实体管理的轻量级解决方案，同时扩展了以下核心功能：
- 增加了基于UniTask的异步加载系统
- 添加了组件间的依赖关系管理
- 自定义Entity更新策略，方便定制不同entity更新频率
- 简化了接入流程，方便快速开发

## 主要功能

- **轻量级实体组件系统**：基于ET框架的简洁高效ECS架构
- **组件依赖管理**：自动处理组件间的依赖关系
- **异步实体加载**：支持基于UniTask的异步初始化和加载组件
- **自定义更新策略** 定制自己的更新策略，比如每秒固定帧数，超出相机范围停止更新等等
- **完整生命周期管理**：Awake、Update、LateUpdate、Destroy等生命周期钩子
- **对象池系统**：高效的对象复用
- **场景管理**：简化场景和实体的组织方式
- **自定义编辑器支持**：方便在Inspector中查看和编辑实体数据

## 拓展模块
- 固定更新指定帧数策略
- 变量变化检测，方便做检测数据变化后，做自己业务的操作 ，比如ui的刷新

## 依赖项

- Cysharp UniTask 2.5.10+
- Unity 2022.3+

## 安装方法

### 方法一：通过Git URL（推荐）

1. 打开Unity项目
2. 打开Package Manager (菜单: Window > Package Manager)
3. 点击左上角的"+"按钮，选择"Add package from git URL..."
4. 输入以下URL：
   ```
   https://github.com/firleaves/GameEntity.git?path=Assets/GameEntity
   ```
5. 点击"Add"按钮

### 方法二：手动导入

1. 下载此仓库的ZIP文件或使用git克隆
2. 将`Assets/GameEntity`文件夹复制到你的Unity项目的Assets文件夹中
3. 确保已安装UniTask包：
   - 打开Package Manager
   - 点击"+"按钮，选择"Add package from git URL..."
   - 输入`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

## 快速入门

### 初始化框架

在场景中创建一个GameObject并添加`GameEntityIniter`组件：

```csharp
// 或者在代码中初始化
GameObject gameObject = new GameObject("GameEntityRoot");
gameObject.AddComponent<GameEntityIniter>();
```

### 创建实体

```csharp
// 创建场景
var gameScene = new GameScene("MainGameScene");
World.Instance.AddScene("MainGameScene", gameScene);

// 创建实体
var player = gameScene.AddChild<PlayerEntity>();
```

### 创建组件

```csharp
// 定义组件
public class HealthComponent : Entity, IAwake
{
    public int MaxHealth = 100;
    public int CurrentHealth;
    
    public void Awake()
    {
        CurrentHealth = MaxHealth;
    }
}

// 添加组件到实体
var health = player.AddComponent<HealthComponent>();
```

### 使用组件依赖

```csharp
// 使用特性声明依赖
[DependsOn(typeof(HealthComponent))]
public class HealthBarComponent : DependentComponentBase, IAwake, IUpdate
{
    protected override void OnActivationChanged(bool isActive)
    {
        // 当依赖状态变化时被调用
        gameObject.SetActive(isActive);
    }
    
    public void Update(float time)
    {
        // 只有当所有依赖满足时才会被调用
        if (AreAllDependenciesMet)
        {
            UpdateHealthBar();
        }
    }
}
```

### 异步实体加载


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

## 致谢

特别感谢ET框架(https://github.com/egametang/ET) 提供的优秀实体组件系统设计，本框架在其基础上进行了提取和增强。


