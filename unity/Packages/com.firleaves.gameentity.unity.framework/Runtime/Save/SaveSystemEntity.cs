using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class SaveSystemEntity : Entity, IAwake<SaveSystemConfig, ISaveStorage>, IUpdate, IDestroy, ISaveSystem
    {
        private SaveSystemConfig _config;
        private ISaveStorage _storage;
        private object _currentData;
        private Type _currentDataType;
        private int _currentSlot = -1;
        private bool _isDirty;
        private float _lastDirtyTime;

        public bool IsInitialized { get; private set; }
        public int CurrentSlot => _currentSlot;
        public bool IsDirty => _isDirty;
        public Type CurrentDataType => _currentDataType;

        public event Action<int> Saved;
        public event Action<int> Loaded;
        public event Action<int> Deleted;

        public void Awake(SaveSystemConfig config, ISaveStorage storage)
        {
            _config = config != null ? config.Clone() : SaveSystemConfig.CreateDefault();
            _storage = storage ?? new LocalSaveStorage(_config.SaveFolderName);
            _currentSlot = Mathf.Max(0, _config.DefaultSlot);
            IsInitialized = true;
        }

        public void Update(float deltaTime)
        {
            if (!_isDirty || _config.AutoSaveInterval <= 0f)
            {
                return;
            }

            if (Time.unscaledTime - _lastDirtyTime < _config.AutoSaveInterval)
            {
                return;
            }

            Save();
        }

        public void OnDestroy()
        {
            if (_isDirty && _config != null && _config.SaveOnDestroy)
            {
                Save();
            }

            IsInitialized = false;
            _storage = null;
            _currentData = null;
            _currentDataType = null;
            _config = null;
        }

        public void SetData<T>(T data, int slot = -1)
        {
            if (data == null)
            {
                throw new FrameworkException($"设置存档数据失败：{typeof(T).Name} 不能为空。");
            }

            _currentData = data;
            _currentDataType = typeof(T);
            if (slot >= 0)
            {
                _currentSlot = slot;
            }

            MarkDirty();
        }

        public bool TryGetData<T>(out T data)
        {
            if (_currentData is T typed)
            {
                data = typed;
                return true;
            }

            data = default;
            return false;
        }

        public T GetData<T>()
        {
            if (TryGetData<T>(out var data))
            {
                return data;
            }

            throw new FrameworkException($"当前存档数据不是指定类型：Request={typeof(T).Name}, Current={_currentDataType?.Name ?? "null"}");
        }

        public void MarkDirty()
        {
            _isDirty = true;
            _lastDirtyTime = Time.unscaledTime;
        }

        public void Save(int slot = -1)
        {
            EnsureInitialized();
            if (_currentData == null || _currentDataType == null)
            {
                throw new FrameworkException("保存失败：当前没有存档数据。");
            }

            SaveObject(_currentData, _currentDataType, ResolveSlot(slot));
        }

        public void Save<T>(T data, int slot = -1)
        {
            SetData(data, slot);
            Save(slot);
        }

        public bool Load<T>(int slot, out T data)
        {
            EnsureInitialized();
            data = default;

            if (!_storage.Exists(slot))
            {
                return false;
            }

            var json = _storage.Read(slot);
            if (string.IsNullOrWhiteSpace(json))
            {
                if (_storage.TryRestoreBackup(slot))
                {
                    json = _storage.Read(slot);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }
            }

            if (!TryDeserializeEnvelope(json, slot, out var envelope))
            {
                return false;
            }

            if (!ValidateChecksum(envelope, slot))
            {
                _storage.TryRestoreBackup(slot);
                return false;
            }

            data = JsonUtility.FromJson<T>(envelope.PayloadJson);
            _currentData = data;
            _currentDataType = typeof(T);
            _currentSlot = slot;
            _isDirty = false;
            Loaded?.Invoke(slot);
            return true;
        }

        public bool HasSave(int slot)
        {
            EnsureInitialized();
            return _storage.Exists(slot);
        }

        public void DeleteSave(int slot)
        {
            EnsureInitialized();
            _storage.Delete(slot);
            if (_currentSlot == slot)
            {
                _currentSlot = Mathf.Max(0, _config.DefaultSlot);
                _currentData = null;
                _currentDataType = null;
                _isDirty = false;
            }

            Deleted?.Invoke(slot);
        }

        public SaveSlotInfo[] GetAllSlotInfo()
        {
            EnsureInitialized();
            var maxSlots = Mathf.Max(1, _config.MaxSlots);
            var infos = new List<SaveSlotInfo>(maxSlots);
            for (var i = 0; i < maxSlots; i++)
            {
                var info = new SaveSlotInfo
                {
                    Slot = i,
                    Exists = _storage.Exists(i)
                };

                if (info.Exists)
                {
                    var json = _storage.Read(i);
                    if (!string.IsNullOrWhiteSpace(json) && TryDeserializeEnvelope(json, i, out var envelope))
                    {
                        info.SchemaVersion = envelope.Meta.SchemaVersion;
                        info.TimestampUtc = envelope.Meta.TimestampUtc;
                        info.GameVersion = envelope.Meta.GameVersion;
                        info.DataType = envelope.Meta.DataType;
                    }
                }

                infos.Add(info);
            }

            return infos.ToArray();
        }

        private void SaveObject(object data, Type dataType, int slot)
        {
            var payloadJson = JsonUtility.ToJson(data, _config.PrettyPrint);
            var envelope = new SaveEnvelope
            {
                PayloadJson = payloadJson,
                Meta = new SaveMeta
                {
                    SchemaVersion = 1,
                    TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    GameVersion = Application.version,
                    DataType = dataType.AssemblyQualifiedName ?? dataType.FullName,
                    Checksum = string.Empty
                }
            };

            if (_config.EnableChecksum)
            {
                envelope.Meta.Checksum = ComputeChecksum(BuildChecksumContent(envelope));
            }

            var finalJson = JsonUtility.ToJson(envelope, _config.PrettyPrint);
            _storage.Write(slot, finalJson);
            _currentSlot = slot;
            _isDirty = false;
            Saved?.Invoke(slot);
        }

        private bool TryDeserializeEnvelope(string json, int slot, out SaveEnvelope envelope)
        {
            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                return envelope != null && envelope.Meta != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemEntity] 解析存档失败：Slot={slot}, Error={ex.Message}");
                envelope = null;
                return false;
            }
        }

        private bool ValidateChecksum(SaveEnvelope envelope, int slot)
        {
            if (!_config.EnableChecksum || envelope == null || string.IsNullOrWhiteSpace(envelope.Meta.Checksum))
            {
                return true;
            }

            var savedChecksum = envelope.Meta.Checksum;
            envelope.Meta.Checksum = string.Empty;
            var computedChecksum = ComputeChecksum(BuildChecksumContent(envelope));
            envelope.Meta.Checksum = savedChecksum;

            if (string.Equals(savedChecksum, computedChecksum, StringComparison.Ordinal))
            {
                return true;
            }

            Debug.LogError($"[SaveSystemEntity] 存档校验失败：Slot={slot}");
            return false;
        }

        private int ResolveSlot(int slot)
        {
            var resolved = slot >= 0 ? slot : (_currentSlot >= 0 ? _currentSlot : _config.DefaultSlot);
            return Mathf.Max(0, resolved);
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized || _storage == null)
            {
                throw new FrameworkException("SaveSystem 尚未初始化。");
            }
        }

        private static string BuildChecksumContent(SaveEnvelope envelope)
        {
            return $"{envelope.Meta.SchemaVersion}|{envelope.Meta.TimestampUtc}|{envelope.Meta.GameVersion}|{envelope.Meta.DataType}|{envelope.PayloadJson}";
        }

        private static string ComputeChecksum(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
