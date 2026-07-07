using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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
