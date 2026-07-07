using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkConnectedEvent
    {
        public readonly INetworkChannel Channel;

        public NetworkConnectedEvent(INetworkChannel channel)
        {
            Channel = channel;
        }
    }

}
