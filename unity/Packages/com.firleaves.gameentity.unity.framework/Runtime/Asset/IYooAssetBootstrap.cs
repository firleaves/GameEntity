using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public interface IYooAssetBootstrap
    {
        ResourcePackage DefaultPackage { get; }
        UniTask InitializeAsync(YooAssetOptions options, CancellationToken ct = default);
        UniTask DestroyAsync(CancellationToken ct = default);
        ResourcePackage GetPackage(string packageName);
    }

}
