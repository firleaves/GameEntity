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

        public IAssetPool AssetPool { get; private set; }
        public IResourceUpdateSystem ResourceUpdateSystem { get; private set; }
        public IGameData GameData { get; private set; }
        public ISceneSystem SceneSystem { get; private set; }
        public IInstancePool InstancePool { get; private set; }
        public IAudioSystem AudioSystem { get; private set; }
        public ITimerSystem TimerSystem { get; private set; }
        public IEventBus EventBus { get; private set; }
        public ILocalizationSystem LocalizationSystem { get; private set; }
        public IGameSettings GameSettings { get; private set; }
        public IUISystem UISystem { get; private set; }
        public ISaveSystem SaveSystem { get; private set; }
        public IProcedureSystem ProcedureSystem { get; private set; }
        public IYooAssetBootstrap YooAssetBootstrap => _yooAssetBootstrap;

        public async UniTask InitializeAsync(FrameworkOptions options, Transform frameworkRoot, CancellationToken ct = default)
        {
            if (_initialized)
            {
                return;
            }

            var runtimeOptions = options != null ? options.Clone() : FrameworkOptions.CreateDefault();
            _yooAssetBootstrap = new YooAssetBootstrap();
            await _yooAssetBootstrap.InitializeAsync(runtimeOptions.YooAsset, ct);

            var assetPool = AddChild<AssetPoolEntity, IYooAssetBootstrap, AssetPoolPolicy>(
                _yooAssetBootstrap,
                runtimeOptions.AssetPool);
            AssetPool = assetPool;

            var gameData = AddChild<GameDataEntity, IAssetPool>(assetPool);
            GameData = gameData;

            var resourceUpdateSystem = AddChild<ResourceUpdateSystemEntity, IYooAssetBootstrap>(_yooAssetBootstrap);
            ResourceUpdateSystem = resourceUpdateSystem;

            var sceneSystem = AddChild<SceneSystemEntity, IYooAssetBootstrap>(_yooAssetBootstrap);
            SceneSystem = sceneSystem;

            var instanceRoot = CreateChildRoot(frameworkRoot, "[InstancePoolRoot]");
            var instancePool = AddChild<InstancePoolEntity, IAssetPool, PoolPolicy, Transform>(
                assetPool,
                runtimeOptions.InstancePool,
                instanceRoot);
            InstancePool = instancePool;

            var audioRoot = CreateChildRoot(frameworkRoot, "[AudioRoot]");
            var audioSystem = AddChild<AudioSystemEntity, IAssetPool, Transform>(
                assetPool,
                audioRoot);
            AudioSystem = audioSystem;

            var timerSystem = AddChild<TimerSystemEntity>();
            TimerSystem = timerSystem;

            var eventBus = AddChild<EventBusEntity>();
            EventBus = eventBus;

            var localizationSystem = AddChild<LocalizationSystemEntity, IAssetPool>(assetPool);
            LocalizationSystem = localizationSystem;

            var gameSettings = AddChild<GameSettingsEntity, IAudioSystem>(audioSystem);
            GameSettings = gameSettings;

            var uiSystem = AddChild<UISystemEntity, UISystemDependencies>(new UISystemDependencies
            {
                Options = runtimeOptions.UI,
                InstancePool = instancePool,
                FrameworkRoot = frameworkRoot,
                AutoCreateEventSystem = runtimeOptions.AutoCreateEventSystem
            });
            UISystem = uiSystem;

            var saveSystem = AddChild<SaveSystemEntity, SaveSystemConfig, ISaveStorage>(
                runtimeOptions.Save,
                null);
            SaveSystem = saveSystem;

            var procedureSystem = AddChild<ProcedureSystemEntity>();
            ProcedureSystem = procedureSystem;

            _initialized = true;
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
            _yooAssetBootstrap = null;
            _initialized = false;
            base.OnDestroy();
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
                Object.DontDestroyOnLoad(root);
            }

            root.SetActive(false);
            return root.transform;
        }
    }
}
