namespace GE.Extensions
{
    /// <summary>
    /// 固定帧率策略 - 确保实体以固定的频率更新，不受实际帧率影响
    /// </summary>
    public class FixedRateStrategy : IUpdateStrategy
    {
        private readonly float _targetUpdateInterval; // 目标更新间隔时间（秒）
        private float _accumulatedTime = 0f; 

        private readonly bool _useUnscaledTime;

        /// <summary>
        /// 创建固定帧率策略
        /// </summary>
        /// <param name="updatesPerSecond">每秒更新次数</param>
        /// <param name="useUnscaledTime">是否使用 真实时间间隔</param>
        public FixedRateStrategy(float updatesPerSecond,bool useUnscaledTime = false)
        {
            _targetUpdateInterval = 1f / updatesPerSecond;
            _useUnscaledTime = useUnscaledTime;
        }


        public int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime)
        {

            _accumulatedTime += _useUnscaledTime ? unscaledDeltaTime : deltaTime;

            int updateCount = 0;

            if (_accumulatedTime >= _targetUpdateInterval)
            {
                updateCount = (int)(_accumulatedTime / _targetUpdateInterval);
                _accumulatedTime -= updateCount * _targetUpdateInterval;
            }

            singleDeltaTime = _targetUpdateInterval;

            return updateCount;
        }
    }
}