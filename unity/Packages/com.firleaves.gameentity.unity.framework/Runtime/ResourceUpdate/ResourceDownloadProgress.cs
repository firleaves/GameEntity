using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
