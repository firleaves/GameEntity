using System;

namespace GameEntity.Unity.Framework
{
    public interface INetworkProtocol
    {
        NetworkProtocolProfile Profile { get; }
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
