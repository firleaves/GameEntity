using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
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
