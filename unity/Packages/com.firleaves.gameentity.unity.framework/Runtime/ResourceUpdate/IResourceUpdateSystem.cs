using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public interface IResourceUpdateSystem
    {
        UniTask<string> RequestPackageVersionAsync(
            string packageName = null,
            bool appendTimeTicks = true,
            int timeout = 60,
            CancellationToken ct = default);

        UniTask UpdatePackageManifestAsync(
            string packageVersion,
            string packageName = null,
            int timeout = 60,
            CancellationToken ct = default);

        ResourceDownloader CreateDownloader(ResourceDownloadOptions options = null);

        UniTask<ResourceDownloader> PrepareDownloaderAsync(
            ResourceUpdateOptions options = null,
            CancellationToken ct = default);

        UniTask<ResourceDownloadResult> DownloadAsync(
            ResourceDownloader downloader,
            IProgress<ResourceDownloadProgress> progress = null,
            CancellationToken ct = default);
    }

}
