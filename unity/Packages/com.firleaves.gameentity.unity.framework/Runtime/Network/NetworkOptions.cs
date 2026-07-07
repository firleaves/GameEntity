using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class NetworkOptions
    {
        public int MaxChannelCount = 8;
        public int ReceiveBufferSize = 64 * 1024;
        public int SendBufferSize = 64 * 1024;
        public float HeartbeatIntervalSeconds = 10f;
        public int MaxMissHeartbeatCount = 3;
        public float CallTimeoutSeconds = 10f;
        public bool ClosePendingCallsOnDisconnect = true;

        public NetworkOptions Clone()
        {
            return new NetworkOptions
            {
                MaxChannelCount = MaxChannelCount,
                ReceiveBufferSize = ReceiveBufferSize,
                SendBufferSize = SendBufferSize,
                HeartbeatIntervalSeconds = HeartbeatIntervalSeconds,
                MaxMissHeartbeatCount = MaxMissHeartbeatCount,
                CallTimeoutSeconds = CallTimeoutSeconds,
                ClosePendingCallsOnDisconnect = ClosePendingCallsOnDisconnect
            };
        }

        public static NetworkOptions CreateDefault()
        {
            return new NetworkOptions();
        }
    }

    public sealed class NetworkChannelOptions
    {
        public NetworkTransportKind Transport = NetworkTransportKind.Tcp;
        public INetworkTransport CustomTransport;
        public INetworkProtocol Protocol;
        public float? HeartbeatIntervalSeconds;
        public int? MaxMissHeartbeatCount;
        public float? CallTimeoutSeconds;
        public int? ReceiveBufferSize;
        public int? SendBufferSize;

        internal NetworkChannelRuntimeOptions ToRuntimeOptions(NetworkOptions defaults)
        {
            var source = defaults ?? NetworkOptions.CreateDefault();
            return new NetworkChannelRuntimeOptions
            {
                Transport = Transport,
                CustomTransport = CustomTransport,
                Protocol = Protocol,
                HeartbeatIntervalSeconds = HeartbeatIntervalSeconds ?? source.HeartbeatIntervalSeconds,
                MaxMissHeartbeatCount = MaxMissHeartbeatCount ?? source.MaxMissHeartbeatCount,
                CallTimeoutSeconds = CallTimeoutSeconds ?? source.CallTimeoutSeconds,
                ReceiveBufferSize = ReceiveBufferSize ?? source.ReceiveBufferSize,
                SendBufferSize = SendBufferSize ?? source.SendBufferSize,
                ClosePendingCallsOnDisconnect = source.ClosePendingCallsOnDisconnect
            };
        }
    }

    internal sealed class NetworkChannelRuntimeOptions
    {
        public NetworkTransportKind Transport;
        public INetworkTransport CustomTransport;
        public INetworkProtocol Protocol;
        public float HeartbeatIntervalSeconds;
        public int MaxMissHeartbeatCount;
        public float CallTimeoutSeconds;
        public int ReceiveBufferSize;
        public int SendBufferSize;
        public bool ClosePendingCallsOnDisconnect;
    }
}
