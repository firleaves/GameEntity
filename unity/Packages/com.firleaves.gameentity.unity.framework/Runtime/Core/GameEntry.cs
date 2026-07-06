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
                return Framework.Scene.AssetPool;
            }
        }

        public static IGameData Data
        {
            get
            {
                EnsureReady();
                return Framework.Scene.GameData;
            }
        }

        public static IResourceUpdateSystem ResourceUpdate
        {
            get
            {
                EnsureReady();
                return Framework.Scene.ResourceUpdateSystem;
            }
        }

        public static ISceneSystem Scene
        {
            get
            {
                EnsureReady();
                return Framework.Scene.SceneSystem;
            }
        }

        public static IInstancePool Instance
        {
            get
            {
                EnsureReady();
                return Framework.Scene.InstancePool;
            }
        }

        public static IAudioSystem Audio
        {
            get
            {
                EnsureReady();
                return Framework.Scene.AudioSystem;
            }
        }

        public static ITimerSystem Timer
        {
            get
            {
                EnsureReady();
                return Framework.Scene.TimerSystem;
            }
        }

        public static IEventBus Event
        {
            get
            {
                EnsureReady();
                return Framework.Scene.EventBus;
            }
        }

        public static ILocalizationSystem Localization
        {
            get
            {
                EnsureReady();
                return Framework.Scene.LocalizationSystem;
            }
        }

        public static IGameSettings Settings
        {
            get
            {
                EnsureReady();
                return Framework.Scene.GameSettings;
            }
        }

        public static IUISystem UI
        {
            get
            {
                EnsureReady();
                return Framework.Scene.UISystem;
            }
        }

        public static ISaveSystem Save
        {
            get
            {
                EnsureReady();
                return Framework.Scene.SaveSystem;
            }
        }

        public static IProcedureSystem Procedure
        {
            get
            {
                EnsureReady();
                return Framework.Scene.ProcedureSystem;
            }
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
    }
}
