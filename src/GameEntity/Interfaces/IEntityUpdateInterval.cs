namespace GameEntity
{
    /// <summary>
    /// 提供 Entity 普通 Update 的最小调用间隔。实现该接口的 Entity 必须同时实现 IUpdate。
    /// </summary>
    public interface IEntityUpdateInterval
    {
        /// <summary>
        /// 两次普通 Update 之间的最小时间，单位为秒。值必须有限且大于等于 0；
        /// 0 表示每次 World.Update 都更新，大于 0 时 Update 接收自上次实际调用起累计的 deltaTime。
        /// </summary>
        float UpdateInterval { get; }
    }
}
