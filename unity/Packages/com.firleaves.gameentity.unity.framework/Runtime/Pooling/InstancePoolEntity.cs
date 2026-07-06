using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class InstancePoolEntity : Entity, IAwake<IAssetPool, PoolPolicy, Transform>, IUpdate, IDestroy, IInstancePool
    {
        private readonly Dictionary<AssetKey, PrefabPool> _pools = new Dictionary<AssetKey, PrefabPool>();
        private readonly Dictionary<GameObject, PrefabPool> _instanceMap = new Dictionary<GameObject, PrefabPool>();
        private IAssetUsageTracker _assetTracker;
        private PoolPolicy _defaultPolicy;
        private Transform _inactiveRoot;
        private float _autoReleaseElapsed;

        public void Awake(IAssetPool assetPool, PoolPolicy defaultPolicy, Transform inactiveRoot)
        {
            if (assetPool == null)
            {
                throw new FrameworkException("InstancePool 初始化失败：AssetPool 不能为空。");
            }

            _assetTracker = assetPool as IAssetUsageTracker
                ?? throw new FrameworkException("InstancePool 需要支持内部资源引用跟踪的 AssetPool。");
            _defaultPolicy = defaultPolicy != null ? defaultPolicy.Clone() : PoolPolicy.CreateDefault();
            _inactiveRoot = inactiveRoot != null ? inactiveRoot : CreateInactiveRoot();
        }

        public void Update(float time)
        {
            if (_defaultPolicy == null || _defaultPolicy.AutoReleaseIntervalSeconds <= 0f)
            {
                return;
            }

            _autoReleaseElapsed += Mathf.Max(0f, time);
            if (_autoReleaseElapsed < _defaultPolicy.AutoReleaseIntervalSeconds)
            {
                return;
            }

            _autoReleaseElapsed = 0f;
            ReleaseUnused(AssetReleaseReason.Expired);
        }

        public void OnDestroy()
        {
            foreach (var pair in _pools)
            {
                pair.Value.ReleaseAll(force: true, _instanceMap);
                pair.Value.ClearTemplate();
            }

            _pools.Clear();
            _instanceMap.Clear();

            if (_inactiveRoot != null)
            {
                UnityEngine.Object.Destroy(_inactiveRoot.gameObject);
            }

            _inactiveRoot = null;
            _assetTracker = null;
        }

        public async UniTask<InstanceRef> RentAsync(
            AssetKey prefabKey,
            Transform parent = null,
            InstanceRentOptions options = null,
            CancellationToken ct = default)
        {
            var key = NormalizePrefabKey(prefabKey);
            var pool = await GetOrCreatePoolAsync(key, options != null ? options.PolicyOverride : null, ct);
            var instance = pool.Rent(parent, options, _inactiveRoot, _instanceMap);
            return new InstanceRef(key, instance, this);
        }

        public async UniTask WarmupAsync(
            AssetKey prefabKey,
            int count,
            Transform inactiveRoot = null,
            PoolPolicy policy = null,
            CancellationToken ct = default)
        {
            if (count <= 0)
            {
                return;
            }

            var key = NormalizePrefabKey(prefabKey);
            var pool = await GetOrCreatePoolAsync(key, policy, ct);
            pool.Warmup(count, inactiveRoot != null ? inactiveRoot : _inactiveRoot, _instanceMap);
        }

        public void Return(InstanceRef instanceRef)
        {
            if (instanceRef == null || instanceRef.GameObject == null)
            {
                return;
            }

            Return(instanceRef.GameObject);
        }

        public bool Return(GameObject instance)
        {
            if (instance == null || !_instanceMap.TryGetValue(instance, out var pool))
            {
                return false;
            }

            return pool.Return(instance, _inactiveRoot, _instanceMap);
        }

        public int ReleaseUnused(AssetReleaseReason reason = AssetReleaseReason.Manual)
        {
            var released = 0;
            var now = DateTime.UtcNow;
            foreach (var pair in _pools)
            {
                released += pair.Value.ReleaseUnused(now);
            }

            var emptyPools = new List<AssetKey>();
            foreach (var pair in _pools)
            {
                if (pair.Value.CanRemove)
                {
                    pair.Value.ClearTemplate();
                    emptyPools.Add(pair.Key);
                }
            }

            for (var i = 0; i < emptyPools.Count; i++)
            {
                _pools.Remove(emptyPools[i]);
            }

            return released;
        }

        public void ReleasePool(AssetKey prefabKey, bool force = false)
        {
            var key = NormalizePrefabKey(prefabKey);
            if (!_pools.TryGetValue(key, out var pool))
            {
                return;
            }

            pool.ReleaseAll(force, _instanceMap);
            if (pool.CanRemove || force)
            {
                pool.ClearTemplate();
                _pools.Remove(key);
            }
        }

        public InstancePoolSnapshot GetSnapshot()
        {
            var states = new List<InstancePoolState>(_pools.Count);
            var canReleaseCount = 0;
            var now = DateTime.UtcNow;
            foreach (var pair in _pools)
            {
                canReleaseCount += pair.Value.GetCanReleaseCount(now);
                states.Add(pair.Value.ToState());
            }

            return new InstancePoolSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                CanReleaseCount = canReleaseCount,
                Pools = states
            };
        }

        private async UniTask<PrefabPool> GetOrCreatePoolAsync(AssetKey key, PoolPolicy policyOverride, CancellationToken ct)
        {
            if (_pools.TryGetValue(key, out var existing))
            {
                if (policyOverride != null)
                {
                    existing.Policy = policyOverride.Clone();
                }

                return existing;
            }

            var loadOptions = new AssetLoadOptions
            {
                Priority = policyOverride != null ? policyOverride.Priority : _defaultPolicy.Priority
            };
            var template = await _assetTracker.LoadCachedAsync<GameObject>(key, loadOptions, ct);
            var pool = new PrefabPool(key, template, policyOverride != null ? policyOverride.Clone() : _defaultPolicy.Clone(), _assetTracker);
            _pools.Add(key, pool);
            return pool;
        }

        private static AssetKey NormalizePrefabKey(AssetKey key)
        {
            if (key.Kind != AssetKind.MainAsset || key.AssetType != typeof(GameObject))
            {
                return AssetKey.Main<GameObject>(key.Location, key.PackageName);
            }

            return key;
        }

        private static Transform CreateInactiveRoot()
        {
            var root = new GameObject("[GameEntity.Unity.Framework.InstancePool]");
            root.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        private sealed class PrefabPool
        {
            private readonly Dictionary<GameObject, PooledInstance> _items = new Dictionary<GameObject, PooledInstance>();
            private readonly ObjectPool<PooledInstance> _idlePool;
            private Dictionary<GameObject, PrefabPool> _instanceMap;
            private readonly HashSet<GameObject> _active = new HashSet<GameObject>();
            private readonly IAssetUsageTracker _assetTracker;
            private GameObject _template;

            public PrefabPool(AssetKey key, GameObject template, PoolPolicy policy, IAssetUsageTracker assetTracker)
            {
                Key = key;
                _template = template;
                Policy = policy ?? PoolPolicy.CreateDefault();
                _assetTracker = assetTracker;
                _idlePool = new ObjectPool<PooledInstance>(
                    OnInstanceReleased,
                    item => item.Locked,
                    item => item.Priority,
                    item => item.LastUseTimeUtc);
                LastUseTimeUtc = DateTime.UtcNow;
            }

            public AssetKey Key { get; }
            public PoolPolicy Policy { get; set; }
            public DateTime LastUseTimeUtc { get; private set; }

            public bool CanRemove => _active.Count == 0 && _idlePool.Count == 0;

            public GameObject Rent(
                Transform parent,
                InstanceRentOptions options,
                Transform inactiveRoot,
                Dictionary<GameObject, PrefabPool> instanceMap)
            {
                _instanceMap = instanceMap;
                var fromPool = TryTakeIdle(out var pooled);
                if (!fromPool)
                {
                    pooled = CreateInstance(inactiveRoot, instanceMap);
                }

                var go = pooled.GameObject;
                if (go == null)
                {
                    pooled = CreateInstance(inactiveRoot, instanceMap);
                    go = pooled.GameObject;
                    fromPool = false;
                }

                _active.Add(go);
                instanceMap[go] = this;
                ApplyTransform(go.transform, parent, options);
                InvokeRent(go, new InstanceRentContext(Key, fromPool));
                go.SetActive(options == null || options.SetActive);
                LastUseTimeUtc = DateTime.UtcNow;
                return go;
            }

            public void Warmup(
                int count,
                Transform inactiveRoot,
                Dictionary<GameObject, PrefabPool> instanceMap)
            {
                _instanceMap = instanceMap;
                while (_idlePool.Count < count)
                {
                    var pooled = CreateInstance(inactiveRoot, instanceMap);
                    pooled.GameObject.SetActive(false);
                    RegisterIdle(pooled);
                }
            }

            public bool Return(
                GameObject instance,
                Transform inactiveRoot,
                Dictionary<GameObject, PrefabPool> instanceMap)
            {
                _instanceMap = instanceMap;
                if (instance == null || !_active.Remove(instance))
                {
                    return false;
                }

                InvokeReturn(instance);
                instance.transform.SetParent(inactiveRoot, false);
                instance.SetActive(false);
                LastUseTimeUtc = DateTime.UtcNow;

                if (!Policy.Locked && Policy.Capacity >= 0 && _idlePool.Count >= Policy.Capacity)
                {
                    DestroyInstance(instance);
                    return true;
                }

                if (!_items.TryGetValue(instance, out var pooled))
                {
                    pooled = new PooledInstance(instance, Key, Policy.Priority, Policy.Locked, DateTime.UtcNow, _assetTracker);
                    _items[instance] = pooled;
                }

                pooled.MarkIdle(Policy.Priority, Policy.Locked, DateTime.UtcNow);
                RegisterIdle(pooled);
                return true;
            }

            public int ReleaseUnused(DateTime now)
            {
                if (Policy.Locked || _idlePool.Count == 0)
                {
                    return 0;
                }

                return _idlePool.ReleaseUnused(now, new ObjectPoolPolicy(Policy.Capacity, Policy.ExpireSeconds, true));
            }

            public int GetCanReleaseCount(DateTime now)
            {
                return _idlePool.GetCanReleaseCount(now);
            }

            public void ReleaseAll(
                bool force,
                Dictionary<GameObject, PrefabPool> instanceMap)
            {
                _instanceMap = instanceMap;
                _idlePool.Shutdown();

                if (!force)
                {
                    return;
                }

                var activeObjects = new List<GameObject>(_active);
                for (var i = 0; i < activeObjects.Count; i++)
                {
                    DestroyInstance(activeObjects[i]);
                }

                _active.Clear();
            }

            public void ClearTemplate()
            {
                _template = null;
            }

            public InstancePoolState ToState()
            {
                return new InstancePoolState
                {
                    PrefabKey = Key,
                    ActiveCount = _active.Count,
                    IdleCount = _idlePool.Count,
                    Capacity = Policy.Capacity,
                    Locked = Policy.Locked,
                    Priority = Policy.Priority,
                    LastUseTimeUtc = LastUseTimeUtc
                };
            }

            private PooledInstance CreateInstance(
                Transform inactiveRoot,
                Dictionary<GameObject, PrefabPool> instanceMap)
            {
                if (_template == null)
                {
                    throw new FrameworkException($"实例池模板不可用：{Key}");
                }

                var go = UnityEngine.Object.Instantiate(_template, inactiveRoot);
                go.name = _template.name;
                go.SetActive(false);
                instanceMap[go] = this;
                _assetTracker?.RetainUsage(Key);
                var pooled = new PooledInstance(go, Key, Policy.Priority, Policy.Locked, DateTime.UtcNow, _assetTracker);
                _items[go] = pooled;
                return pooled;
            }

            private void DestroyInstance(GameObject instance)
            {
                if (instance == null)
                {
                    return;
                }

                if (_items.TryGetValue(instance, out var pooled))
                {
                    _idlePool.Unregister(pooled);
                    _items.Remove(instance);
                    _instanceMap?.Remove(instance);
                    pooled.Release(false);
                }
                else
                {
                    _instanceMap?.Remove(instance);
                    UnityEngine.Object.Destroy(instance);
                    _assetTracker?.ReleaseUsage(Key);
                }
            }

            private void OnInstanceReleased(PooledInstance item)
            {
                if (item?.GameObject == null)
                {
                    return;
                }

                _items.Remove(item.GameObject);
                _instanceMap?.Remove(item.GameObject);
            }

            private bool TryTakeIdle(out PooledInstance pooled)
            {
                foreach (var pair in _items)
                {
                    pooled = pair.Value;
                    if (!pooled.IsInUse && _idlePool.Unregister(pooled))
                    {
                        pooled.MarkActive(DateTime.UtcNow);
                        return true;
                    }
                }

                pooled = null;
                return false;
            }

            private void RegisterIdle(PooledInstance pooled)
            {
                pooled.MarkIdle(Policy.Priority, Policy.Locked, DateTime.UtcNow);
                _idlePool.Register(pooled);
            }

            private static void ApplyTransform(Transform transform, Transform parent, InstanceRentOptions options)
            {
                if (parent != null)
                {
                    transform.SetParent(parent, options != null && options.WorldPositionStays);
                }

                if (options != null && options.Position.HasValue)
                {
                    transform.position = options.Position.Value;
                }

                if (options != null && options.Rotation.HasValue)
                {
                    transform.rotation = options.Rotation.Value;
                }
            }

            private static void InvokeRent(GameObject go, InstanceRentContext context)
            {
                var components = go.GetComponentsInChildren<IInstancePoolable>(true);
                for (var i = 0; i < components.Length; i++)
                {
                    components[i].OnRent(context);
                }
            }

            private static void InvokeReturn(GameObject go)
            {
                var components = go.GetComponentsInChildren<IInstancePoolable>(true);
                for (var i = 0; i < components.Length; i++)
                {
                    components[i].OnReturn();
                }
            }

        }

        private sealed class PooledInstance : IObjectPoolItem, IEquatable<PooledInstance>
        {
            private readonly AssetKey _prefabKey;
            private readonly IAssetUsageTracker _assetTracker;

            public PooledInstance(
                GameObject gameObject,
                AssetKey prefabKey,
                int priority,
                bool locked,
                DateTime lastUseTimeUtc,
                IAssetUsageTracker assetTracker)
            {
                GameObject = gameObject;
                _prefabKey = prefabKey;
                Priority = priority;
                Locked = locked;
                LastUseTimeUtc = lastUseTimeUtc;
                _assetTracker = assetTracker;
                IsInUse = true;
            }

            public GameObject GameObject { get; }
            public int Priority { get; private set; }
            public bool Locked { get; private set; }
            public DateTime LastUseTimeUtc { get; private set; }
            public bool IsInUse { get; private set; }

            public void MarkActive(DateTime now)
            {
                IsInUse = true;
                LastUseTimeUtc = now;
            }

            public void MarkIdle(int priority, bool locked, DateTime now)
            {
                IsInUse = false;
                Priority = priority;
                Locked = locked;
                LastUseTimeUtc = now;
            }

            public bool CanRelease(DateTime now)
            {
                if (GameObject == null)
                {
                    return true;
                }

                var components = GameObject.GetComponentsInChildren<IInstancePoolable>(true);
                for (var i = 0; i < components.Length; i++)
                {
                    if (!components[i].CanReleaseFromPool())
                    {
                        return false;
                    }
                }

                return true;
            }

            public void Release(bool isShutdown)
            {
                if (GameObject != null)
                {
                    UnityEngine.Object.Destroy(GameObject);
                }

                _assetTracker?.ReleaseUsage(_prefabKey);
            }

            public bool Equals(PooledInstance other)
            {
                return other != null && ReferenceEquals(GameObject, other.GameObject);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PooledInstance);
            }

            public override int GetHashCode()
            {
                return GameObject != null ? GameObject.GetInstanceID() : 0;
            }
        }
    }
}
