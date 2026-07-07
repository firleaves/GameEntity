using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class FrameworkScene : Scene
    {
        private YooAssetBootstrap _yooAssetBootstrap;
        private bool _initialized;

        public FrameworkScene(string name) : base(name)
        {
        }

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public IAssetPool AssetPool { get; internal set; }
        public IResourceUpdateSystem ResourceUpdateSystem { get; internal set; }
        public IGameData GameData { get; internal set; }
        public ISceneSystem SceneSystem { get; internal set; }
        public IInstancePool InstancePool { get; internal set; }
        public IAudioSystem AudioSystem { get; internal set; }
        public ITimerSystem TimerSystem { get; internal set; }
        public IEventBus EventBus { get; internal set; }
        public ILocalizationSystem LocalizationSystem { get; internal set; }
        public IGameSettings GameSettings { get; internal set; }
        public IUISystem UISystem { get; internal set; }
        public ISaveSystem SaveSystem { get; internal set; }
        public IProcedureSystem ProcedureSystem { get; internal set; }
        public INetworkSystem NetworkSystem { get; internal set; }
        public IYooAssetBootstrap YooAssetBootstrap => _yooAssetBootstrap;

        public async UniTask InitializeAsync(
            FrameworkOptions options,
            Transform frameworkRoot,
            FrameworkExtensionAsset[] extensions = null,
            CancellationToken ct = default)
        {
            if (_initialized)
            {
                return;
            }

            var runtimeOptions = options != null ? options.Clone() : FrameworkOptions.CreateDefault();
            _yooAssetBootstrap = new YooAssetBootstrap();
            await _yooAssetBootstrap.InitializeAsync(runtimeOptions.YooAsset, ct);

            if (runtimeOptions.HasFeature(FrameworkFeatures.Asset))
            {
                var assetPool = AddChild<AssetPoolEntity, IYooAssetBootstrap, AssetPoolPolicy>(
                    _yooAssetBootstrap,
                    runtimeOptions.AssetPool);
                AssetPool = assetPool;
                SetService<IAssetPool>(assetPool);
                SetService<IAssetUsageTracker>(assetPool);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.GameData))
            {
                var assetPool = RequireService(AssetPool, FrameworkFeatures.Asset, FrameworkFeatures.GameData);
                var gameData = AddChild<GameDataEntity, IAssetPool>(assetPool);
                GameData = gameData;
                SetService<IGameData>(gameData);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.ResourceUpdate))
            {
                var resourceUpdateSystem = AddChild<ResourceUpdateSystemEntity, IYooAssetBootstrap>(_yooAssetBootstrap);
                ResourceUpdateSystem = resourceUpdateSystem;
                SetService<IResourceUpdateSystem>(resourceUpdateSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Scene))
            {
                var sceneSystem = AddChild<SceneSystemEntity, IYooAssetBootstrap>(_yooAssetBootstrap);
                SceneSystem = sceneSystem;
                SetService<ISceneSystem>(sceneSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.InstancePool))
            {
                var assetPool = RequireService(AssetPool, FrameworkFeatures.Asset, FrameworkFeatures.InstancePool);
                var instanceRoot = CreateChildRoot(frameworkRoot, "[InstancePoolRoot]");
                var instancePool = AddChild<InstancePoolEntity, IAssetPool, PoolPolicy, Transform>(
                    assetPool,
                    runtimeOptions.InstancePool,
                    instanceRoot);
                InstancePool = instancePool;
                SetService<IInstancePool>(instancePool);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Audio))
            {
                var assetPool = RequireService(AssetPool, FrameworkFeatures.Asset, FrameworkFeatures.Audio);
                var audioRoot = CreateChildRoot(frameworkRoot, "[AudioRoot]");
                var audioSystem = AddChild<AudioSystemEntity, IAssetPool, Transform>(
                    assetPool,
                    audioRoot);
                AudioSystem = audioSystem;
                SetService<IAudioSystem>(audioSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Timer))
            {
                var timerSystem = AddChild<TimerSystemEntity>();
                TimerSystem = timerSystem;
                SetService<ITimerSystem>(timerSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Event))
            {
                var eventBus = AddChild<EventBusEntity, EventBusOptions>(runtimeOptions.Event);
                EventBus = eventBus;
                SetService<IEventBus>(eventBus);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Localization))
            {
                var assetPool = RequireService(AssetPool, FrameworkFeatures.Asset, FrameworkFeatures.Localization);
                var localizationSystem = AddChild<LocalizationSystemEntity, IAssetPool>(assetPool);
                LocalizationSystem = localizationSystem;
                SetService<ILocalizationSystem>(localizationSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.GameSettings))
            {
                var gameSettings = AddChild<GameSettingsEntity, IAudioSystem>(AudioSystem);
                GameSettings = gameSettings;
                SetService<IGameSettings>(gameSettings);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.UI))
            {
                if (runtimeOptions.UI != null && runtimeOptions.UI.UseInstancePool && InstancePool == null)
                {
                    throw new FrameworkException(
                        $"Framework 功能 {FrameworkFeatures.UI} 需要先启用 {FrameworkFeatures.InstancePool}，或关闭 UIOptions.UseInstancePool。");
                }

                var uiSystem = AddChild<UISystemEntity, UISystemDependencies>(new UISystemDependencies
                {
                    Options = runtimeOptions.UI,
                    InstancePool = InstancePool,
                    FrameworkRoot = frameworkRoot,
                    AutoCreateEventSystem = runtimeOptions.AutoCreateEventSystem
                });
                UISystem = uiSystem;
                SetService<IUISystem>(uiSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Save))
            {
                var saveSystem = AddChild<SaveSystemEntity, SaveSystemConfig, ISaveStorage>(
                    runtimeOptions.Save,
                    null);
                SaveSystem = saveSystem;
                SetService<ISaveSystem>(saveSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Procedure))
            {
                var procedureSystem = AddChild<ProcedureSystemEntity>();
                ProcedureSystem = procedureSystem;
                SetService<IProcedureSystem>(procedureSystem);
            }

            if (runtimeOptions.HasFeature(FrameworkFeatures.Network))
            {
                var networkSystem = AddChild<NetworkSystemEntity, NetworkOptions>(runtimeOptions.Network);
                NetworkSystem = networkSystem;
                SetService<INetworkSystem>(networkSystem);
            }

            InstallExtensions(runtimeOptions, frameworkRoot, extensions);

            _initialized = true;
        }

        public bool TryGetService<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out var value) && value is T typed)
            {
                service = typed;
                return true;
            }

            service = default;
            return false;
        }

        public T GetRequiredService<T>()
        {
            if (TryGetService(out T service))
            {
                return service;
            }

            throw new FrameworkException($"Framework 服务未启用或未注册：{typeof(T).Name}");
        }

        internal void SetService<T>(T service)
        {
            if (service == null)
            {
                throw new FrameworkException($"注册 Framework 服务失败：{typeof(T).Name} 不能为空。");
            }

            _services[typeof(T)] = service;
        }

        private static T RequireService<T>(T service, FrameworkFeatures required, FrameworkFeatures current)
        {
            if (service == null)
            {
                throw new FrameworkException($"Framework 功能 {current} 需要先启用 {required}。");
            }

            return service;
        }

        private void InstallExtensions(
            FrameworkOptions options,
            Transform frameworkRoot,
            FrameworkExtensionAsset[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
                return;
            }

            var context = new FrameworkExtensionContext(this, options, frameworkRoot);
            for (var i = 0; i < extensions.Length; i++)
            {
                var extension = extensions[i];
                if (extension == null)
                {
                    continue;
                }

                extension.InstallIfEnabled(context);
            }
        }

        public async UniTask ShutdownAsync(CancellationToken ct = default)
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;

            if (ProcedureSystem is Entity procedureEntity && !procedureEntity.IsDestroyed)
            {
                procedureEntity.Destroy();
            }

            if (NetworkSystem is Entity networkEntity && !networkEntity.IsDestroyed)
            {
                networkEntity.Destroy();
            }

            if (UISystem is Entity uiEntity && !uiEntity.IsDestroyed)
            {
                uiEntity.Destroy();
            }

            if (GameSettings is Entity gameSettingsEntity && !gameSettingsEntity.IsDestroyed)
            {
                gameSettingsEntity.Destroy();
            }

            if (LocalizationSystem is Entity localizationEntity && !localizationEntity.IsDestroyed)
            {
                localizationEntity.Destroy();
            }

            if (EventBus is Entity eventEntity && !eventEntity.IsDestroyed)
            {
                eventEntity.Destroy();
            }

            if (TimerSystem is Entity timerEntity && !timerEntity.IsDestroyed)
            {
                timerEntity.Destroy();
            }

            if (AudioSystem is Entity audioEntity && !audioEntity.IsDestroyed)
            {
                audioEntity.Destroy();
            }

            if (SaveSystem is Entity saveEntity && !saveEntity.IsDestroyed)
            {
                saveEntity.Destroy();
            }

            if (SceneSystem is Entity sceneEntity && !sceneEntity.IsDestroyed)
            {
                sceneEntity.Destroy();
            }

            if (InstancePool is Entity instanceEntity && !instanceEntity.IsDestroyed)
            {
                instanceEntity.Destroy();
            }

            if (GameData is Entity dataEntity && !dataEntity.IsDestroyed)
            {
                dataEntity.Destroy();
            }

            if (ResourceUpdateSystem is Entity resourceUpdateEntity && !resourceUpdateEntity.IsDestroyed)
            {
                resourceUpdateEntity.Destroy();
            }

            if (AssetPool is Entity assetEntity && !assetEntity.IsDestroyed)
            {
                assetEntity.Destroy();
            }

            DestroyRemainingChildren();

            AssetPool = null;
            ResourceUpdateSystem = null;
            GameData = null;
            SceneSystem = null;
            InstancePool = null;
            AudioSystem = null;
            TimerSystem = null;
            EventBus = null;
            LocalizationSystem = null;
            GameSettings = null;
            UISystem = null;
            SaveSystem = null;
            ProcedureSystem = null;
            NetworkSystem = null;
            _services.Clear();

            if (_yooAssetBootstrap != null)
            {
                await _yooAssetBootstrap.DestroyAsync(ct);
                _yooAssetBootstrap = null;
            }
        }

        public override void OnDestroy()
        {
            if (_yooAssetBootstrap != null)
            {
                _yooAssetBootstrap.DestroyAsync().Forget(Debug.LogException);
            }

            AssetPool = null;
            ResourceUpdateSystem = null;
            GameData = null;
            SceneSystem = null;
            InstancePool = null;
            AudioSystem = null;
            TimerSystem = null;
            EventBus = null;
            LocalizationSystem = null;
            GameSettings = null;
            UISystem = null;
            SaveSystem = null;
            ProcedureSystem = null;
            NetworkSystem = null;
            _yooAssetBootstrap = null;
            _services.Clear();
            _initialized = false;
            base.OnDestroy();
        }

        private void DestroyRemainingChildren()
        {
            var children = GetAllChildren();
            if (children.Count == 0)
            {
                return;
            }

            var buffer = new List<Entity>(children);
            for (var i = 0; i < buffer.Count; i++)
            {
                var child = buffer[i];
                if (child != null && !child.IsDestroyed)
                {
                    child.Destroy();
                }
            }
        }

        private static Transform CreateChildRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }
            else
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            root.SetActive(false);
            return root.transform;
        }
    }
}
