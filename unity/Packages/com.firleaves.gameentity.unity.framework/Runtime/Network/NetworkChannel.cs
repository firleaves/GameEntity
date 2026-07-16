using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    internal sealed class NetworkChannel : INetworkChannel
    {
        private readonly NetworkChannelRuntimeConfig _config;
        private readonly INetworkTransport _transport;
        private readonly INetworkProtocol _protocol;
        private readonly NetworkPacketRouter _router = new NetworkPacketRouter();
        private readonly NetworkCallBox _callBox = new NetworkCallBox();
        private readonly NetworkBufferWriter _writer;
        private readonly NetworkReceiveBuffer _receiveBuffer;
        private float _heartbeatElapsedSeconds;
        private float _lastReceiveElapsedSeconds;
        private int _missHeartbeatCount;

        public NetworkChannel(string name, NetworkChannelRuntimeConfig config)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new FrameworkException("创建网络频道失败：name 不能为空。");
            }

            _config = config ?? throw new FrameworkException("创建网络频道失败：config 不能为空。");
            _protocol = _config.Protocol ?? throw new FrameworkException("创建网络频道失败：Protocol 不能为空。");
            _transport = NetworkTransportFactory.Create(_config);
            _writer = new NetworkBufferWriter(Math.Max(1024, _config.InitialSendBufferSize));
            _receiveBuffer = new NetworkReceiveBuffer(
                Math.Max(1024, _config.InitialReceiveBufferSize),
                _config.MaxBufferRetainSize);
            Name = name;
            HeartbeatIntervalSeconds = _config.HeartbeatIntervalSeconds;
            MaxMissHeartbeatCount = _config.MaxMissHeartbeatCount;
            CallTimeoutSeconds = _config.CallTimeoutSeconds;
            MaxPacketSize = _config.MaxPacketSize;
            BindTransport();
        }

        public string Name { get; }
        public bool IsConnected => _transport.IsConnected;
        public NetworkChannelState State { get; private set; } = NetworkChannelState.Closed;
        public int SentPacketCount { get; private set; }
        public int ReceivedPacketCount { get; private set; }
        public float HeartbeatIntervalSeconds { get; set; }
        public int MaxMissHeartbeatCount { get; set; }
        public float CallTimeoutSeconds { get; set; }
        public int MaxPacketSize { get; set; }
        public int MissHeartbeatCount => _missHeartbeatCount;
        public int PendingCallCount => _callBox.Count;

        public event Action<NetworkConnectedEvent> Connected;
        public event Action<NetworkClosedEvent> Closed;
        public event Action<NetworkErrorEvent> Error;
        public event Action<NetworkPacketEvent> PacketReceived;
        public event Action<NetworkHeartbeatEvent> HeartbeatMissed;

        public UniTask ConnectAsync(string address, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new FrameworkException("网络连接失败：address 不能为空。");
            }

            return ConnectInternalAsync(new NetworkConnectOptions
            {
                Address = address,
                Host = address,
                ReceiveBufferSize = _config.TransportReceiveBufferSize,
                SendBufferSize = _config.TransportSendBufferSize
            }, ct);
        }

        public UniTask ConnectAsync(string host, int port, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new FrameworkException("网络连接失败：host 不能为空。");
            }

            if (port <= 0)
            {
                throw new FrameworkException("网络连接失败：port 必须大于 0。");
            }

            return ConnectInternalAsync(new NetworkConnectOptions
            {
                Host = host,
                Port = port,
                Address = $"{host}:{port}",
                ReceiveBufferSize = _config.TransportReceiveBufferSize,
                SendBufferSize = _config.TransportSendBufferSize
            }, ct);
        }

        public void Close()
        {
            if (State == NetworkChannelState.Closed)
            {
                return;
            }

            State = NetworkChannelState.Closing;
            _transport.Close(NetworkCloseReason.Local);
            State = NetworkChannelState.Closed;
        }

        public void Send<TPacket>(TPacket packet)
        {
            if (packet == null)
            {
                throw new FrameworkException("网络发送失败：packet 不能为空。");
            }

            if (!_transport.IsConnected)
            {
                throw new FrameworkException($"网络发送失败：频道 {Name} 未连接。");
            }

            if (!_protocol.TryEncode(packet, _writer))
            {
                throw new FrameworkException($"网络发送失败：协议无法编码 {packet.GetType().Name}。");
            }

            _transport.Send(_writer.ToSegment());
            SentPacketCount++;
            _writer.TrimExcess(_config.MaxBufferRetainSize);
        }

        public UniTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new FrameworkException("网络请求失败：request 不能为空。");
            }

            var task = _callBox.Add<TResponse>(
                request,
                _protocol,
                CallTimeoutSeconds,
                ct);
            try
            {
                Send(request);
            }
            catch (Exception ex)
            {
                if (request is INetworkRequest networkRequest)
                {
                    _callBox.Fail(networkRequest.RpcId, ex);
                }

                throw;
            }

            return task;
        }

        public IDisposable Listen<TPacket>(Action<TPacket> handler)
        {
            return _router.Listen(handler);
        }

        public NetworkChannelSnapshot GetSnapshot()
        {
            return new NetworkChannelSnapshot
            {
                Name = Name,
                State = State,
                IsConnected = IsConnected,
                SentPacketCount = SentPacketCount,
                ReceivedPacketCount = ReceivedPacketCount,
                PendingCallCount = _callBox.Count,
                MissHeartbeatCount = _missHeartbeatCount,
                HeartbeatElapsedSeconds = _heartbeatElapsedSeconds,
                LastReceiveElapsedSeconds = _lastReceiveElapsedSeconds
            };
        }

        public void Update(float deltaTime)
        {
            var dt = Math.Max(0f, deltaTime);
            _transport.Update(dt);
            _callBox.Update(dt);

            if (!_transport.IsConnected || HeartbeatIntervalSeconds <= 0f)
            {
                return;
            }

            _heartbeatElapsedSeconds += dt;
            _lastReceiveElapsedSeconds += dt;
            if (_heartbeatElapsedSeconds < HeartbeatIntervalSeconds)
            {
                return;
            }

            _heartbeatElapsedSeconds = 0f;
            var heartbeat = _protocol.CreateHeartbeatPacket();
            if (heartbeat != null)
            {
                Send(heartbeat);
            }

            if (_lastReceiveElapsedSeconds >= HeartbeatIntervalSeconds)
            {
                _missHeartbeatCount++;
                HeartbeatMissed?.Invoke(new NetworkHeartbeatEvent(this, _missHeartbeatCount));
                if (MaxMissHeartbeatCount > 0 &&
                    _missHeartbeatCount >= MaxMissHeartbeatCount)
                {
                    _transport.Close(NetworkCloseReason.HeartbeatTimeout, "心跳超时。");
                }
            }
        }

        public void Shutdown()
        {
            _router.Clear();
            _callBox.CancelAll(NetworkCloseReason.Shutdown);
            _receiveBuffer.Clear();
            _transport.Close(NetworkCloseReason.Shutdown);
        }

        private async UniTask ConnectInternalAsync(NetworkConnectOptions connectOptions, CancellationToken ct)
        {
            if (State == NetworkChannelState.Connecting)
            {
                throw new FrameworkException($"网络连接失败：频道 {Name} 正在连接。");
            }

            if (_transport.IsConnected)
            {
                return;
            }

            State = NetworkChannelState.Connecting;
            try
            {
                await _transport.ConnectAsync(connectOptions, ct);
            }
            catch
            {
                State = NetworkChannelState.Closed;
                throw;
            }
        }

        private void BindTransport()
        {
            _transport.Connected += OnTransportConnected;
            _transport.Closed += OnTransportClosed;
            _transport.Received += OnTransportReceived;
            _transport.Error += OnTransportError;
        }

        private void OnTransportConnected()
        {
            State = NetworkChannelState.Connected;
            _heartbeatElapsedSeconds = 0f;
            _lastReceiveElapsedSeconds = 0f;
            _missHeartbeatCount = 0;
            Connected?.Invoke(new NetworkConnectedEvent(this));
        }

        private void OnTransportClosed(NetworkCloseReason reason, string message)
        {
            State = NetworkChannelState.Closed;
            if (_config.ClosePendingCallsOnDisconnect)
            {
                _callBox.CancelAll(reason);
            }

            Closed?.Invoke(new NetworkClosedEvent(this, reason, message));
        }

        private void OnTransportReceived(ArraySegment<byte> bytes)
        {
            _lastReceiveElapsedSeconds = 0f;
            _missHeartbeatCount = 0;
            try
            {
                _receiveBuffer.Append(bytes);
                while (_receiveBuffer.TryReadPacket(_protocol, MaxPacketSize, out var packetBytes))
                {
                    DecodeAndDispatch(packetBytes);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(new NetworkErrorEvent(this, ex, ex.Message));
                _transport.Close(NetworkCloseReason.Error, ex.Message);
            }
        }

        private void DecodeAndDispatch(ArraySegment<byte> packetBytes)
        {
            if (!_protocol.TryDecode(new NetworkBufferReader(packetBytes), out var packet, out var header))
            {
                Error?.Invoke(new NetworkErrorEvent(
                    this,
                    null,
                    $"网络解码失败：频道={Name}，字节数={packetBytes.Count}。"));
                return;
            }

            ReceivedPacketCount++;
            if (_protocol.IsHeartbeat(packet))
            {
                return;
            }

            PacketReceived?.Invoke(new NetworkPacketEvent(this, packet, header.PacketId));
            if (_protocol.TryGetResponseId(packet, out var rpcId) &&
                _callBox.TrySetResponse(packet, rpcId))
            {
                return;
            }

            _router.Dispatch(packet);
        }

        private void OnTransportError(Exception exception)
        {
            Error?.Invoke(new NetworkErrorEvent(
                this,
                exception,
                exception != null ? exception.Message : "网络传输错误。"));
        }
    }
}
