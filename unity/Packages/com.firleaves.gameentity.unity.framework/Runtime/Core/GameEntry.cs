namespace GameEntity.Unity.Framework
{
    public static class GameEntry
    {
        public static FrameworkEntry Framework { get; private set; }

        public static IAssetPool Asset
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.AssetPool, FrameworkFeatures.Asset);
            }
        }

        public static IGameData Data
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.GameData, FrameworkFeatures.GameData);
            }
        }

        public static IResourceUpdateSystem ResourceUpdate
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.ResourceUpdateSystem, FrameworkFeatures.ResourceUpdate);
            }
        }

        public static ISceneSystem Scene
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.SceneSystem, FrameworkFeatures.Scene);
            }
        }

        public static IInstancePool Instance
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.InstancePool, FrameworkFeatures.InstancePool);
            }
        }

        public static IAudioSystem Audio
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.AudioSystem, FrameworkFeatures.Audio);
            }
        }

        public static ITimerSystem Timer
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.TimerSystem, FrameworkFeatures.Timer);
            }
        }

        public static IEventBus Event
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.EventBus, FrameworkFeatures.Event);
            }
        }

        public static ILocalizationSystem Localization
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.LocalizationSystem, FrameworkFeatures.Localization);
            }
        }

        public static IGameSettings Settings
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.GameSettings, FrameworkFeatures.GameSettings);
            }
        }

        public static IUISystem UI
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.UISystem, FrameworkFeatures.UI);
            }
        }

        public static ISaveSystem Save
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.SaveSystem, FrameworkFeatures.Save);
            }
        }

        public static IProcedureSystem Procedure
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.ProcedureSystem, FrameworkFeatures.Procedure);
            }
        }

        public static INetworkSystem Network
        {
            get
            {
                EnsureReady();
                return EnsureFeature(Framework.Scene.NetworkSystem, FrameworkFeatures.Network);
            }
        }

        public static T Get<T>()
        {
            EnsureReady();
            return Framework.Scene.GetRequiredService<T>();
        }

        public static bool TryGet<T>(out T service)
        {
            service = default;
            if (Framework == null || !Framework.IsReady || Framework.Scene == null)
            {
                return false;
            }

            return Framework.Scene.TryGetService(out service);
        }

        public static bool Has<T>()
        {
            return TryGet<T>(out _);
        }

        public static bool HasFeature(FrameworkFeatures feature)
        {
            if (Framework == null || Framework.Options == null)
            {
                return false;
            }

            return Framework.Options.HasFeature(feature);
        }

        internal static void Register(FrameworkEntry entry)
        {
            Framework = entry;
        }

        internal static void Unregister(FrameworkEntry entry)
        {
            if (ReferenceEquals(Framework, entry))
            {
                Framework = null;
            }
        }

        private static void EnsureReady()
        {
            if (Framework == null || !Framework.IsReady || Framework.Scene == null)
            {
                throw new FrameworkException("Framework 尚未初始化完成，不能访问 GameEntry 服务。");
            }
        }

        private static T EnsureFeature<T>(T service, FrameworkFeatures feature)
        {
            if (service == null)
            {
                throw new FrameworkException($"Framework 功能未启用：{feature}");
            }

            return service;
        }
    }
}
