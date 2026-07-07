using System;

namespace GameEntity.Unity.Framework
{
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

}
