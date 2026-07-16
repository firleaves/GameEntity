namespace GameEntity
{
    /// <summary>
    /// 提供 Entity 是否参与 World 更新生命周期的状态。
    /// </summary>
    public interface IEntityUpdateState
    {
        /// <summary>
        /// Scheduler 当前是否允许该 Entity 进入 Start、FixedUpdate 和 Update。
        /// 该值只作用于当前 Entity，不传播到 Child 或 Component。
        /// </summary>
        bool IsUpdateEnabled { get; }
    }
}
