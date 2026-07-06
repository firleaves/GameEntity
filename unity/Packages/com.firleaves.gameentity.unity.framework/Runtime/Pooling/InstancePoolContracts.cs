using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class InstanceRentOptions
    {
        public bool SetActive = true;
        public bool WorldPositionStays;
        public Vector3? Position;
        public Quaternion? Rotation;
        public PoolPolicy PolicyOverride;
    }

    public readonly struct InstanceRentContext
    {
        public readonly AssetKey PrefabKey;
        public readonly bool FromPool;

        public InstanceRentContext(AssetKey prefabKey, bool fromPool)
        {
            PrefabKey = prefabKey;
            FromPool = fromPool;
        }
    }

    public interface IInstancePoolable
    {
        void OnRent(InstanceRentContext context);
        void OnReturn();
        bool CanReleaseFromPool();
    }

    public interface IInstancePool
    {
        UniTask<InstanceRef> RentAsync(
            AssetKey prefabKey,
            Transform parent = null,
            InstanceRentOptions options = null,
            CancellationToken ct = default);

        UniTask WarmupAsync(
            AssetKey prefabKey,
            int count,
            Transform inactiveRoot = null,
            PoolPolicy policy = null,
            CancellationToken ct = default);

        void Return(InstanceRef instanceRef);
        bool Return(GameObject instance);
        int ReleaseUnused(AssetReleaseReason reason = AssetReleaseReason.Manual);
        void ReleasePool(AssetKey prefabKey, bool force = false);
        InstancePoolSnapshot GetSnapshot();
    }

    public sealed class InstanceRef : IDisposable
    {
        private readonly IInstancePool _pool;

        internal InstanceRef(AssetKey prefabKey, GameObject gameObject, IInstancePool pool)
        {
            PrefabKey = prefabKey;
            GameObject = gameObject;
            Transform = gameObject != null ? gameObject.transform : null;
            _pool = pool;
            IsValid = gameObject != null;
        }

        public AssetKey PrefabKey { get; }
        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public bool IsValid { get; private set; }

        public void Return()
        {
            if (!IsValid)
            {
                return;
            }

            IsValid = false;
            _pool?.Return(this);
        }

        public void Dispose()
        {
            Return();
        }
    }

    public sealed class InstancePoolState
    {
        public AssetKey PrefabKey;
        public int ActiveCount;
        public int IdleCount;
        public int Capacity;
        public bool Locked;
        public int Priority;
        public DateTime LastUseTimeUtc;
    }

    public sealed class InstancePoolSnapshot
    {
        public DateTime CapturedAtUtc;
        public int CanReleaseCount;
        public IReadOnlyList<InstancePoolState> Pools;
    }
}
