using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkSystemSnapshot
    {
        public DateTime CapturedAtUtc;
        public int ChannelCount;
        public IReadOnlyList<NetworkChannelSnapshot> Channels;
    }

}
