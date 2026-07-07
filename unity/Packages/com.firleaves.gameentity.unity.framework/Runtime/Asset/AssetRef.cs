using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
