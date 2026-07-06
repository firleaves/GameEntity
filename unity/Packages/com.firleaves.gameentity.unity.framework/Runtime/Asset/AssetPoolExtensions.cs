using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public static class AssetPoolExtensions
    {
        public static UniTask<AssetRef<T>> LoadAsync<T>(
            this IAssetPool assetPool,
            string location,
            string packageName = null,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            EnsureAssetPool(assetPool);
            return assetPool.LoadAsync<T>(AssetKey.Main<T>(location, packageName), options, ct);
        }

        public static UniTask<SubAssetsRef<T>> LoadSubAssetsAsync<T>(
            this IAssetPool assetPool,
            string location,
            string packageName = null,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            EnsureAssetPool(assetPool);
            return assetPool.LoadSubAssetsAsync<T>(AssetKey.SubAssets<T>(location, packageName), options, ct);
        }

        public static UniTask<RawFileRef> LoadRawFileAsync(
            this IAssetPool assetPool,
            string location,
            string packageName = null,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
        {
            EnsureAssetPool(assetPool);
            return assetPool.LoadRawFileAsync(AssetKey.RawFile(location, packageName), options, ct);
        }

        public static UniTask<AssetPreloadToken> PreloadAsync<T>(
            this IAssetPool assetPool,
            string location,
            string packageName = null,
            AssetPreloadOptions options = null,
            uint loadPriority = 0,
            bool locked = false,
            int priority = 0,
            CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            EnsureAssetPool(assetPool);
            return assetPool.PreloadAsync(
                new AssetPreloadItem
                {
                    Key = AssetKey.Main<T>(location, packageName),
                    LoadPriority = loadPriority,
                    Locked = locked,
                    Priority = priority
                },
                options,
                ct);
        }

        public static UniTask<AssetPreloadToken> PreloadRawFileAsync(
            this IAssetPool assetPool,
            string location,
            string packageName = null,
            AssetPreloadOptions options = null,
            uint loadPriority = 0,
            bool locked = false,
            int priority = 0,
            CancellationToken ct = default)
        {
            EnsureAssetPool(assetPool);
            return assetPool.PreloadAsync(
                new AssetPreloadItem
                {
                    Key = AssetKey.RawFile(location, packageName),
                    LoadPriority = loadPriority,
                    Locked = locked,
                    Priority = priority
                },
                options,
                ct);
        }

        public static bool TryGetLoaded<T>(
            this IAssetPool assetPool,
            string location,
            out T asset,
            string packageName = null)
            where T : UnityEngine.Object
        {
            EnsureAssetPool(assetPool);
            return assetPool.TryGetLoaded(AssetKey.Main<T>(location, packageName), out asset);
        }

        private static void EnsureAssetPool(IAssetPool assetPool)
        {
            if (assetPool == null)
            {
                throw new FrameworkException("AssetPool 不能为空。");
            }
        }
    }
}
