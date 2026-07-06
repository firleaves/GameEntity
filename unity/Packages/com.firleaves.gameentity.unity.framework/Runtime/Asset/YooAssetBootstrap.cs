using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public interface IYooAssetBootstrap
    {
        ResourcePackage DefaultPackage { get; }
        UniTask InitializeAsync(YooAssetOptions options, CancellationToken ct = default);
        UniTask DestroyAsync(CancellationToken ct = default);
        ResourcePackage GetPackage(string packageName);
    }

    public sealed class YooAssetBootstrap : IYooAssetBootstrap
    {
        private YooAssetOptions _options;
        private bool _createdPackage;

        public ResourcePackage DefaultPackage { get; private set; }

        public async UniTask InitializeAsync(YooAssetOptions options, CancellationToken ct = default)
        {
            if (DefaultPackage != null && DefaultPackage.InitializeStatus == EOperationStatus.Succeed)
            {
                return;
            }

            _options = options != null ? options.Clone() : YooAssetOptions.CreateDefault();
            if (string.IsNullOrWhiteSpace(_options.DefaultPackageName))
            {
                throw new FrameworkException("YooAsset 默认 package 名称不能为空。");
            }

            if (!YooAssets.Initialized)
            {
                YooAssets.Initialize();
            }

            var package = YooAssets.TryGetPackage(_options.DefaultPackageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(_options.DefaultPackageName);
                _createdPackage = true;
            }

            DefaultPackage = package;
            if (_options.SetAsDefaultPackage)
            {
                YooAssets.SetDefaultPackage(package);
            }

            if (package.InitializeStatus == EOperationStatus.Succeed)
            {
                return;
            }

            var parameters = CreateInitializeParameters(_options);
            ApplyCommonParameters(parameters, _options);
            var operation = package.InitializeAsync(parameters);
            await operation.Task.AsUniTask().AttachExternalCancellation(ct);
            if (operation.Status != EOperationStatus.Succeed)
            {
                throw new FrameworkException(
                    $"YooAsset package 初始化失败：Package={_options.DefaultPackageName}, PlayMode={_options.PlayMode}, Error={operation.Error}");
            }
        }

        public ResourcePackage GetPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return DefaultPackage;
            }

            var package = YooAssets.TryGetPackage(packageName);
            if (package == null)
            {
                throw new FrameworkException($"找不到 YooAsset package：{packageName}");
            }

            return package;
        }

        public async UniTask DestroyAsync(CancellationToken ct = default)
        {
            var options = _options;
            var package = DefaultPackage;
            DefaultPackage = null;

            if (package != null && options != null && options.DestroyPackageOnShutdown)
            {
                var operation = package.DestroyAsync();
                await operation.Task.AsUniTask().AttachExternalCancellation(ct);
                if (operation.Status != EOperationStatus.Succeed)
                {
                    throw new FrameworkException($"YooAsset package 销毁失败：{package.PackageName}, Error={operation.Error}");
                }

                if (_createdPackage)
                {
                    YooAssets.RemovePackage(package);
                }
            }

            if (options != null && options.DestroyYooAssetsOnShutdown)
            {
                YooAssets.Destroy();
            }

            _createdPackage = false;
            _options = null;
        }

        private static InitializeParameters CreateInitializeParameters(YooAssetOptions options)
        {
            switch (options.PlayMode)
            {
                case YooAssetPlayMode.EditorSimulate:
                {
#if UNITY_EDITOR
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(options.DefaultPackageName);
                    return new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory)
                    };
#else
                    throw new FrameworkException("EditorSimulate 只能在 Unity Editor 中使用。");
#endif
                }
                case YooAssetPlayMode.OfflinePlay:
                    return new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                case YooAssetPlayMode.HostPlay:
                {
                    if (string.IsNullOrWhiteSpace(options.HostServerUrl))
                    {
                        throw new FrameworkException("HostPlay 需要配置 HostServerUrl。");
                    }

                    var remoteServices = new FrameworkRemoteServices(options.HostServerUrl, options.FallbackHostServerUrl);
                    return new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                    };
                }
                case YooAssetPlayMode.WebPlay:
                    return new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(options.PlayMode), options.PlayMode, "未知的 YooAsset PlayMode。");
            }
        }

        private static void ApplyCommonParameters(InitializeParameters parameters, YooAssetOptions options)
        {
            parameters.BundleLoadingMaxConcurrency = Math.Max(1, options.BundleLoadingMaxConcurrency);
            parameters.AutoUnloadBundleWhenUnused = options.AutoUnloadBundleWhenUnused;
            parameters.WebGLForceSyncLoadAsset = options.WebGLForceSyncLoadAsset;
        }

        private sealed class FrameworkRemoteServices : IRemoteServices
        {
            private readonly string _mainUrl;
            private readonly string _fallbackUrl;

            public FrameworkRemoteServices(string mainUrl, string fallbackUrl)
            {
                _mainUrl = TrimEnd(mainUrl);
                _fallbackUrl = string.IsNullOrWhiteSpace(fallbackUrl) ? _mainUrl : TrimEnd(fallbackUrl);
            }

            public string GetRemoteMainURL(string fileName)
            {
                return $"{_mainUrl}/{fileName}";
            }

            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackUrl}/{fileName}";
            }

            private static string TrimEnd(string value)
            {
                return (value ?? string.Empty).TrimEnd('/');
            }
        }
    }
}
