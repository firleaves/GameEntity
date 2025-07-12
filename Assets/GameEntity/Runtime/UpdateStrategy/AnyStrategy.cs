namespace GE
{
    /// <summary>
    /// 任意一个策略满足时调用udpate
    /// update count 所有子策略的最大值
    /// singleDeltaTime 所有子策略的最小deltaTime
    /// </summary>
    public class AnyStrategy : IUpdateStrategy
    {
        private readonly IUpdateStrategy[] _strategies;

        public AnyStrategy(params IUpdateStrategy[] strategies)
        {
            _strategies = strategies;
        }

        public int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime)
        {
            int maxCount = 0;
            float minDelta = float.MaxValue;

            foreach (var strategy in _strategies)
            {
                float childDelta;
                int count = strategy.GetUpdateCount(entity, deltaTime,unscaledDeltaTime, out childDelta);
                if (count > maxCount)
                    maxCount = count;
                if (count > 0 && childDelta < minDelta)
                    minDelta = childDelta;
            }

            if (maxCount == 0)
            {
                singleDeltaTime = minDelta;
                return 0;
            }

            singleDeltaTime = minDelta;
            return maxCount;
        }
    }
}