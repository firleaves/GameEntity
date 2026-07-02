namespace GameEntity
{
    /// <summary>
    /// 实体生命周期门控。core 只读取状态，不管理异步加载、取消或资源释放。
    /// </summary>
    public interface IEntityLifecycleGate
    {
        /// <summary>
        /// 当前实体是否已经完成运行前准备，可被依赖系统视为可用。
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 当前实体是否允许执行 Update。
        /// </summary>
        bool CanRun { get; }
    }
}
