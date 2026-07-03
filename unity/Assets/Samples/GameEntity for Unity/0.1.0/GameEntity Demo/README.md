# GameEntity for Unity Demo

在 Package Manager 中导入 `GameEntity Demo` 示例后，打开
`Assets/Samples/GameEntity.Unity/0.1.0/GameEntityDemo/GameEntityDemo.unity`，点击 Play。

运行后查看 Unity Hierarchy：

```text
GameEntity Demo Root
  DemoScene
    Player
      Stats
      Inventory
      Companion
        Stats
    Monster
      Stats
```

3 秒后，示例会通过 `Entity.ReparentTo` 把 `Monster` 挂到 `Player` 下。
Unity Hierarchy 只是调试投影，真实父子关系仍由 GameEntity core 维护。

入口脚本：`Assets/Samples/GameEntity.Unity/0.1.0/GameEntityDemo/Scripts/GameEntityDemo.cs`
