using System;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkBufferWriter
    {
        private byte[] _buffer;
        private int _length;

        public NetworkBufferWriter(int capacity = 1024)
        {
            _buffer = new byte[Math.Max(16, capacity)];
        }

        public int Length => _length;

        public void Clear()
        {
            _length = 0;
        }

        public void Write(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            Write(bytes, 0, bytes.Length);
        }

        public void Write(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (offset < 0 || count < 0 || offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EnsureCapacity(_length + count);
            Buffer.BlockCopy(bytes, offset, _buffer, _length, count);
            _length += count;
        }

        public ArraySegment<byte> ToSegment()
        {
            return new ArraySegment<byte>(_buffer, 0, _length);
        }

        public byte[] ToArray()
        {
            var result = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, result, 0, _length);
            return result;
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
    }

    public readonly struct NetworkBufferReader
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _count;

        public NetworkBufferReader(ArraySegment<byte> segment)
        {
            _buffer = segment.Array;
            _offset = segment.Offset;
            _count = segment.Count;
        }

        public NetworkBufferReader(byte[] bytes)
        {
            _buffer = bytes;
            _offset = 0;
            _count = bytes != null ? bytes.Length : 0;
        }

        public int Count => _count;
        internal byte[] RawBuffer => _buffer;
        internal int Offset => _offset;

        public byte[] ToArray()
        {
            if (_buffer == null || _count <= 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[_count];
            System.Buffer.BlockCopy(_buffer, _offset, result, 0, _count);
            return result;
        }
    }
}
