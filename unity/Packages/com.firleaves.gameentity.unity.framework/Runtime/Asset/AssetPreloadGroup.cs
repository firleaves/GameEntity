using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetPreloadGroup
    {
        public string Name;
        public float? ExpireSeconds;
        public IReadOnlyList<AssetPreloadItem> Items;
        public IProgress<AssetPreloadProgress> Progress;
    }

}
