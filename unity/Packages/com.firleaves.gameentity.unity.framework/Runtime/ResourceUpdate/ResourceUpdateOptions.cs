using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
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

}
