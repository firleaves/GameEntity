namespace GameEntity
{
    /// <summary>
    /// 引擎无关的实体树观察者，由 Unity / 调试工具等外层 adapter 通过 World.ObserveEntities 订阅。
    /// </summary>
    public interface IEntityTreeObserver
    {
        /// <summary>
        /// Entity 已完成 Awake、结构挂接与调度注册后调用。此时 Handle、Owner 和 SceneRoot 均可读取。
        /// </summary>
        void OnEntityRegistered(Entity entity);

        void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent);

        void OnEntityDestroyed(Entity entity);
    }
}
