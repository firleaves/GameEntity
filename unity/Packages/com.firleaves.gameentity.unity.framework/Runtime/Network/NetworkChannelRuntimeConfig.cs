namespace GameEntity.Unity.Framework
{
    internal sealed class NetworkChannelRuntimeConfig
    {
        public NetworkTransportKind Transport;
        public INetworkTransport CustomTransport;
        public INetworkProtocol Protocol;
        public float HeartbeatIntervalSeconds;
        public int MaxMissHeartbeatCount;
        public float CallTimeoutSeconds;
        public int TransportReceiveBufferSize;
        public int TransportSendBufferSize;
        public int InitialReceiveBufferSize;
        public int InitialSendBufferSize;
        public int MaxPacketSize;
        public int MaxBufferRetainSize;
        public bool ClosePendingCallsOnDisconnect;
    }
}
