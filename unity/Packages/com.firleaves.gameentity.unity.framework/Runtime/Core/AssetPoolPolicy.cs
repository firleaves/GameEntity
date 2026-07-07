using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
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

}
