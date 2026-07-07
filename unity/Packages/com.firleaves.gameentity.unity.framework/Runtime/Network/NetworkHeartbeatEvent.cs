using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkHeartbeatEvent
    {
        public readonly INetworkChannel Channel;
        public readonly int MissCount;

        public NetworkHeartbeatEvent(INetworkChannel channel, int missCount)
        {
            Channel = channel;
            MissCount = missCount;
        }
    }

}
