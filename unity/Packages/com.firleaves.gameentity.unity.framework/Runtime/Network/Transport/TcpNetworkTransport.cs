using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public sealed class TcpNetworkTransport : INetworkTransport
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private byte[] _receiveBuffer;
        private CancellationTokenSource _receiveCts;

        public bool IsConnected => _client != null && _client.Connected;

        public event Action Connected;
        public event Action<NetworkCloseReason, string> Closed;
        public event Action<ArraySegment<byte>> Received;
        public event Action<Exception> Error;

        public async UniTask ConnectAsync(NetworkConnectOptions options, CancellationToken ct = default)
        {
            if (options == null)
            {
                throw new FrameworkException("TCP 连接失败：连接参数不能为空。");
            }

            Close(NetworkCloseReason.Local);

            _client = new TcpClient();
            if (options.ReceiveBufferSize > 0)
            {
                _client.ReceiveBufferSize = options.ReceiveBufferSize;
            }

            if (options.SendBufferSize > 0)
            {
                _client.SendBufferSize = options.SendBufferSize;
            }

            await _client.ConnectAsync(options.Host, options.Port).AsUniTask().AttachExternalCancellation(ct);
            _stream = _client.GetStream();
            _receiveBuffer = new byte[Math.Max(1024, options.ReceiveBufferSize)];
            _receiveCts = new CancellationTokenSource();
            Connected?.Invoke();
            ReceiveLoop(_receiveCts.Token).Forget();
        }

        public void Send(ArraySegment<byte> bytes)
        {
            if (_stream == null || !IsConnected)
            {
                Error?.Invoke(new FrameworkException("TCP 发送失败：连接未建立。"));
                return;
            }

            SendAsync(bytes).Forget();
        }

        public void Close(NetworkCloseReason reason = NetworkCloseReason.Local, string message = null)
        {
            var wasConnected = IsConnected;
            _receiveCts?.Cancel();
            _receiveCts?.Dispose();
            _receiveCts = null;
            _stream?.Dispose();
            _stream = null;
            _client?.Close();
            _client = null;

            if (wasConnected)
            {
                Closed?.Invoke(reason, message);
            }
        }

        public void Tick(float deltaTime)
        {
        }

        private async UniTaskVoid SendAsync(ArraySegment<byte> bytes)
        {
            try
            {
                await _stream.WriteAsync(bytes.Array, bytes.Offset, bytes.Count);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                Close(NetworkCloseReason.Error, ex.Message);
            }
        }

        private async UniTaskVoid ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _stream != null)
                {
                    var count = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, ct);
                    if (count <= 0)
                    {
                        Close(NetworkCloseReason.Remote, "远端关闭连接。");
                        return;
                    }

                    var copy = new byte[count];
                    Buffer.BlockCopy(_receiveBuffer, 0, copy, 0, count);
                    Received?.Invoke(new ArraySegment<byte>(copy));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                Close(NetworkCloseReason.Error, ex.Message);
            }
        }
    }
}
