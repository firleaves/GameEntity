using System;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkBufferWriter
    {
        private byte[] _buffer;
        private readonly int _initialCapacity;
        private int _length;

        public NetworkBufferWriter(int capacity = 1024)
        {
            _initialCapacity = Math.Max(16, capacity);
            _buffer = new byte[_initialCapacity];
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

        public void TrimExcess(int maxRetainSize)
        {
            if (maxRetainSize <= 0 || _buffer.Length <= maxRetainSize)
            {
                return;
            }

            _buffer = new byte[_initialCapacity];
            _length = 0;
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

}
