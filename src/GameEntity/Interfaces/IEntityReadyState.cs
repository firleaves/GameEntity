namespace GameEntity
{
    /// <summary>
    /// 提供 Entity 的运行前就绪状态，供更新要求判断该 Entity 是否可用。
    /// </summary>
    public interface IEntityReadyState
    {
        /// <summary>
        /// 当前 Entity 是否已经完成运行前准备，可以满足其他 Entity 的更新要求。
        /// 该值不控制当前 Entity 自身的 Start、FixedUpdate 或 Update。
        /// </summary>
        bool IsReady { get; }
    }
}
