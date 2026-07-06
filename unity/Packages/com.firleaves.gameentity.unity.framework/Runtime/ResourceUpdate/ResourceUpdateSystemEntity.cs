using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public sealed class ResourceUpdateSystemEntity : Entity, IAwake<IYooAssetBootstrap>, IDestroy, IResourceUpdateSystem
    {
        private IYooAssetBootstrap _bootstrap;

        public void Awake(IYooAssetBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new FrameworkException("ResourceUpdateSystem 初始化失败：YooAssetBootstrap 不能为空。");
        }

        public void OnDestroy()
        {
            _bootstrap = null;
        }

        public async UniTask<string> RequestPackageVersionAsync(
            string packageName = null,
            bool appendTimeTicks = true,
            int timeout = 60,
            CancellationToken ct = default)
        {
            var package = GetPackage(packageName);
            var operation = package.RequestPackageVersionAsync(appendTimeTicks, Math.Max(1, timeout));
            await operation.Task.AsUniTask().AttachExternalCancellation(ct);
            if (operation.Status != EOperationStatus.Succeed)
            {
                throw new FrameworkException($"请求资源版本失败：Package={package.PackageName}, Error={operation.Error}");
            }

            return operation.PackageVersion;
        }

        public async UniTask UpdatePackageManifestAsync(
            string packageVersion,
            string packageName = null,
            int timeout = 60,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new FrameworkException("更新资源清单失败：packageVersion 不能为空。");
            }

            var package = GetPackage(packageName);
            var operation = package.UpdatePackageManifestAsync(packageVersion, Math.Max(1, timeout));
            await operation.Task.AsUniTask().AttachExternalCancellation(ct);
            if (operation.Status != EOperationStatus.Succeed)
            {
                throw new FrameworkException($"更新资源清单失败：Package={package.PackageName}, Version={packageVersion}, Error={operation.Error}");
            }
        }

        public ResourceDownloader CreateDownloader(ResourceDownloadOptions options = null)
        {
            options = NormalizeDownloadOptions(options, options != null ? options.PackageName : null);
            var package = GetPackage(options.PackageName);
            var downloadingMaxNumber = Math.Max(1, options.DownloadingMaxNumber);
            var failedTryAgain = Math.Max(0, options.FailedTryAgain);
            ResourceDownloaderOperation operation;

            if (HasItems(options.Locations) && HasItems(options.Tags))
            {
                throw new FrameworkException("创建下载器失败：Locations 和 Tags 不能同时指定。");
            }

            if (HasItems(options.Locations))
            {
                operation = options.Locations.Length == 1
                    ? package.CreateBundleDownloader(options.Locations[0], options.RecursiveDownload, downloadingMaxNumber, failedTryAgain)
                    : package.CreateBundleDownloader(options.Locations, options.RecursiveDownload, downloadingMaxNumber, failedTryAgain);
            }
            else if (HasItems(options.Tags))
            {
                operation = options.Tags.Length == 1
                    ? package.CreateResourceDownloader(options.Tags[0], downloadingMaxNumber, failedTryAgain)
                    : package.CreateResourceDownloader(options.Tags, downloadingMaxNumber, failedTryAgain);
            }
            else
            {
                operation = package.CreateResourceDownloader(downloadingMaxNumber, failedTryAgain);
            }

            return new ResourceDownloader(operation);
        }

        public async UniTask<ResourceDownloader> PrepareDownloaderAsync(
            ResourceUpdateOptions options = null,
            CancellationToken ct = default)
        {
            options = options ?? new ResourceUpdateOptions();
            var packageName = options.PackageName;
            var packageVersion = options.PackageVersion;

            if (options.RequestVersion || string.IsNullOrWhiteSpace(packageVersion))
            {
                packageVersion = await RequestPackageVersionAsync(
                    packageName,
                    options.AppendTimeTicks,
                    options.Timeout,
                    ct);
            }

            if (options.UpdateManifest)
            {
                await UpdatePackageManifestAsync(packageVersion, packageName, options.Timeout, ct);
            }

            return CreateDownloader(NormalizeDownloadOptions(options.Download, packageName));
        }

        public async UniTask<ResourceDownloadResult> DownloadAsync(
            ResourceDownloader downloader,
            IProgress<ResourceDownloadProgress> progress = null,
            CancellationToken ct = default)
        {
            if (downloader == null)
            {
                throw new FrameworkException("下载资源失败：downloader 不能为空。");
            }

            void OnProgress(ResourceDownloadProgress value)
            {
                progress?.Report(value);
            }

            downloader.ProgressChanged += OnProgress;
            try
            {
                downloader.Begin();
                await downloader.Operation.Task.AsUniTask().AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                downloader.Cancel();
                throw;
            }
            finally
            {
                downloader.ProgressChanged -= OnProgress;
            }

            var result = new ResourceDownloadResult(
                downloader.PackageName,
                downloader.Succeeded,
                downloader.Error,
                downloader.TotalDownloadCount,
                downloader.TotalDownloadBytes);

            if (!result.Succeeded)
            {
                throw new FrameworkException($"下载资源失败：Package={result.PackageName}, Error={result.Error}");
            }

            return result;
        }

        private ResourcePackage GetPackage(string packageName)
        {
            if (_bootstrap == null)
            {
                throw new FrameworkException("ResourceUpdateSystem 尚未初始化。");
            }

            return _bootstrap.GetPackage(packageName);
        }

        private static ResourceDownloadOptions NormalizeDownloadOptions(ResourceDownloadOptions options, string fallbackPackageName)
        {
            options = options ?? new ResourceDownloadOptions();
            if (string.IsNullOrWhiteSpace(options.PackageName))
            {
                options.PackageName = fallbackPackageName;
            }

            if (options.DownloadingMaxNumber <= 0)
            {
                options.DownloadingMaxNumber = 10;
            }

            if (options.FailedTryAgain < 0)
            {
                options.FailedTryAgain = 0;
            }

            options.Tags = NormalizeItems(options.Tags);
            options.Locations = NormalizeItems(options.Locations);
            return options;
        }

        private static bool HasItems(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] NormalizeItems(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            var items = new List<string>(values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    items.Add(values[i]);
                }
            }

            return items.Count > 0 ? items.ToArray() : null;
        }
    }
}
