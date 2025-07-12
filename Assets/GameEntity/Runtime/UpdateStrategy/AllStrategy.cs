namespace GE
{
    /// <summary>
    /// 所有策略都满足时才调用update
    /// update count 所有子策略的最小值
    /// singleDeltaTime 所有子策略的最小deltaTime
    /// </summary>
    public class AllStrategy : IUpdateStrategy
    {
        private readonly IUpdateStrategy[] _strategies;

        public AllStrategy(params IUpdateStrategy[] strategies)
        {
            _strategies = strategies;
        }

        public int GetUpdateCount(Entity entity, float deltaTime,float unscaledDeltaTime, out float singleDeltaTime)
        {
            int minCount = int.MaxValue;
            float minDelta = float.MaxValue;

            foreach (var strategy in _strategies)
            {
                float childDelta;
                int count = strategy.GetUpdateCount(entity, deltaTime,unscaledDeltaTime, out childDelta);
                if (count < minCount)
                    minCount = count;
                if (childDelta < minDelta)
                    minDelta = childDelta;
            }

            // 如果有任何策略不更新，则整体不更新
            if (minCount == 0)
            {
                singleDeltaTime = minDelta;
                return 0;
            }

            singleDeltaTime = minDelta;
            return minCount;
        }
    }
}