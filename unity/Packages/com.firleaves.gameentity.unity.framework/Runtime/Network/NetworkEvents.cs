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

    public readonly struct NetworkPacketEvent
    {
        public readonly INetworkChannel Channel;
        public readonly object Packet;
        public readonly int PacketId;

        public NetworkPacketEvent(INetworkChannel channel, object packet, int packetId)
        {
            Channel = channel;
            Packet = packet;
            PacketId = packetId;
        }
    }

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
