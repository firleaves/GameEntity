using System;

namespace GameEntity.Unity.Framework
{
    public readonly struct NetworkProtocolProfile
    {
        public readonly int HeaderLength;
        public readonly int MaxPacketSize;
        public readonly int InitialReceiveBufferSize;
        public readonly int InitialSendBufferSize;
        public readonly int MaxBufferRetainSize;

        public NetworkProtocolProfile(
            int headerLength,
            int maxPacketSize,
            int initialReceiveBufferSize,
            int initialSendBufferSize,
            int maxBufferRetainSize)
        {
            HeaderLength = headerLength;
            MaxPacketSize = maxPacketSize;
            InitialReceiveBufferSize = initialReceiveBufferSize;
            InitialSendBufferSize = initialSendBufferSize;
            MaxBufferRetainSize = maxBufferRetainSize;
        }

        public static NetworkProtocolProfile CreateDefault(int headerLength)
        {
            return new NetworkProtocolProfile(
                headerLength,
                1024 * 1024,
                64 * 1024,
                8 * 1024,
                256 * 1024);
        }
    }

}
