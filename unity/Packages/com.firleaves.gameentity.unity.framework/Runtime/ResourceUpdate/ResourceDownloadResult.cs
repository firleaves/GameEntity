using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
