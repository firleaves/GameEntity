using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class InstancePoolState
    {
        public AssetKey PrefabKey;
        public int ActiveCount;
        public int IdleCount;
        public int Capacity;
        public bool Locked;
        public int Priority;
        public DateTime LastUseTimeUtc;
    }

}
