using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public static class InstancePoolExtensions
    {
        public static UniTask<InstanceRef> RentAsync(
            this IInstancePool instancePool,
            string prefabLocation,
            Transform parent = null,
            InstanceRentOptions options = null,
            string packageName = null,
            CancellationToken ct = default)
        {
            EnsureInstancePool(instancePool);
            return instancePool.RentAsync(AssetKey.Main<GameObject>(prefabLocation, packageName), parent, options, ct);
        }

        public static UniTask WarmupAsync(
            this IInstancePool instancePool,
            string prefabLocation,
            int count,
            Transform inactiveRoot = null,
            PoolPolicy policy = null,
            string packageName = null,
            CancellationToken ct = default)
        {
            EnsureInstancePool(instancePool);
            return instancePool.WarmupAsync(
                AssetKey.Main<GameObject>(prefabLocation, packageName),
                count,
                inactiveRoot,
                policy,
                ct);
        }

        public static void ReleasePool(
            this IInstancePool instancePool,
            string prefabLocation,
            bool force = false,
            string packageName = null)
        {
            EnsureInstancePool(instancePool);
            instancePool.ReleasePool(AssetKey.Main<GameObject>(prefabLocation, packageName), force);
        }

        private static void EnsureInstancePool(IInstancePool instancePool)
        {
            if (instancePool == null)
            {
                throw new FrameworkException("InstancePool 不能为空。");
            }
        }
    }
}
