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

    public interface INetworkProtocol
    {
        int PacketHeaderLength { get; }
        bool TryGetPacketLength(NetworkBufferReader reader, out int packetLength);
        bool TryEncode<TPacket>(TPacket packet, NetworkBufferWriter writer);
        bool TryDecode(NetworkBufferReader reader, out object packet, out NetworkPacketHeader header);
        int GetPacketId(Type packetType);
        int GetPacketId(object packet);
        bool TrySetRequestId(object packet, int rpcId);
        bool TryGetResponseId(object packet, out int rpcId);
        bool IsHeartbeat(object packet);
        object CreateHeartbeatPacket();
    }
}
