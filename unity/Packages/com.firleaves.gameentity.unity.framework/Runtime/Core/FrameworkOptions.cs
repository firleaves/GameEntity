using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum YooAssetPlayMode
    {
        EditorSimulate,
        OfflinePlay,
        HostPlay,
        WebPlay
    }

    [Serializable]
    public sealed class FrameworkOptions
    {
        public YooAssetOptions YooAsset = YooAssetOptions.CreateDefault();
        public AssetPoolPolicy AssetPool = AssetPoolPolicy.CreateDefault();
        public PoolPolicy InstancePool = PoolPolicy.CreateDefault();
        public UIOptions UI = UIOptions.CreateDefault();
        public SaveSystemConfig Save = SaveSystemConfig.CreateDefault();
        public bool AutoCreateEventSystem = true;
        public bool DontDestroyOnLoad = true;

        public FrameworkOptions Clone()
        {
            return new FrameworkOptions
            {
                YooAsset = YooAsset != null ? YooAsset.Clone() : YooAssetOptions.CreateDefault(),
                AssetPool = AssetPool != null ? AssetPool.Clone() : AssetPoolPolicy.CreateDefault(),
                InstancePool = InstancePool != null ? InstancePool.Clone() : PoolPolicy.CreateDefault(),
                UI = UI != null ? UI.Clone() : UIOptions.CreateDefault(),
                Save = Save != null ? Save.Clone() : SaveSystemConfig.CreateDefault(),
                AutoCreateEventSystem = AutoCreateEventSystem,
                DontDestroyOnLoad = DontDestroyOnLoad
            };
        }

        public static FrameworkOptions CreateDefault()
        {
            return new FrameworkOptions();
        }
    }

    [Serializable]
    public sealed class YooAssetOptions
    {
        public string DefaultPackageName = "DefaultPackage";
        public YooAssetPlayMode PlayMode = YooAssetPlayMode.EditorSimulate;
        public string BuildPipeline = "EditorSimulateBuildPipeline";
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
                BuildPipeline = BuildPipeline,
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

    [Serializable]
    public sealed class AssetPoolPolicy
    {
        public int Capacity = 256;
        public float ExpireSeconds = 60f;
        public float AutoReleaseIntervalSeconds = 10f;
        public int DefaultPriority;
        public bool ReleaseYooAssetUnusedAfterScan = true;
        public int YooAssetUnloadLoopCount = 10;

        public AssetPoolPolicy Clone()
        {
            return new AssetPoolPolicy
            {
                Capacity = Capacity,
                ExpireSeconds = ExpireSeconds,
                AutoReleaseIntervalSeconds = AutoReleaseIntervalSeconds,
                DefaultPriority = DefaultPriority,
                ReleaseYooAssetUnusedAfterScan = ReleaseYooAssetUnusedAfterScan,
                YooAssetUnloadLoopCount = YooAssetUnloadLoopCount
            };
        }

        public static AssetPoolPolicy CreateDefault()
        {
            return new AssetPoolPolicy();
        }
    }

    [Serializable]
    public sealed class PoolPolicy
    {
        public int Capacity = 32;
        public float ExpireSeconds = 60f;
        public float AutoReleaseIntervalSeconds = 10f;
        public int Priority;
        public bool Locked;

        public PoolPolicy Clone()
        {
            return new PoolPolicy
            {
                Capacity = Capacity,
                ExpireSeconds = ExpireSeconds,
                AutoReleaseIntervalSeconds = AutoReleaseIntervalSeconds,
                Priority = Priority,
                Locked = Locked
            };
        }

        public static PoolPolicy CreateDefault()
        {
            return new PoolPolicy();
        }
    }

    [Serializable]
    public sealed class UIOptions
    {
        public Transform UIRoot;
        public string DefaultGroup = "Default";
        public int GroupDepthStep = 1000;
        public bool AutoCreateCanvas = true;
        public bool UseInstancePool = true;

        public UIOptions Clone()
        {
            return new UIOptions
            {
                UIRoot = UIRoot,
                DefaultGroup = DefaultGroup,
                GroupDepthStep = GroupDepthStep,
                AutoCreateCanvas = AutoCreateCanvas,
                UseInstancePool = UseInstancePool
            };
        }

        public static UIOptions CreateDefault()
        {
            return new UIOptions();
        }
    }
}
