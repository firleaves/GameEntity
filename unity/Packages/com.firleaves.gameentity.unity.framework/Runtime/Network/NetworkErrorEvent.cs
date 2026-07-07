using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkErrorEvent
    {
        public readonly INetworkChannel Channel;
        public readonly Exception Exception;
        public readonly string Message;

        public NetworkErrorEvent(INetworkChannel channel, Exception exception, string message)
        {
            Channel = channel;
            Exception = exception;
            Message = message;
        }
    }

}
