using System;

namespace GameEntity.Unity.Framework
{
    public interface ISaveStorage
    {
        bool Exists(int slot);
        string Read(int slot);
        void Write(int slot, string json);
        void Delete(int slot);
        bool TryRestoreBackup(int slot);
    }

    public interface ISaveSystem
    {
        bool IsInitialized { get; }
        int CurrentSlot { get; }
        bool IsDirty { get; }
        Type CurrentDataType { get; }

        event Action<int> Saved;
        event Action<int> Loaded;
        event Action<int> Deleted;

        void SetData<T>(T data, int slot = -1);
        bool TryGetData<T>(out T data);
        T GetData<T>();
        void MarkDirty();
        void Save(int slot = -1);
        void Save<T>(T data, int slot = -1);
        bool Load<T>(int slot, out T data);
        bool HasSave(int slot);
        void DeleteSave(int slot);
        SaveSlotInfo[] GetAllSlotInfo();
    }

    [Serializable]
    public sealed class SaveSystemConfig
    {
        public int MaxSlots = 3;
        public int DefaultSlot;
        public float AutoSaveInterval = 1f;
        public bool EnableChecksum;
        public bool PrettyPrint = true;
        public bool SaveOnDestroy = true;
        public string SaveFolderName = "saves";

        public SaveSystemConfig Clone()
        {
            return new SaveSystemConfig
            {
                MaxSlots = MaxSlots,
                DefaultSlot = DefaultSlot,
                AutoSaveInterval = AutoSaveInterval,
                EnableChecksum = EnableChecksum,
                PrettyPrint = PrettyPrint,
                SaveOnDestroy = SaveOnDestroy,
                SaveFolderName = SaveFolderName
            };
        }

        public static SaveSystemConfig CreateDefault()
        {
            return new SaveSystemConfig();
        }
    }

    [Serializable]
    public sealed class SaveEnvelope
    {
        public SaveMeta Meta = new SaveMeta();
        public string PayloadJson = string.Empty;
    }

    [Serializable]
    public sealed class SaveMeta
    {
        public int SchemaVersion = 1;
        public long TimestampUtc;
        public string GameVersion = string.Empty;
        public string DataType = string.Empty;
        public string Checksum = string.Empty;
    }

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
