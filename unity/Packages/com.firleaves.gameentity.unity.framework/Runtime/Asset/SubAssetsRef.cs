using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
