using System;

namespace GameEntity.Unity.Framework
{
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
