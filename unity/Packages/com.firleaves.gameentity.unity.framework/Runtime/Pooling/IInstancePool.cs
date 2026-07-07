using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
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

}
