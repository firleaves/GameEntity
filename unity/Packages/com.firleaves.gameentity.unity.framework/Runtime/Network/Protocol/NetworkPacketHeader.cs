using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkPacketHeader
    {
        public readonly int PacketId;
        public readonly int RpcId;
        public readonly int BodyLength;

        public NetworkPacketHeader(int packetId, int rpcId, int bodyLength)
        {
            PacketId = packetId;
            RpcId = rpcId;
            BodyLength = bodyLength;
        }
    }

}
