using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public abstract class AssetRefBase : IDisposable
    {
        private readonly Action<AssetKey> _release;

        protected AssetRefBase(AssetKey key, Action<AssetKey> release)
        {
            Key = key;
            _release = release;
            IsValid = true;
        }

        public AssetKey Key { get; }

        public bool IsValid { get; private set; }

        public void Release()
        {
            if (!IsValid)
            {
                return;
            }

            IsValid = false;
            _release?.Invoke(Key);
        }

        public void Dispose()
        {
            Release();
        }
    }

    public sealed class AssetRef<T> : AssetRefBase where T : UnityEngine.Object
    {
        internal AssetRef(AssetKey key, T asset, Action<AssetKey> release) : base(key, release)
        {
            Asset = asset;
        }

        public T Asset { get; }

        public static implicit operator T(AssetRef<T> assetRef)
        {
            return assetRef != null && assetRef.IsValid ? assetRef.Asset : null;
        }
    }

    public sealed class SubAssetsRef<T> : AssetRefBase where T : UnityEngine.Object
    {
        private readonly Dictionary<string, T> _byName;

        internal SubAssetsRef(AssetKey key, IReadOnlyList<T> assets, Action<AssetKey> release) : base(key, release)
        {
            Assets = assets ?? Array.Empty<T>();
            _byName = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < Assets.Count; i++)
            {
                var asset = Assets[i];
                if (asset != null && !_byName.ContainsKey(asset.name))
                {
                    _byName.Add(asset.name, asset);
                }
            }
        }

        public IReadOnlyList<T> Assets { get; }

        public T Get(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName) || !IsValid)
            {
                return null;
            }

            return _byName.TryGetValue(assetName, out var asset) ? asset : null;
        }
    }

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

    public sealed class AssetPreloadToken : IDisposable
    {
        private readonly Action<AssetPreloadToken> _release;

        internal AssetPreloadToken(Guid tokenId, string group, IReadOnlyList<AssetKey> keys, Action<AssetPreloadToken> release)
        {
            TokenId = tokenId;
            Group = group;
            Keys = keys ?? Array.Empty<AssetKey>();
            _release = release;
        }

        internal Guid TokenId { get; }

        public string Group { get; }

        public IReadOnlyList<AssetKey> Keys { get; }

        public bool IsReleased { get; private set; }

        public void Release()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;
            _release?.Invoke(this);
        }

        public void Dispose()
        {
            Release();
        }

        internal void MarkReleasedByPool()
        {
            IsReleased = true;
        }
    }
}
