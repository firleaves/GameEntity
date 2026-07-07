using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum AssetReleaseReason
    {
        Manual,
        Capacity,
        Expired,
        GroupReleased,
        Shutdown
    }

}
