using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
