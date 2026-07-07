using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public sealed class RawFileRef : AssetRefBase
    {
        private readonly RawFileHandle _handle;

        internal RawFileRef(AssetKey key, RawFileHandle handle, Action<AssetKey> release) : base(key, release)
        {
            _handle = handle;
        }

        public string GetRawFilePath()
        {
            return IsValid && _handle != null && _handle.IsValid ? _handle.GetRawFilePath() : string.Empty;
        }

        public byte[] GetRawFileData()
        {
            return IsValid && _handle != null && _handle.IsValid ? _handle.GetRawFileData() : null;
        }

        public string GetRawFileText()
        {
            return IsValid && _handle != null && _handle.IsValid ? _handle.GetRawFileText() : null;
        }
    }

}
