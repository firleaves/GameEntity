using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum AssetLoadState
    {
        None,
        Loading,
        Loaded,
        Failed,
        Released
    }

    public enum AssetReleaseReason
    {
        Manual,
        Capacity,
        Expired,
        GroupReleased,
        Shutdown
    }

    public sealed class AssetLoadOptions
    {
        public uint LoadPriority;
        public string Group;
        public bool Locked;
        public int Priority;
        public float? ExpireSeconds;

        public static readonly AssetLoadOptions Default = new AssetLoadOptions();
    }

    public sealed class AssetPreloadItem
    {
        public AssetKey Key;
        public uint LoadPriority;
        public bool Locked;
        public int Priority;
    }

    public sealed class AssetPreloadGroup
    {
        public string Name;
        public float? ExpireSeconds;
        public IReadOnlyList<AssetPreloadItem> Items;
        public IProgress<AssetPreloadProgress> Progress;
    }

    public sealed class AssetPreloadOptions
    {
        public string Group;
        public float? ExpireSeconds;
        public IProgress<AssetPreloadProgress> Progress;
    }

    public readonly struct AssetPreloadProgress
    {
        public readonly string Group;
        public readonly AssetKey Key;
        public readonly int CompletedCount;
        public readonly int TotalCount;
        public readonly float Progress;

        public AssetPreloadProgress(string group, AssetKey key, int completedCount, int totalCount)
        {
            Group = group;
            Key = key;
            CompletedCount = completedCount;
            TotalCount = totalCount;
            Progress = totalCount <= 0 ? 1f : Mathf.Clamp01((float)completedCount / totalCount);
        }
    }

    public sealed class AssetInfo
    {
        public AssetKey Key;
        public AssetLoadState LoadState;
        public int RefCount;
        public bool Locked;
        public int Priority;
        public DateTime LastUseTimeUtc;
        public DateTime? CacheUntilUtc;
        public IReadOnlyList<string> Groups;
        public string Error;
    }

    public sealed class AssetPoolSnapshot
    {
        public DateTime CapturedAtUtc;
        public int CanReleaseCount;
        public IReadOnlyList<AssetInfo> Items;
    }

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

    internal interface IAssetUsageTracker
    {
        UniTask<T> LoadCachedAsync<T>(
            AssetKey key,
            AssetLoadOptions options = null,
            CancellationToken ct = default)
            where T : UnityEngine.Object;

        void RetainUsage(AssetKey key);
        void ReleaseUsage(AssetKey key);
    }
}
