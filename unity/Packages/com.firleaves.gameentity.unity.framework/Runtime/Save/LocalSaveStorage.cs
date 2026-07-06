using System;
using System.IO;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class LocalSaveStorage : ISaveStorage
    {
        private readonly string _savePath;

        public LocalSaveStorage(string folderName)
        {
            var safeFolderName = string.IsNullOrWhiteSpace(folderName) ? "saves" : folderName;
            _savePath = Path.Combine(Application.persistentDataPath, safeFolderName);
            Directory.CreateDirectory(_savePath);
        }

        public bool Exists(int slot)
        {
            return File.Exists(GetFilePath(slot));
        }

        public string Read(int slot)
        {
            var path = GetFilePath(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveStorage] 读取存档失败：Slot={slot}, Error={ex.Message}");
                return null;
            }
        }

        public void Write(int slot, string json)
        {
            if (json == null)
            {
                throw new FrameworkException("写入存档失败：json 不能为空。");
            }

            var targetPath = GetFilePath(slot);
            var tempPath = GetTempPath(slot);
            var backupPath = GetBackupPath(slot);

            try
            {
                File.WriteAllText(tempPath, json);
                var verify = File.ReadAllText(tempPath);
                if (!string.Equals(verify, json, StringComparison.Ordinal))
                {
                    throw new IOException("临时存档校验失败。");
                }

                if (File.Exists(targetPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Move(targetPath, backupPath);
                }

                File.Move(tempPath, targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveStorage] 写入存档失败：Slot={slot}, Error={ex.Message}");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }

        public void Delete(int slot)
        {
            DeleteIfExists(GetFilePath(slot));
            DeleteIfExists(GetBackupPath(slot));
            DeleteIfExists(GetTempPath(slot));
        }

        public bool TryRestoreBackup(int slot)
        {
            var targetPath = GetFilePath(slot);
            var backupPath = GetBackupPath(slot);
            if (!File.Exists(backupPath))
            {
                return false;
            }

            try
            {
                DeleteIfExists(targetPath);
                File.Copy(backupPath, targetPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveStorage] 恢复备份失败：Slot={slot}, Error={ex.Message}");
                return false;
            }
        }

        private string GetFilePath(int slot)
        {
            return Path.Combine(_savePath, $"slot_{slot}.json");
        }

        private string GetBackupPath(int slot)
        {
            return Path.Combine(_savePath, $"slot_{slot}.json.bak");
        }

        private string GetTempPath(int slot)
        {
            return Path.Combine(_savePath, $"slot_{slot}.json.tmp");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
