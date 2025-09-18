#changlog

##  2025-9-18
- Entity分离Awake和Update，没有实现IAwake，只实现IUpdate也有能Update
- 异步组件先触发Awake，加载完成后，才会执行自己的Update
- 优化Unity Inspector界面显示，支持显示属性，支持更多类型显示
- 增加使用说明
