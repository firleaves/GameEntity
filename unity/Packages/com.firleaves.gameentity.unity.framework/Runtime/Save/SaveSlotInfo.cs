using System;

namespace GameEntity.Unity.Framework
{
    public struct SaveSlotInfo
    {
        public int Slot;
        public bool Exists;
        public int SchemaVersion;
        public long TimestampUtc;
        public string GameVersion;
        public string DataType;
    }

}
