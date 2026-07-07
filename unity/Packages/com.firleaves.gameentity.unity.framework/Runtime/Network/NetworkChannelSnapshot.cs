using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkChannelSnapshot
    {
        public string Name;
        public NetworkChannelState State;
        public bool IsConnected;
        public int SentPacketCount;
        public int ReceivedPacketCount;
        public int PendingCallCount;
        public int MissHeartbeatCount;
        public float HeartbeatElapsedSeconds;
        public float LastReceiveElapsedSeconds;
    }

}
