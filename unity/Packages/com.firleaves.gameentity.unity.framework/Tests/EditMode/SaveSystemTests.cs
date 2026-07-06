using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class SaveSystemTests
    {
        [Test]
        public void SaveAndLoad_RestoresData()
        {
            var storage = new MemorySaveStorage();
            var save = CreateSaveSystem(storage, checksum: true);
            var data = new TestSaveData { Level = 7, Name = "Knight" };

            save.Save(data, slot: 1);

            Assert.IsTrue(save.Load<TestSaveData>(1, out var loaded));
            Assert.AreEqual(7, loaded.Level);
            Assert.AreEqual("Knight", loaded.Name);
            Assert.IsFalse(save.IsDirty);
        }

        [Test]
        public void DeleteSave_RemovesSlotAndRaisesEvent()
        {
            var storage = new MemorySaveStorage();
            var save = CreateSaveSystem(storage, checksum: false);
            var deletedSlot = -1;
            save.Deleted += slot => deletedSlot = slot;
            save.Save(new TestSaveData { Level = 1, Name = "A" }, slot: 2);

            save.DeleteSave(2);

            Assert.IsFalse(save.HasSave(2));
            Assert.AreEqual(2, deletedSlot);
        }

        [Test]
        public void Load_ReturnsFalse_WhenChecksumInvalid()
        {
            var storage = new MemorySaveStorage();
            var save = CreateSaveSystem(storage, checksum: true);
            save.Save(new TestSaveData { Level = 3, Name = "Valid" }, slot: 0);
            storage.Corrupt(0, "Valid", "Invalid");

            LogAssert.Expect(LogType.Error, "[SaveSystemEntity] 存档校验失败：Slot=0");
            Assert.IsFalse(save.Load<TestSaveData>(0, out var loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void GetAllSlotInfo_ReturnsExistingSlotMetadata()
        {
            var storage = new MemorySaveStorage();
            var save = CreateSaveSystem(storage, checksum: false);
            save.Save(new TestSaveData { Level = 5, Name = "Meta" }, slot: 1);

            var infos = save.GetAllSlotInfo();

            Assert.AreEqual(3, infos.Length);
            Assert.IsTrue(infos[1].Exists);
            Assert.AreEqual(typeof(TestSaveData).AssemblyQualifiedName, infos[1].DataType);
        }

        private static SaveSystemEntity CreateSaveSystem(ISaveStorage storage, bool checksum)
        {
            var save = new SaveSystemEntity();
            save.Awake(new SaveSystemConfig
            {
                MaxSlots = 3,
                DefaultSlot = 0,
                EnableChecksum = checksum,
                PrettyPrint = false,
                SaveOnDestroy = false
            }, storage);
            return save;
        }

        [Serializable]
        private sealed class TestSaveData
        {
            public int Level;
            public string Name;
        }

        private sealed class MemorySaveStorage : ISaveStorage
        {
            private readonly Dictionary<int, string> _slots = new Dictionary<int, string>();
            private readonly Dictionary<int, string> _backups = new Dictionary<int, string>();

            public bool Exists(int slot)
            {
                return _slots.ContainsKey(slot);
            }

            public string Read(int slot)
            {
                return _slots.TryGetValue(slot, out var json) ? json : null;
            }

            public void Write(int slot, string json)
            {
                if (_slots.TryGetValue(slot, out var existing))
                {
                    _backups[slot] = existing;
                }

                _slots[slot] = json;
            }

            public void Delete(int slot)
            {
                _slots.Remove(slot);
                _backups.Remove(slot);
            }

            public bool TryRestoreBackup(int slot)
            {
                if (!_backups.TryGetValue(slot, out var backup))
                {
                    return false;
                }

                _slots[slot] = backup;
                return true;
            }

            public void Corrupt(int slot, string oldValue, string newValue)
            {
                _slots[slot] = _slots[slot].Replace(oldValue, newValue);
            }
        }
    }
}
