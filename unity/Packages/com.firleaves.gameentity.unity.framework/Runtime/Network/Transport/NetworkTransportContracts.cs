using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkConnectOptions
    {
        public string Address;
        public string Host;
        public int Port;
        public int ReceiveBufferSize;
        public int SendBufferSize;
    }

    public interface INetworkTransport
    {
        bool IsConnected { get; }

        event Action Connected;
        event Action<NetworkCloseReason, string> Closed;
        event Action<ArraySegment<byte>> Received;
        event Action<Exception> Error;

        UniTask ConnectAsync(NetworkConnectOptions options, CancellationToken ct = default);
        void Send(ArraySegment<byte> bytes);
        void Close(NetworkCloseReason reason = NetworkCloseReason.Local, string message = null);
        void Tick(float deltaTime);
    }
}
