using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class NetworkSystemTests
    {
        private JsonNetworkProtocol _protocol;
        private NetworkSystemEntity _network;

        [SetUp]
        public void SetUp()
        {
            _protocol = new JsonNetworkProtocol()
                .Register<LoginRequest>(100)
                .Register<LoginResponse>(101)
                .Register<GoldChangedPacket>(102);
            _network = new NetworkSystemEntity();
            _network.Awake(NetworkOptions.CreateDefault());
            _network.SetDefaultProtocol(_protocol);
        }

        [TearDown]
        public void TearDown()
        {
            _network.OnDestroy();
        }

        [Test]
        public async UniTask Listen_DispatchesReceivedPacket()
        {
            var transport = new MockNetworkTransport();
            var channel = CreateMockChannel(transport);
            await channel.ConnectAsync("mock://local");

            var received = 0;
            channel.Listen<GoldChangedPacket>(packet => received = packet.Gold);

            transport.Receive(Encode(new GoldChangedPacket { Gold = 120 }));

            Assert.AreEqual(120, received);
        }

        [Test]
        public async UniTask CallAsync_ReturnsMatchedResponse()
        {
            var transport = new MockNetworkTransport();
            var channel = CreateMockChannel(transport);
            await channel.ConnectAsync("mock://local");

            var task = channel.CallAsync<LoginRequest, LoginResponse>(
                new LoginRequest { Account = "Player001" });

            Assert.IsTrue(transport.TryDequeueSent(out var sentBytes));
            Assert.IsTrue(_protocol.TryDecode(new NetworkBufferReader(sentBytes), out var sentPacket, out _));
            var request = (LoginRequest)sentPacket;
            Assert.Greater(request.RpcId, 0);

            transport.Receive(Encode(new LoginResponse
            {
                RpcId = request.RpcId,
                PlayerId = 7,
                Nickname = "Player001"
            }));

            var response = await task;
            Assert.AreEqual(7, response.PlayerId);
            Assert.AreEqual("Player001", response.Nickname);
        }

        [Test]
        public async UniTask CallAsync_TimesOut_WhenNoResponse()
        {
            var transport = new MockNetworkTransport();
            var channel = CreateMockChannel(transport, callTimeoutSeconds: 0.5f);
            await channel.ConnectAsync("mock://local");

            var task = channel.CallAsync<LoginRequest, LoginResponse>(
                new LoginRequest { Account = "Timeout" });

            _network.Update(0.25f);
            _network.Update(0.25f);

            Assert.ThrowsAsync<TimeoutException>(async () => await task);
        }

        [Test]
        public async UniTask Disconnect_CancelsPendingCall()
        {
            var transport = new MockNetworkTransport();
            var channel = CreateMockChannel(transport);
            await channel.ConnectAsync("mock://local");

            var task = channel.CallAsync<LoginRequest, LoginResponse>(
                new LoginRequest { Account = "Disconnect" });

            transport.Close(NetworkCloseReason.Remote, "test");

            Assert.ThrowsAsync<FrameworkException>(async () => await task);
        }

        [Test]
        public async UniTask HeartbeatTimeout_ClosesChannel()
        {
            var transport = new MockNetworkTransport();
            var channel = CreateMockChannel(
                transport,
                heartbeatIntervalSeconds: 0.5f,
                maxMissHeartbeatCount: 2);
            await channel.ConnectAsync("mock://local");

            var closedReason = NetworkCloseReason.Local;
            channel.Closed += evt => closedReason = evt.Reason;

            _network.Update(0.5f);
            _network.Update(0.5f);

            Assert.AreEqual(NetworkCloseReason.HeartbeatTimeout, closedReason);
            Assert.IsFalse(channel.IsConnected);
        }

        [Test]
        public void CloseAll_RemovesChannels()
        {
            CreateMockChannel(new MockNetworkTransport());
            CreateMockChannel(new MockNetworkTransport());

            _network.CloseAll();

            Assert.AreEqual(0, _network.GetSnapshot().ChannelCount);
        }

        [Test]
        public void CreateMockChannel_UsesDefaultProtocol()
        {
            var channel = _network.CreateMockChannel(Guid.NewGuid().ToString("N"));

            Assert.AreEqual(NetworkChannelState.Closed, channel.State);
            Assert.AreEqual(10f, channel.CallTimeoutSeconds);
        }

        [Test]
        public void ChannelRuntimeSettings_CanBeChanged()
        {
            var channel = CreateMockChannel(new MockNetworkTransport());

            channel.HeartbeatIntervalSeconds = 2f;
            channel.MaxMissHeartbeatCount = 5;
            channel.CallTimeoutSeconds = 3f;
            channel.MaxPacketSize = 4096;

            Assert.AreEqual(2f, channel.HeartbeatIntervalSeconds);
            Assert.AreEqual(5, channel.MaxMissHeartbeatCount);
            Assert.AreEqual(3f, channel.CallTimeoutSeconds);
            Assert.AreEqual(4096, channel.MaxPacketSize);
        }

        private INetworkChannel CreateMockChannel(
            MockNetworkTransport transport,
            float callTimeoutSeconds = 10f,
            float heartbeatIntervalSeconds = 10f,
            int maxMissHeartbeatCount = 3)
        {
            return _network.CreateChannel(Guid.NewGuid().ToString("N"), NetworkTransportKind.Custom, new NetworkChannelConfig
            {
                CustomTransport = transport,
                CallTimeoutSeconds = callTimeoutSeconds,
                HeartbeatIntervalSeconds = heartbeatIntervalSeconds,
                MaxMissHeartbeatCount = maxMissHeartbeatCount
            });
        }

        private ArraySegment<byte> Encode<TPacket>(TPacket packet)
        {
            var writer = new NetworkBufferWriter();
            Assert.IsTrue(_protocol.TryEncode(packet, writer));
            return writer.ToSegment();
        }

        [Serializable]
        private sealed class LoginRequest : INetworkRequest
        {
            public int RpcId { get; set; }
            public string Account;
        }

        [Serializable]
        private sealed class LoginResponse : INetworkResponse
        {
            public int RpcId { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public int PlayerId;
            public string Nickname;
        }

        [Serializable]
        private sealed class GoldChangedPacket : INetworkPacket
        {
            public int Gold;
        }
    }
}
