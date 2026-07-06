using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetPoolEntity : Entity, IAwake<IYooAssetBootstrap, AssetPoolPolicy>, IUpdate, IDestroy, IAssetPool, IAssetUsageTracker
    {
        private readonly Dictionary<AssetKey, AssetItem> _items = new Dictionary<AssetKey, AssetItem>();
        private readonly Dictionary<Guid, AssetPreloadToken> _preloadTokens = new Dictionary<Guid, AssetPreloadToken>();
        private readonly ObjectPool<AssetItem> _pool;
        private readonly List<AssetPreloadToken> _groupReleaseBuffer = new List<AssetPreloadToken>();
        private IYooAssetBootstrap _bootstrap;
        private AssetPoolPolicy _policy;
        private float _autoReleaseElapsed;

        public AssetPoolEntity()
        {
            _pool = new ObjectPool<AssetItem>(
                item => _items.Remove(item.Key),
                item => item.Locked,
                item => item.Priority,
                item => item.LastUseTimeUtc);
        }

        public void Awake(IYooAssetBootstrap bootstrap, AssetPoolPolicy policy)
        {
            _bootstrap = bootstrap ?? throw new FrameworkException("AssetPool 初始化失败：YooAssetBootstrap 不能为空。");
            _policy = policy != null ? policy.Clone() : AssetPoolPolicy.CreateDefault();
        }

        public void Update(float time)
        {
            if (_policy == null || _policy.AutoReleaseIntervalSeconds <= 0f)
            {
                return;
            }

            _autoReleaseElapsed += Mathf.Max(0f, time);
            if (_autoReleaseElapsed < _policy.AutoReleaseIntervalSeconds)
            {
                return;
            }

            _autoReleaseElapsed = 0f;
            ReleaseUnused(AssetReleaseReason.Expired);
        }

        public void OnDestroy()
        {
            foreach (var pair in _preloadTokens)
            {
                pair.Value.MarkReleasedByPool();
            }

            foreach (var pair in _items)
            {
                pair.Value.Release(true);
            }

            _preloadTokens.Clear();
            _items.Clear();
            _pool.Clear();
            _groupReleaseBuffer.Clear();
            _bootstrap = null;
            _policy = null;
        }

        public async UniTask<AssetRef<T>> LoadAsync<T>(AssetKey key, AssetLoadOptions options = null, CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            var normalizedKey = NormalizeObjectKey<T>(key, AssetKind.MainAsset);
            var item = await LoadItemAsync(normalizedKey, options, ct);
            var handle = item.Handle as AssetHandle;
            var asset = handle != null ? handle.GetAssetObject<T>() : null;
            if (asset == null)
            {
                throw new FrameworkException($"资源加载成功但类型不匹配：Key={normalizedKey}, Expected={typeof(T).Name}");
            }

            RetainReference(item, options);
            return new AssetRef<T>(normalizedKey, asset, ReleaseReference);
        }

        public async UniTask<SubAssetsRef<T>> LoadSubAssetsAsync<T>(AssetKey key, AssetLoadOptions options = null, CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            var normalizedKey = NormalizeObjectKey<T>(key, AssetKind.SubAssets);
            var item = await LoadItemAsync(normalizedKey, options, ct);
            var handle = item.Handle as SubAssetsHandle;
            var assets = handle != null ? handle.GetSubAssetObjects<T>() : null;
            if (assets == null)
            {
                throw new FrameworkException($"子资源加载成功但类型不匹配：Key={normalizedKey}, Expected={typeof(T).Name}");
            }

            RetainReference(item, options);
            return new SubAssetsRef<T>(normalizedKey, assets, ReleaseReference);
        }

        public async UniTask<RawFileRef> LoadRawFileAsync(AssetKey key, AssetLoadOptions options = null, CancellationToken ct = default)
        {
            if (key.Kind != AssetKind.RawFile)
            {
                key = AssetKey.RawFile(key.Location, key.PackageName);
            }

            var item = await LoadItemAsync(key, options, ct);
            var handle = item.Handle as RawFileHandle;
            if (handle == null)
            {
                throw new FrameworkException($"RawFile 资源句柄类型错误：Key={key}");
            }

            RetainReference(item, options);
            return new RawFileRef(key, handle, ReleaseReference);
        }

        public async UniTask<AssetPreloadToken> PreloadAsync(AssetPreloadItem preloadItem, AssetPreloadOptions options = null, CancellationToken ct = default)
        {
            if (preloadItem == null || !preloadItem.Key.IsValid)
            {
                throw new FrameworkException("预加载资源不能为空。");
            }

            var group = options != null && !string.IsNullOrWhiteSpace(options.Group) ? options.Group : null;
            var tokenId = Guid.NewGuid();
            var key = preloadItem.Key;
            var loadOptions = new AssetLoadOptions
            {
                LoadPriority = preloadItem.LoadPriority,
                Locked = preloadItem.Locked,
                Priority = preloadItem.Priority,
                ExpireSeconds = options != null ? options.ExpireSeconds : null
            };

            var item = await LoadItemAsync(key, loadOptions, ct);
            CachePreload(item, tokenId, group, loadOptions);
            var token = new AssetPreloadToken(tokenId, group, new[] { key }, ReleasePreloadToken);
            _preloadTokens[tokenId] = token;
            options?.Progress?.Report(new AssetPreloadProgress(group, key, 1, 1));
            return token;
        }

        public async UniTask<AssetPreloadToken> PreloadGroupAsync(AssetPreloadGroup group, CancellationToken ct = default)
        {
            if (group == null || group.Items == null || group.Items.Count == 0)
            {
                throw new FrameworkException("预加载组不能为空。");
            }

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                throw new FrameworkException("预加载组名称不能为空。");
            }

            var tokenId = Guid.NewGuid();
            var keys = new List<AssetKey>(group.Items.Count);
            try
            {
                for (var i = 0; i < group.Items.Count; i++)
                {
                    var preloadItem = group.Items[i];
                    if (preloadItem == null || !preloadItem.Key.IsValid)
                    {
                        throw new FrameworkException($"预加载组 {group.Name} 包含无效资源。");
                    }

                    var loadOptions = new AssetLoadOptions
                    {
                        LoadPriority = preloadItem.LoadPriority,
                        Locked = preloadItem.Locked,
                        Priority = preloadItem.Priority,
                        ExpireSeconds = group.ExpireSeconds
                    };
                    var item = await LoadItemAsync(preloadItem.Key, loadOptions, ct);
                    CachePreload(item, tokenId, group.Name, loadOptions);
                    keys.Add(preloadItem.Key);
                    group.Progress?.Report(new AssetPreloadProgress(group.Name, preloadItem.Key, i + 1, group.Items.Count));
                }
            }
            catch
            {
                ReleasePreloadToken(tokenId, keys);
                throw;
            }

            var token = new AssetPreloadToken(tokenId, group.Name, keys, ReleasePreloadToken);
            _preloadTokens[tokenId] = token;
            return token;
        }

        public bool TryGetLoaded<T>(AssetKey key, out T asset) where T : UnityEngine.Object
        {
            var normalizedKey = NormalizeObjectKey<T>(key, AssetKind.MainAsset);
            asset = null;
            if (!_items.TryGetValue(normalizedKey, out var item) || item.LoadState != AssetLoadState.Loaded)
            {
                return false;
            }

            var handle = item.Handle as AssetHandle;
            asset = handle != null ? handle.GetAssetObject<T>() : null;
            return asset != null;
        }

        public void ReleaseGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            _groupReleaseBuffer.Clear();
            foreach (var pair in _preloadTokens)
            {
                var token = pair.Value;
                if (token != null && string.Equals(token.Group, group, StringComparison.Ordinal))
                {
                    _groupReleaseBuffer.Add(token);
                }
            }

            for (var i = 0; i < _groupReleaseBuffer.Count; i++)
            {
                var token = _groupReleaseBuffer[i];
                token.MarkReleasedByPool();
                ReleasePreloadToken(token.TokenId, token.Keys);
            }

            _groupReleaseBuffer.Clear();

            ReleaseUnused(AssetReleaseReason.GroupReleased);
        }

        public int ReleaseUnused(AssetReleaseReason reason = AssetReleaseReason.Manual)
        {
            if (_items.Count == 0)
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            var released = _pool.ReleaseUnused(now, CreatePoolPolicy());
            if (released > 0 && _policy.ReleaseYooAssetUnusedAfterScan && _bootstrap?.DefaultPackage != null)
            {
                _bootstrap.DefaultPackage.UnloadUnusedAssetsAsync(Math.Max(1, _policy.YooAssetUnloadLoopCount));
            }

            return released;
        }

        public async UniTask UnloadUnusedAssetsAsync(int loopCount = 10, CancellationToken ct = default)
        {
            if (_bootstrap?.DefaultPackage == null)
            {
                return;
            }

            var operation = _bootstrap.DefaultPackage.UnloadUnusedAssetsAsync(Math.Max(1, loopCount));
            await operation.Task.AsUniTask().AttachExternalCancellation(ct);
            if (operation.Status != EOperationStatus.Succeed)
            {
                throw new FrameworkException($"YooAsset UnloadUnusedAssetsAsync 失败：{operation.Error}");
            }
        }

        public AssetPoolSnapshot GetSnapshot()
        {
            var infos = new List<AssetInfo>(_items.Count);
            foreach (var pair in _items)
            {
                infos.Add(pair.Value.ToInfo());
            }

            return new AssetPoolSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                CanReleaseCount = _pool.GetCanReleaseCount(DateTime.UtcNow),
                Items = infos
            };
        }

        async UniTask<T> IAssetUsageTracker.LoadCachedAsync<T>(AssetKey key, AssetLoadOptions options, CancellationToken ct)
        {
            var normalizedKey = NormalizeObjectKey<T>(key, AssetKind.MainAsset);
            var item = await LoadItemAsync(normalizedKey, options, ct);
            var handle = item.Handle as AssetHandle;
            var asset = handle != null ? handle.GetAssetObject<T>() : null;
            if (asset == null)
            {
                throw new FrameworkException($"资源加载成功但类型不匹配：Key={normalizedKey}, Expected={typeof(T).Name}");
            }

            return asset;
        }

        void IAssetUsageTracker.RetainUsage(AssetKey key)
        {
            var item = GetOrCreateItem(key);
            item.RefCount++;
            item.Touch();
        }

        void IAssetUsageTracker.ReleaseUsage(AssetKey key)
        {
            ReleaseReference(key);
        }

        private async UniTask<AssetItem> LoadItemAsync(AssetKey key, AssetLoadOptions options, CancellationToken ct)
        {
            if (key.Kind == AssetKind.Scene)
            {
                throw new FrameworkException("AssetPool 首期不支持 Scene 加载。");
            }

            var item = GetOrCreateItem(key);
            ApplyOptions(item, options);

            if (item.LoadState == AssetLoadState.Loaded)
            {
                item.Touch();
                return item;
            }

            if (item.LoadState != AssetLoadState.Loading || item.LoadTask == null)
            {
                StartLoad(item, options);
            }

            ct.ThrowIfCancellationRequested();
            var loadingHandle = item.Handle;
            var loadingTask = item.LoadTask;
            if (loadingHandle == null || loadingTask == null)
            {
                item.LoadState = AssetLoadState.Failed;
                item.Error = "YooAsset handle 或 Task 为空。";
                throw new FrameworkException($"资源加载失败：Key={key}, Error={item.Error}");
            }

            await loadingTask.AsUniTask().AttachExternalCancellation(ct);
            ct.ThrowIfCancellationRequested();

            if (!ReferenceEquals(item.Handle, loadingHandle))
            {
                return await LoadItemAsync(key, options, ct);
            }

            if (loadingHandle.Status != EOperationStatus.Succeed)
            {
                item.LoadState = AssetLoadState.Failed;
                item.Error = !string.IsNullOrWhiteSpace(loadingHandle.LastError) ? loadingHandle.LastError : "YooAsset handle 加载失败。";
                UpdateCacheUntil(item);
                throw new FrameworkException($"资源加载失败：Key={key}, Error={item.Error}");
            }

            item.LoadState = AssetLoadState.Loaded;
            item.Error = null;
            item.Touch();
            return item;
        }

        private void StartLoad(AssetItem item, AssetLoadOptions options)
        {
            var package = _bootstrap.GetPackage(item.Key.PackageName);
            var priority = options != null ? options.LoadPriority : 0u;
            ReleaseHandle(item.Handle);

            HandleBase handle;
            switch (item.Key.Kind)
            {
                case AssetKind.MainAsset:
                    handle = item.Key.AssetType != null
                        ? package.LoadAssetAsync(item.Key.Location, item.Key.AssetType, priority)
                        : package.LoadAssetAsync(item.Key.Location, priority);
                    break;
                case AssetKind.SubAssets:
                    handle = item.Key.AssetType != null
                        ? package.LoadSubAssetsAsync(item.Key.Location, item.Key.AssetType, priority)
                        : package.LoadSubAssetsAsync(item.Key.Location, priority);
                    break;
                case AssetKind.RawFile:
                    handle = package.LoadRawFileAsync(item.Key.Location, priority);
                    break;
                default:
                    throw new FrameworkException($"不支持的资源类型：{item.Key.Kind}");
            }

            item.Handle = handle;
            item.LoadTask = handle.Task;
            item.LoadState = AssetLoadState.Loading;
            item.Error = null;
            item.Touch();
            TrackLoadCompletionAsync(item, handle).Forget();
        }

        private async UniTaskVoid TrackLoadCompletionAsync(AssetItem item, HandleBase handle)
        {
            try
            {
                var task = handle != null ? handle.Task : null;
                if (task != null)
                {
                    await task.AsUniTask();
                }
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(item.Handle, handle))
                {
                    item.LoadState = AssetLoadState.Failed;
                    item.Error = ex.Message;
                    item.Touch();
                    UpdateCacheUntil(item);
                }

                return;
            }

            if (!ReferenceEquals(item.Handle, handle))
            {
                return;
            }

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                item.LoadState = AssetLoadState.Failed;
                item.Error = handle != null && !string.IsNullOrWhiteSpace(handle.LastError)
                    ? handle.LastError
                    : "YooAsset handle 加载失败。";
                UpdateCacheUntil(item);
            }
            else
            {
                item.LoadState = AssetLoadState.Loaded;
                item.Error = null;
            }

            item.Touch();
        }

        private AssetItem GetOrCreateItem(AssetKey key)
        {
            if (!_items.TryGetValue(key, out var item))
            {
                item = new AssetItem(key, _policy != null ? _policy.DefaultPriority : 0);
                _items.Add(key, item);
                _pool.Register(item);
            }

            return item;
        }

        private void RetainReference(AssetItem item, AssetLoadOptions options)
        {
            ApplyOptions(item, options);
            item.RefCount++;
            item.Touch();
        }

        private void ReleaseReference(AssetKey key)
        {
            if (!_items.TryGetValue(key, out var item))
            {
                return;
            }

            item.RefCount = Math.Max(0, item.RefCount - 1);
            item.Touch();
            UpdateCacheUntil(item);
        }

        private void CachePreload(AssetItem item, Guid tokenId, string group, AssetLoadOptions options)
        {
            ApplyOptions(item, options);
            item.CachePreload(tokenId, group, CalculateCacheUntil(item));
            item.Touch();
            item.RefreshCacheUntil();
        }

        private void ReleasePreloadToken(AssetPreloadToken token)
        {
            if (token == null)
            {
                return;
            }

            ReleasePreloadToken(token.TokenId, token.Keys);
        }

        private void ReleasePreloadToken(Guid tokenId, IReadOnlyList<AssetKey> keys)
        {
            _preloadTokens.Remove(tokenId);
            if (keys == null)
            {
                return;
            }

            for (var i = 0; i < keys.Count; i++)
            {
                if (_items.TryGetValue(keys[i], out var item))
                {
                    item.ReleasePreload(tokenId);
                    item.Touch();
                    item.RefreshCacheUntil();
                }
            }
        }

        private void ApplyOptions(AssetItem item, AssetLoadOptions options)
        {
            if (item == null)
            {
                return;
            }

            if (options != null)
            {
                item.Locked |= options.Locked;
                item.Priority = options.Priority;
                item.CustomExpireSeconds = options.ExpireSeconds;
            }
            else if (_policy != null)
            {
                item.Priority = _policy.DefaultPriority;
            }
        }

        private void UpdateCacheUntil(AssetItem item)
        {
            if (item == null)
            {
                return;
            }

            var nextCacheUntil = CalculateCacheUntil(item);
            if (!nextCacheUntil.HasValue)
            {
                return;
            }

            if (!item.IdleCacheUntilUtc.HasValue || nextCacheUntil.Value > item.IdleCacheUntilUtc.Value)
            {
                item.IdleCacheUntilUtc = nextCacheUntil;
                item.RefreshCacheUntil();
            }
        }

        private DateTime? CalculateCacheUntil(AssetItem item)
        {
            var seconds = item != null
                ? item.CustomExpireSeconds ?? (_policy != null ? _policy.ExpireSeconds : 0f)
                : _policy != null ? _policy.ExpireSeconds : 0f;
            if (seconds <= 0f)
            {
                return null;
            }

            return DateTime.UtcNow.AddSeconds(seconds);
        }

        private ObjectPoolPolicy CreatePoolPolicy()
        {
            return new ObjectPoolPolicy(
                _policy != null ? _policy.Capacity : 0,
                0f,
                true);
        }

        private static void ReleaseHandle(HandleBase handle)
        {
            if (handle != null && handle.IsValid)
            {
                handle.Release();
            }
        }

        private static AssetKey NormalizeObjectKey<T>(AssetKey key, AssetKind kind) where T : UnityEngine.Object
        {
            if (key.Kind != kind)
            {
                key = new AssetKey(key.Location, key.PackageName, kind, typeof(T), key.SubAssetName);
            }

            if (key.AssetType == null || key.AssetType == typeof(UnityEngine.Object))
            {
                return new AssetKey(key.Location, key.PackageName, key.Kind, typeof(T), key.SubAssetName);
            }

            if (key.AssetType != typeof(T))
            {
                throw new FrameworkException($"AssetKey 类型和请求类型不一致：Key={key.AssetType.Name}, Request={typeof(T).Name}");
            }

            return key;
        }

        private sealed class AssetItem : IObjectPoolItem
        {
            private readonly Dictionary<Guid, DateTime?> _preloadTokens = new Dictionary<Guid, DateTime?>();
            private readonly Dictionary<string, HashSet<Guid>> _groupTokens = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);

            public AssetItem(AssetKey key, int priority)
            {
                Key = key;
                Priority = priority;
                LoadState = AssetLoadState.None;
                LastUseTimeUtc = DateTime.UtcNow;
            }

            public AssetKey Key { get; }
            public AssetLoadState LoadState;
            public HandleBase Handle;
            public Task LoadTask;
            public int RefCount;
            public bool Locked;
            public int Priority;
            public DateTime LastUseTimeUtc;
            public DateTime? IdleCacheUntilUtc;
            public DateTime? CacheUntilUtc;
            public float? CustomExpireSeconds;
            public string Error;

            public void Touch()
            {
                LastUseTimeUtc = DateTime.UtcNow;
            }

            public bool IsInUse => RefCount > 0 || LoadState == AssetLoadState.Loading;

            public void CachePreload(Guid tokenId, string group, DateTime? cacheUntilUtc)
            {
                _preloadTokens[tokenId] = cacheUntilUtc;
                if (!string.IsNullOrWhiteSpace(group))
                {
                    if (!_groupTokens.TryGetValue(group, out var tokens))
                    {
                        tokens = new HashSet<Guid>();
                        _groupTokens.Add(group, tokens);
                    }

                    tokens.Add(tokenId);
                }
            }

            public void ReleasePreload(Guid tokenId)
            {
                _preloadTokens.Remove(tokenId);
                RefreshCacheUntil();
                if (_groupTokens.Count == 0)
                {
                    return;
                }

                var emptyGroups = new List<string>();
                foreach (var pair in _groupTokens)
                {
                    pair.Value.Remove(tokenId);
                    if (pair.Value.Count == 0)
                    {
                        emptyGroups.Add(pair.Key);
                    }
                }

                for (var i = 0; i < emptyGroups.Count; i++)
                {
                    _groupTokens.Remove(emptyGroups[i]);
                }
            }

            public void ReleaseGroup(string group)
            {
                if (!_groupTokens.TryGetValue(group, out var tokens))
                {
                    return;
                }

                foreach (var token in tokens)
                {
                    _preloadTokens.Remove(token);
                }

                _groupTokens.Remove(group);
                RefreshCacheUntil();
            }

            public void ClearRetainers()
            {
                RefCount = 0;
                IdleCacheUntilUtc = null;
                CacheUntilUtc = null;
                _preloadTokens.Clear();
                _groupTokens.Clear();
            }

            public void RefreshCacheUntil()
            {
                var cacheUntil = IdleCacheUntilUtc;
                foreach (var pair in _preloadTokens)
                {
                    if (!pair.Value.HasValue)
                    {
                        continue;
                    }

                    if (!cacheUntil.HasValue || pair.Value.Value > cacheUntil.Value)
                    {
                        cacheUntil = pair.Value;
                    }
                }

                CacheUntilUtc = cacheUntil;
            }

            public bool CanRelease(DateTime now)
            {
                if (LoadState == AssetLoadState.Failed || LoadState == AssetLoadState.Released)
                {
                    return true;
                }

                if (Handle != null && !Handle.IsValid)
                {
                    return false;
                }

                if (CacheUntilUtc.HasValue)
                {
                    return now >= CacheUntilUtc.Value;
                }

                return true;
            }

            public void Release(bool isShutdown)
            {
                ReleaseHandle(Handle);

                Handle = null;
                LoadTask = null;
                LoadState = AssetLoadState.Released;
                Error = null;
                ClearRetainers();
            }

            public AssetInfo ToInfo()
            {
                var groups = new List<string>(_groupTokens.Keys);
                groups.Sort(StringComparer.Ordinal);
                return new AssetInfo
                {
                    Key = Key,
                    LoadState = LoadState,
                    RefCount = RefCount,
                    Locked = Locked,
                    Priority = Priority,
                    LastUseTimeUtc = LastUseTimeUtc,
                    CacheUntilUtc = CacheUntilUtc,
                    Groups = groups,
                    Error = Error
                };
            }
        }
    }
}
