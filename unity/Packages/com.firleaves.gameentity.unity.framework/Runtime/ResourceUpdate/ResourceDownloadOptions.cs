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

}
