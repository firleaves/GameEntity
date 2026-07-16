using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public sealed class MockNetworkTransport : INetworkTransport
    {
        private readonly Queue<ArraySegment<byte>> _sentPackets = new Queue<ArraySegment<byte>>();

        public bool IsConnected { get; private set; }
        public int SentPacketCount => _sentPackets.Count;

        public event Action Connected;
        public event Action<NetworkCloseReason, string> Closed;
        public event Action<ArraySegment<byte>> Received;
        public event Action<Exception> Error;

        public UniTask ConnectAsync(NetworkConnectOptions options, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            IsConnected = true;
            Connected?.Invoke();
            return UniTask.CompletedTask;
        }

        public void Send(ArraySegment<byte> bytes)
        {
            if (!IsConnected)
            {
                Error?.Invoke(new FrameworkException("MockNetworkTransport 发送失败：连接未建立。"));
                return;
            }

            var copy = new byte[bytes.Count];
            if (bytes.Array != null && bytes.Count > 0)
            {
                Buffer.BlockCopy(bytes.Array, bytes.Offset, copy, 0, bytes.Count);
            }

            _sentPackets.Enqueue(new ArraySegment<byte>(copy));
        }

        public bool TryDequeueSent(out ArraySegment<byte> bytes)
        {
            if (_sentPackets.Count > 0)
            {
                bytes = _sentPackets.Dequeue();
                return true;
            }

            bytes = default;
            return false;
        }

        public void Receive(ArraySegment<byte> bytes)
        {
            if (!IsConnected)
            {
                return;
            }

            Received?.Invoke(bytes);
        }

        public void Fail(Exception exception)
        {
            Error?.Invoke(exception);
        }

        public void Close(NetworkCloseReason reason = NetworkCloseReason.Local, string message = null)
        {
            if (!IsConnected)
            {
                return;
            }

            IsConnected = false;
            Closed?.Invoke(reason, message);
        }

        public void Update(float deltaTime)
        {
        }
    }
}
