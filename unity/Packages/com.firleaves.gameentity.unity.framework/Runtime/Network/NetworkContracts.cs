using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public enum NetworkChannelState
    {
        Closed,
        Connecting,
        Connected,
        Closing
    }

    public enum NetworkTransportKind
    {
        Tcp,
        Mock,
        Custom
    }

    public enum NetworkCloseReason
    {
        Local,
        Remote,
        Error,
        HeartbeatTimeout,
        Shutdown
    }

    public interface INetworkPacket
    {
    }

    public interface INetworkRequest : INetworkPacket
    {
        int RpcId { get; set; }
    }

    public interface INetworkResponse : INetworkPacket
    {
        int RpcId { get; set; }
        int ErrorCode { get; set; }
        string ErrorMessage { get; set; }
    }

    public interface INetworkSystem
    {
        INetworkChannel CreateChannel(string name, NetworkChannelOptions options);
        bool TryGetChannel(string name, out INetworkChannel channel);
        bool DestroyChannel(string name);
        void CloseAll();
        NetworkSystemSnapshot GetSnapshot();
    }

    public interface INetworkChannel
    {
        string Name { get; }
        bool IsConnected { get; }
        NetworkChannelState State { get; }

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
