using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class YooAssetOptions
    {
        public string DefaultPackageName = "DefaultPackage";
        public YooAssetPlayMode PlayMode = YooAssetPlayMode.EditorSimulate;
        public string HostServerUrl;
        public string FallbackHostServerUrl;
        public bool SetAsDefaultPackage = true;
        public bool DestroyPackageOnShutdown = true;
        public bool DestroyYooAssetsOnShutdown;
        public int BundleLoadingMaxConcurrency = int.MaxValue;
        public bool AutoUnloadBundleWhenUnused;
        public bool WebGLForceSyncLoadAsset;

        public YooAssetOptions Clone()
        {
            return new YooAssetOptions
            {
                DefaultPackageName = DefaultPackageName,
                PlayMode = PlayMode,
                HostServerUrl = HostServerUrl,
                FallbackHostServerUrl = FallbackHostServerUrl,
                SetAsDefaultPackage = SetAsDefaultPackage,
                DestroyPackageOnShutdown = DestroyPackageOnShutdown,
                DestroyYooAssetsOnShutdown = DestroyYooAssetsOnShutdown,
                BundleLoadingMaxConcurrency = BundleLoadingMaxConcurrency,
                AutoUnloadBundleWhenUnused = AutoUnloadBundleWhenUnused,
                WebGLForceSyncLoadAsset = WebGLForceSyncLoadAsset
            };
        }

        public static YooAssetOptions CreateDefault()
        {
            return new YooAssetOptions();
        }
    }

}
