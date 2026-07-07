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

}
