using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetPreloadItem
    {
        public AssetKey Key;
        public uint LoadPriority;
        public bool Locked;
        public int Priority;
    }

}
