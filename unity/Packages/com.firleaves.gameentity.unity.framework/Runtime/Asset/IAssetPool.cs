using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IAssetPool
    {
        UniTask<AssetRef<T>> LoadAsync<T>(
            AssetKey key,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
            where T : UnityEngine.Object;

        UniTask<SubAssetsRef<T>> LoadSubAssetsAsync<T>(
            AssetKey key,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
            where T : UnityEngine.Object;

        UniTask<RawFileRef> LoadRawFileAsync(
            AssetKey key,
            AssetLoadOptions options = null,
            CancellationToken ct = default);

        UniTask<AssetPreloadToken> PreloadAsync(
            AssetPreloadItem item,
            AssetPreloadOptions options = null,
            CancellationToken ct = default);

        UniTask<AssetPreloadToken> PreloadGroupAsync(
            AssetPreloadGroup group,
            CancellationToken ct = default);

        bool TryGetLoaded<T>(AssetKey key, out T asset)
            where T : UnityEngine.Object;

        void ReleaseGroup(string group);
        int ReleaseUnused(AssetReleaseReason reason = AssetReleaseReason.Manual);
        UniTask UnloadUnusedAssetsAsync(int loopCount = 10, CancellationToken ct = default);
        AssetPoolSnapshot GetSnapshot();
    }

}
