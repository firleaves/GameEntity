using System;

namespace GameEntity.Unity.Framework
{
    internal sealed class NetworkReceiveBuffer
    {
        private byte[] _buffer;
        private readonly int _initialCapacity;
        private readonly int _maxRetainSize;
        private int _length;

        public NetworkReceiveBuffer(int capacity, int maxRetainSize)
        {
            _initialCapacity = Math.Max(1024, capacity);
            _maxRetainSize = Math.Max(_initialCapacity, maxRetainSize);
            _buffer = new byte[_initialCapacity];
        }

        public void Append(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null || bytes.Count <= 0)
            {
                return;
            }

            EnsureCapacity(_length + bytes.Count);
            Buffer.BlockCopy(bytes.Array, bytes.Offset, _buffer, _length, bytes.Count);
            _length += bytes.Count;
        }

        public bool TryReadPacket(INetworkProtocol protocol, int maxPacketSize, out ArraySegment<byte> packet)
        {
            packet = default;
            if (protocol == null || _length < protocol.PacketHeaderLength)
            {
                return false;
            }

            var reader = new NetworkBufferReader(new ArraySegment<byte>(_buffer, 0, _length));
            if (!protocol.TryGetPacketLength(reader, out var packetLength) ||
                packetLength <= 0)
            {
                return false;
            }

            var packetLimit = Math.Max(1024, maxPacketSize);
            if (packetLength > packetLimit)
            {
                throw new FrameworkException($"网络包过大：{packetLength}/{packetLimit}");
            }

            if (_length < packetLength)
            {
                return false;
            }

            var copy = new byte[packetLength];
            Buffer.BlockCopy(_buffer, 0, copy, 0, packetLength);
            var remaining = _length - packetLength;
            if (remaining > 0)
            {
                Buffer.BlockCopy(_buffer, packetLength, _buffer, 0, remaining);
            }

            _length = remaining;
            TrimExcessIfNeeded();
            packet = new ArraySegment<byte>(copy);
            return true;
        }

        public void Clear()
        {
            _length = 0;
            TrimExcessIfNeeded();
        }

        private void EnsureCapacity(int capacity)
        {
            if (_buffer.Length >= capacity)
            {
                return;
            }

            var next = _buffer.Length;
            while (next < capacity)
            {
                next *= 2;
            }

            Array.Resize(ref _buffer, next);
        }

        private void TrimExcessIfNeeded()
        {
            if (_length == 0 && _buffer.Length > _maxRetainSize)
            {
                _buffer = new byte[_initialCapacity];
            }
        }
    }
}
