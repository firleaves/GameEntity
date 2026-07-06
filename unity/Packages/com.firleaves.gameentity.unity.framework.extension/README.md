# GameEntity Unity Framework Extension

`GameEntity.Unity.Framework.Extension` 用来承载暂时不进入 Framework core 的可选生产化能力。

当前先放入 3C 生产化课程需要的输入服务：

- `IInputSystem`
- `InputSystemEntity`
- `IFrameworkInputSource`
- `FrameworkExtensionLegacyInputSource`
- `SimulatedInputSource`

这样做的边界是：

- `GameEntity.Unity.Framework` 不直接依赖具体输入后端。
- Unity 输入采样仍在 MonoBehaviour 桥接层。
- GameEntity 运行时只消费语义化 `FrameworkInputFrame`。
- 后续确认稳定后，再决定是否把某些契约上升到 Framework core。
