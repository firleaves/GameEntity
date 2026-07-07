using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class FrameworkOptions
    {
        public FrameworkFeatures Features = FrameworkFeatures.Default;
        public YooAssetOptions YooAsset = YooAssetOptions.CreateDefault();
        public AssetPoolPolicy AssetPool = AssetPoolPolicy.CreateDefault();
        public PoolPolicy InstancePool = PoolPolicy.CreateDefault();
        public EventBusOptions Event = EventBusOptions.CreateDefault();
        public UIOptions UI = UIOptions.CreateDefault();
        public SaveSystemConfig Save = SaveSystemConfig.CreateDefault();
        public NetworkOptions Network = NetworkOptions.CreateDefault();
        public bool AutoCreateEventSystem = true;
        public bool DontDestroyOnLoad = true;

        public FrameworkOptions Clone()
        {
            return new FrameworkOptions
            {
                YooAsset = YooAsset != null ? YooAsset.Clone() : YooAssetOptions.CreateDefault(),
                AssetPool = AssetPool != null ? AssetPool.Clone() : AssetPoolPolicy.CreateDefault(),
                InstancePool = InstancePool != null ? InstancePool.Clone() : PoolPolicy.CreateDefault(),
                Event = Event != null ? Event.Clone() : EventBusOptions.CreateDefault(),
                UI = UI != null ? UI.Clone() : UIOptions.CreateDefault(),
                Save = Save != null ? Save.Clone() : SaveSystemConfig.CreateDefault(),
                Network = Network != null ? Network.Clone() : NetworkOptions.CreateDefault(),
                Features = Features,
                AutoCreateEventSystem = AutoCreateEventSystem,
                DontDestroyOnLoad = DontDestroyOnLoad
            };
        }

        public bool HasFeature(FrameworkFeatures feature)
        {
            return (Features & feature) == feature;
        }

        public static FrameworkOptions CreateDefault()
        {
            return new FrameworkOptions();
        }

    }

}
