using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface INetworkChannel
    {
        string Name { get; }
        bool IsConnected { get; }
        NetworkChannelState State { get; }
        float HeartbeatIntervalSeconds { get; set; }
        int MaxMissHeartbeatCount { get; set; }
        float CallTimeoutSeconds { get; set; }
        int MaxPacketSize { get; set; }
        int MissHeartbeatCount { get; }
        int SentPacketCount { get; }
        int ReceivedPacketCount { get; }
        int PendingCallCount { get; }

        event Action<NetworkConnectedEvent> Connected;
        event Action<NetworkClosedEvent> Closed;
        event Action<NetworkErrorEvent> Error;
        event Action<NetworkPacketEvent> PacketReceived;
        event Action<NetworkHeartbeatEvent> HeartbeatMissed;

        UniTask ConnectAsync(string address, CancellationToken ct = default);
        UniTask ConnectAsync(string host, int port, CancellationToken ct = default);
        void Close();
        void Send<TPacket>(TPacket packet);
        UniTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request, CancellationToken ct = default);
        IDisposable Listen<TPacket>(Action<TPacket> handler);
        NetworkChannelSnapshot GetSnapshot();
    }

}
