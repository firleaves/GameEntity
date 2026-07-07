using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
