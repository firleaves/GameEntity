using System;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkChannelConfig
    {
        public INetworkProtocol Protocol;
        public float? HeartbeatIntervalSeconds;
        public int? MaxMissHeartbeatCount;
        public float? CallTimeoutSeconds;
        public int? TransportReceiveBufferSize;
        public int? TransportSendBufferSize;
        public int? InitialReceiveBufferSize;
        public int? InitialSendBufferSize;
        public int? MaxPacketSize;
        public int? MaxBufferRetainSize;
        public INetworkTransport CustomTransport;

        public static NetworkChannelConfig CreateDefault()
        {
            return new NetworkChannelConfig();
        }

        internal NetworkChannelRuntimeConfig ToRuntimeConfig(
            NetworkTransportKind transport,
            INetworkProtocol defaultProtocol,
            NetworkOptions defaults)
        {
            var source = defaults ?? NetworkOptions.CreateDefault();
            var protocol = Protocol ?? defaultProtocol;
            if (protocol == null)
            {
                throw new FrameworkException("创建网络频道失败：Protocol 不能为空，请先设置默认协议或传入频道协议。");
            }

            var profile = protocol.Profile;
            var initialReceiveBufferSize = InitialReceiveBufferSize
                ?? PositiveOrDefault(source.InitialReceiveBufferSize, profile.InitialReceiveBufferSize);
            var initialSendBufferSize = InitialSendBufferSize
                ?? PositiveOrDefault(source.InitialSendBufferSize, profile.InitialSendBufferSize);
            var maxPacketSize = MaxPacketSize
                ?? PositiveOrDefault(source.MaxPacketSize, profile.MaxPacketSize);
            var maxBufferRetainSize = MaxBufferRetainSize
                ?? PositiveOrDefault(source.MaxBufferRetainSize, profile.MaxBufferRetainSize);

            return new NetworkChannelRuntimeConfig
            {
                Transport = transport,
                CustomTransport = CustomTransport,
                Protocol = protocol,
                HeartbeatIntervalSeconds = HeartbeatIntervalSeconds ?? source.HeartbeatIntervalSeconds,
                MaxMissHeartbeatCount = MaxMissHeartbeatCount ?? source.MaxMissHeartbeatCount,
                CallTimeoutSeconds = CallTimeoutSeconds ?? source.CallTimeoutSeconds,
                TransportReceiveBufferSize = TransportReceiveBufferSize ?? source.TransportReceiveBufferSize,
                TransportSendBufferSize = TransportSendBufferSize ?? source.TransportSendBufferSize,
                InitialReceiveBufferSize = initialReceiveBufferSize,
                InitialSendBufferSize = initialSendBufferSize,
                MaxPacketSize = maxPacketSize,
                MaxBufferRetainSize = maxBufferRetainSize,
                ClosePendingCallsOnDisconnect = source.ClosePendingCallsOnDisconnect
            };
        }

        private static int PositiveOrDefault(int value, int defaultValue)
        {
            return value > 0 ? value : defaultValue;
        }
    }

}
