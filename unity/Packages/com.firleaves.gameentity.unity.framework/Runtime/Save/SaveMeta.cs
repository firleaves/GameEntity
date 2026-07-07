using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class SaveMeta
    {
        public int SchemaVersion = 1;
        public long TimestampUtc;
        public string GameVersion = string.Empty;
        public string DataType = string.Empty;
        public string Checksum = string.Empty;
    }

}
