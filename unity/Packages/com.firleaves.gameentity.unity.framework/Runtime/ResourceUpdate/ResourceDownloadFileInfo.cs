using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
