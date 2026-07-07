using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkClosedEvent
    {
        public readonly INetworkChannel Channel;
        public readonly NetworkCloseReason Reason;
        public readonly string Message;

        public NetworkClosedEvent(INetworkChannel channel, NetworkCloseReason reason, string message)
        {
            Channel = channel;
            Reason = reason;
            Message = message;
        }
    }

}
