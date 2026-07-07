using System;

namespace GameEntity.Unity.Framework
{
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

}
