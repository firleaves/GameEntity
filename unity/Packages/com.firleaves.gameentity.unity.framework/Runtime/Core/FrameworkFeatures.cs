using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Flags]
    public enum FrameworkFeatures
    {
        None = 0,
        Asset = 1 << 0,
        GameData = 1 << 1,
        ResourceUpdate = 1 << 2,
        Scene = 1 << 3,
        InstancePool = 1 << 4,
        Audio = 1 << 5,
        Timer = 1 << 6,
        Event = 1 << 7,
        Localization = 1 << 8,
        GameSettings = 1 << 9,
        UI = 1 << 10,
        Save = 1 << 11,
        Procedure = 1 << 12,
        Network = 1 << 13,
        Default = Asset
            | GameData
            | ResourceUpdate
            | Scene
            | InstancePool
            | Audio
            | Timer
            | Event
            | Localization
            | GameSettings
            | UI
            | Save
            | Procedure
            | Network
    }

}
