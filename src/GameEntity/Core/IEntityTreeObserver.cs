namespace GameEntity
{
    /// <summary>
    /// 引擎无关的实体树观察者，由 Unity / 调试工具等外层 adapter 订阅。
    /// </summary>
    public interface IEntityTreeObserver
    {
        void OnEntityRegistered(Entity entity);

        void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent);

        void OnEntityDestroyed(Entity entity);
    }
}
