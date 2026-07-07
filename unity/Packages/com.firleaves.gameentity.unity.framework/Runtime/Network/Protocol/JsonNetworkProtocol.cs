using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class JsonNetworkProtocol : INetworkProtocol
    {
        private const int HeaderLength = 12;
        private readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
        private readonly Dictionary<int, Type> _idToType = new Dictionary<int, Type>();
        private readonly Encoding _encoding = Encoding.UTF8;

        public JsonNetworkProtocol(int heartbeatPacketId = 1)
        {
            if (heartbeatPacketId > 0)
            {
                Register<NetworkHeartbeatPacket>(heartbeatPacketId);
            }
        }

        public int PacketHeaderLength => HeaderLength;

        public JsonNetworkProtocol Register<TPacket>(int packetId)
        {
            return Register(typeof(TPacket), packetId);
        }

        public JsonNetworkProtocol Register(Type packetType, int packetId)
        {
            if (packetType == null)
            {
                throw new FrameworkException("注册网络协议包失败：packetType 不能为空。");
            }

            if (packetId <= 0)
            {
                throw new FrameworkException("注册网络协议包失败：packetId 必须大于 0。");
            }

            if (_idToType.TryGetValue(packetId, out var existed) && existed != packetType)
            {
                throw new FrameworkException($"注册网络协议包失败：PacketId 重复：{packetId}");
            }

            _typeToId[packetType] = packetId;
            _idToType[packetId] = packetType;
            return this;
        }

        public bool TryGetPacketLength(NetworkBufferReader reader, out int packetLength)
        {
            packetLength = 0;
            if (reader.Count < HeaderLength || reader.RawBuffer == null)
            {
                return false;
            }

            var bodyLength = BitConverter.ToInt32(reader.RawBuffer, reader.Offset + 8);
            if (bodyLength < 0)
            {
                return false;
            }

            packetLength = HeaderLength + bodyLength;
            return true;
        }

        public bool TryEncode<TPacket>(TPacket packet, NetworkBufferWriter writer)
        {
            if (packet == null || writer == null)
            {
                return false;
            }

            var packetId = GetPacketId(packet);
            if (packetId <= 0)
            {
                return false;
            }

            var rpcId = packet is INetworkRequest request
                ? request.RpcId
                : packet is INetworkResponse response ? response.RpcId : 0;
            var json = JsonUtility.ToJson(packet);
            var body = _encoding.GetBytes(json);

            writer.Clear();
            WriteInt(writer, packetId);
            WriteInt(writer, rpcId);
            WriteInt(writer, body.Length);
            writer.Write(body);
            return true;
        }

        public bool TryDecode(NetworkBufferReader reader, out object packet, out NetworkPacketHeader header)
        {
            packet = null;
            header = default;
            var bytes = reader.ToArray();
            if (bytes.Length < HeaderLength)
            {
                return false;
            }

            var packetId = BitConverter.ToInt32(bytes, 0);
            var rpcId = BitConverter.ToInt32(bytes, 4);
            var bodyLength = BitConverter.ToInt32(bytes, 8);
            if (packetId <= 0 || bodyLength < 0 || bytes.Length < HeaderLength + bodyLength)
            {
                return false;
            }

            if (!_idToType.TryGetValue(packetId, out var packetType))
            {
                return false;
            }

            var json = _encoding.GetString(bytes, HeaderLength, bodyLength);
            packet = JsonUtility.FromJson(json, packetType);
            if (packet is INetworkRequest request)
            {
                request.RpcId = rpcId;
            }
            else if (packet is INetworkResponse response)
            {
                response.RpcId = rpcId;
            }

            header = new NetworkPacketHeader(packetId, rpcId, bodyLength);
            return packet != null;
        }

        public int GetPacketId(Type packetType)
        {
            if (packetType == null)
            {
                return 0;
            }

            return _typeToId.TryGetValue(packetType, out var packetId) ? packetId : 0;
        }

        public int GetPacketId(object packet)
        {
            return packet != null ? GetPacketId(packet.GetType()) : 0;
        }

        public bool TrySetRequestId(object packet, int rpcId)
        {
            if (packet is INetworkRequest request)
            {
                request.RpcId = rpcId;
                return true;
            }

            return false;
        }

        public bool TryGetResponseId(object packet, out int rpcId)
        {
            if (packet is INetworkResponse response && response.RpcId > 0)
            {
                rpcId = response.RpcId;
                return true;
            }

            rpcId = 0;
            return false;
        }

        public bool IsHeartbeat(object packet)
        {
            return packet is NetworkHeartbeatPacket;
        }

        public object CreateHeartbeatPacket()
        {
            return new NetworkHeartbeatPacket();
        }

        private static void WriteInt(NetworkBufferWriter writer, int value)
        {
            writer.Write(BitConverter.GetBytes(value));
        }
    }

    [Serializable]
    public sealed class NetworkHeartbeatPacket : INetworkPacket
    {
    }
}
