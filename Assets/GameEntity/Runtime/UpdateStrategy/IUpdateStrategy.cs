

namespace GE
{
    /// <summary>
    /// update 策略
    /// </summary>
    public interface IUpdateStrategy
    {
        /// <summary>
        /// 获得更新次数
        /// </summary>
        int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime);
    }


    public interface IHasUpdateStrategy
    {
        /// <summary>
        /// 获得更新策略
        /// </summary>
        IUpdateStrategy GetUpdateStrategy();
    }
}
