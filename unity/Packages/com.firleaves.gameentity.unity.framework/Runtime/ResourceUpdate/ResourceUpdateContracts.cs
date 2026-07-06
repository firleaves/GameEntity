using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public sealed class ResourceDownloadOptions
    {
        public string PackageName;
        public string[] Tags;
        public string[] Locations;
        public bool RecursiveDownload;
        public int DownloadingMaxNumber = 10;
        public int FailedTryAgain = 3;
    }

    public sealed class ResourceUpdateOptions
    {
        public string PackageName;
        public string PackageVersion;
        public bool RequestVersion = true;
        public bool UpdateManifest = true;
        public bool AppendTimeTicks = true;
        public int Timeout = 60;
        public ResourceDownloadOptions Download = new ResourceDownloadOptions();
    }

    public readonly struct ResourceDownloadProgress
    {
        public readonly string PackageName;
        public readonly float Progress;
        public readonly int TotalDownloadCount;
        public readonly int CurrentDownloadCount;
        public readonly long TotalDownloadBytes;
        public readonly long CurrentDownloadBytes;

        public ResourceDownloadProgress(
            string packageName,
            float progress,
            int totalDownloadCount,
            int currentDownloadCount,
            long totalDownloadBytes,
            long currentDownloadBytes)
        {
            PackageName = packageName;
            Progress = progress;
            TotalDownloadCount = totalDownloadCount;
            CurrentDownloadCount = currentDownloadCount;
            TotalDownloadBytes = totalDownloadBytes;
            CurrentDownloadBytes = currentDownloadBytes;
        }
    }

    public readonly struct ResourceDownloadFileInfo
    {
        public readonly string PackageName;
        public readonly string FileName;
        public readonly long FileSize;

        public ResourceDownloadFileInfo(string packageName, string fileName, long fileSize)
        {
            PackageName = packageName;
            FileName = fileName;
            FileSize = fileSize;
        }
    }

    public readonly struct ResourceDownloadErrorInfo
    {
        public readonly string PackageName;
        public readonly string FileName;
        public readonly string Error;

        public ResourceDownloadErrorInfo(string packageName, string fileName, string error)
        {
            PackageName = packageName;
            FileName = fileName;
            Error = error;
        }
    }

    public readonly struct ResourceDownloadResult
    {
        public readonly string PackageName;
        public readonly bool Succeeded;
        public readonly string Error;
        public readonly int TotalDownloadCount;
        public readonly long TotalDownloadBytes;

        public ResourceDownloadResult(
            string packageName,
            bool succeeded,
            string error,
            int totalDownloadCount,
            long totalDownloadBytes)
        {
            PackageName = packageName;
            Succeeded = succeeded;
            Error = error;
            TotalDownloadCount = totalDownloadCount;
            TotalDownloadBytes = totalDownloadBytes;
        }
    }

    public sealed class ResourceDownloader
    {
        private readonly ResourceDownloaderOperation _operation;

        internal ResourceDownloader(ResourceDownloaderOperation operation)
        {
            _operation = operation ?? throw new FrameworkException("ResourceDownloader 需要有效的 YooAsset 下载器。");
            PackageName = operation.PackageName;
            BindCallbacks();
        }

        public string PackageName { get; }
        public int TotalDownloadCount => _operation.TotalDownloadCount;
        public long TotalDownloadBytes => _operation.TotalDownloadBytes;
        public int CurrentDownloadCount => _operation.CurrentDownloadCount;
        public long CurrentDownloadBytes => _operation.CurrentDownloadBytes;
        public bool IsEmpty => TotalDownloadCount <= 0 || TotalDownloadBytes <= 0;
        public bool IsDone => _operation.IsDone;
        public bool Succeeded => _operation.Status == EOperationStatus.Succeed;
        public string Error => _operation.Error;

        public event Action<ResourceDownloadProgress> ProgressChanged;
        public event Action<ResourceDownloadFileInfo> FileDownloadStarted;
        public event Action<ResourceDownloadErrorInfo> DownloadFailed;
        public event Action<ResourceDownloadResult> Finished;

        internal ResourceDownloaderOperation Operation => _operation;

        public void Begin()
        {
            _operation.BeginDownload();
        }

        public void Pause()
        {
            _operation.PauseDownload();
        }

        public void Resume()
        {
            _operation.ResumeDownload();
        }

        public void Cancel()
        {
            _operation.CancelDownload();
        }

        private void BindCallbacks()
        {
            _operation.DownloadUpdateCallback = data =>
            {
                ProgressChanged?.Invoke(new ResourceDownloadProgress(
                    data.PackageName,
                    data.Progress,
                    data.TotalDownloadCount,
                    data.CurrentDownloadCount,
                    data.TotalDownloadBytes,
                    data.CurrentDownloadBytes));
            };

            _operation.DownloadFileBeginCallback = data =>
            {
                FileDownloadStarted?.Invoke(new ResourceDownloadFileInfo(
                    data.PackageName,
                    data.FileName,
                    data.FileSize));
            };

            _operation.DownloadErrorCallback = data =>
            {
                DownloadFailed?.Invoke(new ResourceDownloadErrorInfo(
                    data.PackageName,
                    data.FileName,
                    data.ErrorInfo));
            };

            _operation.DownloadFinishCallback = data =>
            {
                Finished?.Invoke(new ResourceDownloadResult(
                    data.PackageName,
                    data.Succeed,
                    _operation.Error,
                    _operation.TotalDownloadCount,
                    _operation.TotalDownloadBytes));
            };
        }
    }

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
