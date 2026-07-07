using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class NetworkOptions
    {
        public int MaxChannelCount = 8;
        public int TransportReceiveBufferSize = 64 * 1024;
        public int TransportSendBufferSize = 64 * 1024;
        public int InitialReceiveBufferSize = 64 * 1024;
        public int InitialSendBufferSize = 64 * 1024;
        public int MaxPacketSize = 1024 * 1024;
        public int MaxBufferRetainSize = 256 * 1024;
        public float HeartbeatIntervalSeconds = 10f;
        public int MaxMissHeartbeatCount = 3;
        public float CallTimeoutSeconds = 10f;
        public bool ClosePendingCallsOnDisconnect = true;

        public NetworkOptions Clone()
        {
            return new NetworkOptions
            {
                MaxChannelCount = MaxChannelCount,
                TransportReceiveBufferSize = TransportReceiveBufferSize,
                TransportSendBufferSize = TransportSendBufferSize,
                InitialReceiveBufferSize = InitialReceiveBufferSize,
                InitialSendBufferSize = InitialSendBufferSize,
                MaxPacketSize = MaxPacketSize,
                MaxBufferRetainSize = MaxBufferRetainSize,
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
}
